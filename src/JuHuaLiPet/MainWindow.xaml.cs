using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace JuHuaLiPet;

public partial class MainWindow : Window
{
    private const double BaseWindowWidth = 268;
    private const double BaseWindowHeight = 300;
    private const double BasePetWidth = 230;
    private const double BasePetHeight = 250;

    private readonly TodoStore _todoStore = new();
    private readonly SettingsStore _settingsStore = new();
    private readonly PetSoundPlayer _soundPlayer = new();
    private readonly Random _random = new();
    private readonly DispatcherTimer _reminderTimer = new() { Interval = TimeSpan.FromSeconds(10) };
    private readonly DispatcherTimer _idleBehaviorTimer = new() { Interval = TimeSpan.FromSeconds(6) };
    private readonly DispatcherTimer _strollTimer = new() { Interval = TimeSpan.FromMilliseconds(33) };
    private readonly DispatcherTimer _keyboardHopTimer = new() { Interval = TimeSpan.FromMilliseconds(24) };
    private SpriteAnimator? _animator;
    private ReminderWindow? _activeReminder;
    private MiniKeyboardWindow? _miniKeyboard;
    private GlobalKeyboardHook? _keyboardHook;
    private bool _dragging;
    private bool _isStrolling;
    private bool _isHidingNearTaskbar;
    private bool _keyboardMode;
    private bool _isKeyboardHopping;
    private bool _modalOpen;
    private DateTime _busyUntil = DateTime.MinValue;
    private DateTime _strollStartedAt;
    private DateTime _keyboardHopStartedAt;
    private TimeSpan _strollDuration;
    private TimeSpan _keyboardHopDuration;
    private Point _dragStartScreen;
    private double _dragStartLeft;
    private double _dragStartTop;
    private double _lastDragX;
    private double _strollStartLeft;
    private double _strollTargetLeft;
    private double _keyboardHopStartLeft;
    private double _keyboardHopStartTop;
    private double _keyboardHopTargetLeft;
    private double _keyboardHopTargetTop;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        MouseLeftButtonDown += OnMouseLeftButtonDown;
        MouseMove += OnMouseMove;
        MouseLeftButtonUp += OnMouseLeftButtonUp;
        MouseDoubleClick += (_, _) => PlayPetVoice();
        ContextMenu = BuildContextMenu();
        _reminderTimer.Tick += (_, _) => CheckReminders();
        _idleBehaviorTimer.Tick += (_, _) => MaybeRunIdleBehavior();
        _strollTimer.Tick += (_, _) => AdvanceStroll();
        _keyboardHopTimer.Tick += (_, _) => AdvanceKeyboardHop();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _animator = new SpriteAnimator(PetImage);
        _soundPlayer.Volume = _settingsStore.Settings.Volume;
        ApplyPetScale(_settingsStore.Settings.PetScale, preserveAnchor: false);
        var workArea = SystemParameters.WorkArea;
        Left = workArea.Right - Width - 40;
        Top = workArea.Bottom - Height - 60;
        _reminderTimer.Start();
        _idleBehaviorTimer.Start();
    }

    private ContextMenu BuildContextMenu()
    {
        var menu = new ContextMenu();
        menu.Opened += (_, _) => PopulateContextMenu(menu);
        return menu;
    }

    private void PopulateContextMenu(ContextMenu menu)
    {
        menu.Items.Clear();

        var pending = _todoStore.Items
            .Where(item => !item.IsDone)
            .OrderBy(item => item.DueAt)
            .Take(4)
            .ToList();

        if (pending.Count == 0)
        {
            menu.Items.Add(new MenuItem { Header = "还没有待办", IsEnabled = false });
        }
        else
        {
            foreach (var item in pending)
            {
                menu.Items.Add(new MenuItem
                {
                    Header = $"{item.DueAt:MM-dd HH:mm}  {item.Title}",
                    IsEnabled = false
                });
            }
        }

        menu.Items.Add(new Separator());
        menu.Items.Add(MenuItem("添加待办...", AddTodo));
        menu.Items.Add(MenuItem("查看待办...", ShowTodos));
        menu.Items.Add(MenuItem("测试提醒声音", TestReminder));
        menu.Items.Add(MenuItem("设置大小和音量...", ShowSettings));
        menu.Items.Add(new Separator());
        menu.Items.Add(MenuItem(_keyboardMode ? "关闭迷你键盘" : "打开迷你键盘", ToggleMiniKeyboard));
        menu.Items.Add(MenuItem("让菊花梨散步", () => Dispatcher.BeginInvoke(() => StartStroll(180, 460, 2200, 5200))));
        menu.Items.Add(MenuItem("躲到任务栏下", () => Dispatcher.BeginInvoke(StartTaskbarPeek)));
        menu.Items.Add(MenuItem("开心跳一下", () => Dispatcher.BeginInvoke(() => PlayIdleAction(PetAnimationState.Jumping, PetSoundCue.Happy, TimeSpan.FromMilliseconds(1700), 1))));
        menu.Items.Add(new Separator());
        menu.Items.Add(MenuItem("退出菊花梨", Close));
    }

    private static MenuItem MenuItem(string header, Action action)
    {
        var item = new MenuItem { Header = header };
        item.Click += (_, _) => action();
        return item;
    }

    private void AddTodo()
    {
        CancelStroll();
        _soundPlayer.Play(PetSoundCue.Curious);
        _animator?.PlayTemporary(PetAnimationState.Waiting, TimeSpan.FromMilliseconds(1600));
        _modalOpen = true;
        try
        {
            var dialog = new TodoDialog { Owner = this };
            if (dialog.ShowDialog() == true && dialog.CreatedItem != null)
            {
                _todoStore.Add(dialog.CreatedItem);
                _soundPlayer.Play(PetSoundCue.Confirm);
                _animator?.PlayTemporary(PetAnimationState.Review, TimeSpan.FromMilliseconds(1300));
            }
        }
        finally
        {
            _modalOpen = false;
            SetBusy(TimeSpan.FromSeconds(2));
        }
    }

    private void ShowTodos()
    {
        CancelStroll();
        _soundPlayer.Play(PetSoundCue.Curious);
        _animator?.PlayTemporary(PetAnimationState.Review, TimeSpan.FromMilliseconds(1300));
        _modalOpen = true;
        try
        {
            new TodosWindow(_todoStore) { Owner = this }.ShowDialog();
        }
        finally
        {
            _modalOpen = false;
            SetBusy(TimeSpan.FromSeconds(2));
        }
    }

    private void TestReminder()
    {
        CancelStroll();
        SetBusy(TimeSpan.FromMilliseconds(2400));
        _soundPlayer.Play(PetSoundCue.Reminder);
        _animator?.PlayTemporary(PetAnimationState.Jumping, TimeSpan.FromMilliseconds(1800));
    }

    private void ShowSettings()
    {
        CancelStroll();
        _modalOpen = true;
        try
        {
            var settingsWindow = new SettingsWindow(
                _settingsStore,
                value => ApplyPetScale(value),
                value => _soundPlayer.Volume = value,
                () => _soundPlayer.Play(PetSoundCue.Tap))
            {
                Owner = this
            };
            settingsWindow.ShowDialog();
        }
        finally
        {
            _modalOpen = false;
            SetBusy(TimeSpan.FromSeconds(1));
        }
    }

    private void PlayPetVoice()
    {
        CancelStroll();
        var isHappyTap = _random.NextDouble() < 0.35;
        _soundPlayer.Play(isHappyTap ? PetSoundCue.Happy : PetSoundCue.Tap);
        _animator?.PlayTemporary(
            isHappyTap ? PetAnimationState.Jumping : PetAnimationState.Waving,
            TimeSpan.FromMilliseconds(isHappyTap ? 1700 : 1500));
        SetBusy(TimeSpan.FromMilliseconds(2200));
    }

    private void CheckReminders()
    {
        if (_activeReminder != null)
        {
            return;
        }

        var due = _todoStore.DueItems().FirstOrDefault();
        if (due == null)
        {
            return;
        }

        due.Notified = true;
        _todoStore.Save();
        ShowReminder(due);
    }

    private void ShowReminder(TodoItem item)
    {
        CancelStroll();
        SetBusy(TimeSpan.FromSeconds(5));
        _soundPlayer.Play(PetSoundCue.Reminder);
        _animator?.PlayTemporary(PetAnimationState.Jumping, TimeSpan.FromMilliseconds(2400));

        var reminder = new ReminderWindow(item) { Owner = this };
        reminder.Left = Math.Max(0, Left + Width / 2 - reminder.Width / 2);
        reminder.Top = Math.Max(0, Top - reminder.Height - 12);
        reminder.Closed += (_, _) =>
        {
            ApplyReminderResult(item, reminder.Result);
            _activeReminder = null;
        };
        _activeReminder = reminder;
        reminder.Show();
        reminder.Activate();
    }

    private void ApplyReminderResult(TodoItem item, ReminderResult result)
    {
        switch (result)
        {
            case ReminderResult.Done:
                item.IsDone = true;
                item.Notified = true;
                _soundPlayer.Play(PetSoundCue.Happy);
                _animator?.PlayTemporary(PetAnimationState.Waving, TimeSpan.FromMilliseconds(1400));
                break;
            case ReminderResult.Snooze:
                item.DueAt = DateTime.Now.AddMinutes(5);
                item.Notified = false;
                _soundPlayer.Play(PetSoundCue.Nudge);
                _animator?.PlayTemporary(PetAnimationState.Waiting, TimeSpan.FromMilliseconds(1200));
                break;
            case ReminderResult.None:
            case ReminderResult.Dismiss:
            default:
                item.Notified = true;
                _animator?.PlayTemporary(PetAnimationState.Idle, TimeSpan.FromMilliseconds(300));
                break;
        }

        _todoStore.Save();
        SetBusy(TimeSpan.FromSeconds(2));
    }

    private void MaybeRunIdleBehavior()
    {
        _idleBehaviorTimer.Interval = TimeSpan.FromSeconds(_random.Next(4, 10));
        if (IsInteractionBusy())
        {
            return;
        }

        var roll = _random.Next(100);
        if (roll < 42)
        {
            StartStroll(120, 380, 1800, 4300);
        }
        else if (roll < 52)
        {
            StartTaskbarPeek();
        }
        else if (roll < 64)
        {
            PlayIdleAction(PetAnimationState.Waving, PetSoundCue.Greeting, TimeSpan.FromMilliseconds(1500), 0.55);
        }
        else if (roll < 76)
        {
            PlayIdleAction(PetAnimationState.Jumping, PetSoundCue.Happy, TimeSpan.FromMilliseconds(1700), 0.5);
        }
        else if (roll < 89)
        {
            PlayIdleAction(PetAnimationState.Review, PetSoundCue.Curious, TimeSpan.FromMilliseconds(1400), 0.45);
        }
        else
        {
            PlayIdleAction(PetAnimationState.Waiting, PetSoundCue.Nudge, TimeSpan.FromMilliseconds(1500), 0.4);
        }
    }

    private void PlayIdleAction(
        PetAnimationState animationState,
        PetSoundCue soundCue,
        TimeSpan duration,
        double soundChance)
    {
        if (_random.NextDouble() < soundChance)
        {
            _soundPlayer.Play(soundCue);
        }

        _animator?.PlayTemporary(animationState, duration);
        SetBusy(duration + TimeSpan.FromSeconds(1.2));
    }

    private void StartStroll(int minDistance = 90, int maxDistance = 221, int minDurationMs = 1600, int maxDurationMs = 2800)
    {
        if (IsInteractionBusy())
        {
            return;
        }

        var workArea = SystemParameters.WorkArea;
        var direction = _random.Next(2) == 0 ? -1 : 1;
        var distance = _random.Next(minDistance, maxDistance + 1) * direction;
        var minLeft = workArea.Left + 8;
        var maxLeft = workArea.Right - Width - 8;
        var targetLeft = Math.Clamp(Left + distance, minLeft, maxLeft);
        if (Math.Abs(targetLeft - Left) < 24)
        {
            targetLeft = Math.Clamp(Left - distance, minLeft, maxLeft);
            direction *= -1;
        }

        if (Math.Abs(targetLeft - Left) < 24)
        {
            return;
        }

        _isStrolling = true;
        _strollStartedAt = DateTime.Now;
        _strollDuration = TimeSpan.FromMilliseconds(_random.Next(minDurationMs, maxDurationMs + 1));
        _strollStartLeft = Left;
        _strollTargetLeft = targetLeft;
        _animator?.SetState(direction > 0 ? PetAnimationState.RunningRight : PetAnimationState.RunningLeft);
        if (_random.NextDouble() < 0.65)
        {
            _soundPlayer.Play(PetSoundCue.Stroll);
        }

        _strollTimer.Start();
    }

    private async void StartTaskbarPeek()
    {
        if (IsInteractionBusy())
        {
            return;
        }

        CancelStroll();
        var workArea = SystemParameters.WorkArea;
        var originalTop = Top;
        var peekTop = workArea.Bottom - Height * 0.42;
        if (peekTop <= originalTop + 18)
        {
            peekTop = Math.Min(SystemParameters.PrimaryScreenHeight - Height * 0.42, originalTop + Height * 0.45);
        }

        _isHidingNearTaskbar = true;
        SetBusy(TimeSpan.FromSeconds(5));
        _soundPlayer.Play(PetSoundCue.Curious);
        _animator?.SetState(PetAnimationState.Waiting);

        await AnimateTopAsync(peekTop, TimeSpan.FromMilliseconds(680));
        if (!_isHidingNearTaskbar)
        {
            return;
        }

        Topmost = false;
        await Task.Delay(_random.Next(1200, 2300));
        Topmost = true;
        _soundPlayer.Play(PetSoundCue.Happy);
        _animator?.PlayTemporary(PetAnimationState.Jumping, TimeSpan.FromMilliseconds(900));
        await AnimateTopAsync(originalTop, TimeSpan.FromMilliseconds(620));
        _isHidingNearTaskbar = false;
        _animator?.SetState(PetAnimationState.Idle);
    }

    private async Task AnimateTopAsync(double targetTop, TimeSpan duration)
    {
        var startTop = Top;
        var start = DateTime.Now;
        while ((DateTime.Now - start) < duration)
        {
            if (_dragging)
            {
                _isHidingNearTaskbar = false;
                return;
            }

            var progress = Math.Clamp((DateTime.Now - start).TotalMilliseconds / duration.TotalMilliseconds, 0, 1);
            var eased = 0.5 - Math.Cos(progress * Math.PI) / 2;
            Top = startTop + (targetTop - startTop) * eased;
            await Task.Delay(16);
        }

        Top = targetTop;
    }

    private void AdvanceStroll()
    {
        if (!_isStrolling)
        {
            _strollTimer.Stop();
            return;
        }

        var progress = Math.Clamp((DateTime.Now - _strollStartedAt).TotalMilliseconds / _strollDuration.TotalMilliseconds, 0, 1);
        var eased = 0.5 - Math.Cos(progress * Math.PI) / 2;
        Left = _strollStartLeft + (_strollTargetLeft - _strollStartLeft) * eased;

        if (progress < 1)
        {
            return;
        }

        _isStrolling = false;
        _strollTimer.Stop();
        _animator?.SetState(PetAnimationState.Idle);
        SetBusy(TimeSpan.FromSeconds(1.8));
    }

    private void CancelStroll()
    {
        if (!_isStrolling)
        {
            return;
        }

        _isStrolling = false;
        _strollTimer.Stop();
        _animator?.SetState(PetAnimationState.Idle);
    }

    private void ToggleMiniKeyboard()
    {
        if (_keyboardMode)
        {
            StopMiniKeyboardMode();
        }
        else
        {
            StartMiniKeyboardMode();
        }
    }

    private void StartMiniKeyboardMode()
    {
        if (_keyboardMode)
        {
            return;
        }

        CancelStroll();
        _isHidingNearTaskbar = false;
        _keyboardMode = true;
        _miniKeyboard = new MiniKeyboardWindow { Owner = this };
        _miniKeyboard.Show();
        _keyboardHook = new GlobalKeyboardHook();
        _keyboardHook.KeyPressed += OnGlobalKeyPressed;

        ApplyPetScale(Math.Min(_settingsStore.Settings.PetScale, 0.72), preserveAnchor: false);
        Dispatcher.BeginInvoke(() =>
        {
            if (_miniKeyboard?.TryPressKey(Key.Space, out var center) == true)
            {
                StartKeyboardHop(center, quiet: true);
            }
        });
        _soundPlayer.Play(PetSoundCue.Greeting);
        _animator?.PlayTemporary(PetAnimationState.Waving, TimeSpan.FromMilliseconds(1300));
        SetBusy(TimeSpan.FromSeconds(1.2));
    }

    private void StopMiniKeyboardMode()
    {
        _keyboardMode = false;
        _isKeyboardHopping = false;
        _keyboardHopTimer.Stop();
        if (_keyboardHook != null)
        {
            _keyboardHook.KeyPressed -= OnGlobalKeyPressed;
            _keyboardHook.Dispose();
            _keyboardHook = null;
        }

        _miniKeyboard?.Close();
        _miniKeyboard = null;
        ApplyPetScale(_settingsStore.Settings.PetScale);
        _animator?.SetState(PetAnimationState.Idle);
        SetBusy(TimeSpan.FromSeconds(1));
    }

    private void OnGlobalKeyPressed(object? sender, Key key)
    {
        if (!_keyboardMode || _miniKeyboard == null)
        {
            return;
        }

        if (_miniKeyboard.TryPressKey(key, out var center))
        {
            StartKeyboardHop(center);
        }
    }

    private void StartKeyboardHop(Point keyCenterOnScreen, bool quiet = false)
    {
        if (!_keyboardMode)
        {
            return;
        }

        CancelStroll();
        _keyboardHopStartLeft = Left;
        _keyboardHopStartTop = Top;
        _keyboardHopTargetLeft = keyCenterOnScreen.X - Width / 2;
        _keyboardHopTargetTop = keyCenterOnScreen.Y - Height + 24;
        _keyboardHopStartedAt = DateTime.Now;
        _keyboardHopDuration = TimeSpan.FromMilliseconds(220);
        _isKeyboardHopping = true;
        _animator?.PlayTemporary(PetAnimationState.Jumping, TimeSpan.FromMilliseconds(420));
        if (!quiet && _random.NextDouble() < 0.16)
        {
            _soundPlayer.Play(PetSoundCue.Tap);
        }

        _keyboardHopTimer.Start();
    }

    private void AdvanceKeyboardHop()
    {
        if (!_isKeyboardHopping)
        {
            _keyboardHopTimer.Stop();
            return;
        }

        var progress = Math.Clamp((DateTime.Now - _keyboardHopStartedAt).TotalMilliseconds / _keyboardHopDuration.TotalMilliseconds, 0, 1);
        var eased = 0.5 - Math.Cos(progress * Math.PI) / 2;
        var arc = Math.Sin(progress * Math.PI) * 22;
        Left = _keyboardHopStartLeft + (_keyboardHopTargetLeft - _keyboardHopStartLeft) * eased;
        Top = _keyboardHopStartTop + (_keyboardHopTargetTop - _keyboardHopStartTop) * eased - arc;

        if (progress < 1)
        {
            return;
        }

        Left = _keyboardHopTargetLeft;
        Top = _keyboardHopTargetTop;
        _isKeyboardHopping = false;
        _keyboardHopTimer.Stop();
    }

    private bool IsInteractionBusy()
    {
        return _dragging
            || _isStrolling
            || _isHidingNearTaskbar
            || _keyboardMode
            || _modalOpen
            || _activeReminder != null
            || ContextMenu?.IsOpen == true
            || DateTime.Now < _busyUntil;
    }

    private void SetBusy(TimeSpan duration)
    {
        var until = DateTime.Now + duration;
        if (until > _busyUntil)
        {
            _busyUntil = until;
        }
    }

    private void ApplyPetScale(double scale, bool preserveAnchor = true)
    {
        scale = Math.Clamp(scale, 0.65, 1.7);
        var previousCenter = Left + Width / 2;
        var previousBottom = Top + Height;

        PetImage.Width = BasePetWidth * scale;
        PetImage.Height = BasePetHeight * scale;
        Width = BaseWindowWidth * scale;
        Height = BaseWindowHeight * scale;

        if (!preserveAnchor)
        {
            return;
        }

        var workArea = SystemParameters.WorkArea;
        Left = Math.Clamp(previousCenter - Width / 2, workArea.Left, Math.Max(workArea.Left, workArea.Right - Width));
        Top = Math.Clamp(previousBottom - Height, workArea.Top, Math.Max(workArea.Top, workArea.Bottom - Height));
    }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount > 1)
        {
            return;
        }

        CancelStroll();
        _isHidingNearTaskbar = false;
        SetBusy(TimeSpan.FromSeconds(1));
        _dragging = true;
        _dragStartScreen = PointToScreen(e.GetPosition(this));
        _dragStartLeft = Left;
        _dragStartTop = Top;
        _lastDragX = _dragStartScreen.X;
        CaptureMouse();
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!_dragging || e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        var screen = PointToScreen(e.GetPosition(this));
        Left = _dragStartLeft + screen.X - _dragStartScreen.X;
        Top = _dragStartTop + screen.Y - _dragStartScreen.Y;

        if (Math.Abs(screen.X - _lastDragX) > 2)
        {
            _animator?.SetState(screen.X >= _lastDragX ? PetAnimationState.RunningRight : PetAnimationState.RunningLeft);
            _lastDragX = screen.X;
        }
    }

    private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_dragging)
        {
            return;
        }

        _dragging = false;
        ReleaseMouseCapture();
        _animator?.SetState(PetAnimationState.Idle);
        SetBusy(TimeSpan.FromSeconds(2));
    }

    protected override void OnClosed(EventArgs e)
    {
        StopMiniKeyboardMode();
        base.OnClosed(e);
    }
}
