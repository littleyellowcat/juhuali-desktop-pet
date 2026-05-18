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
    private SpriteAnimator? _animator;
    private ReminderWindow? _activeReminder;
    private bool _dragging;
    private bool _isStrolling;
    private bool _modalOpen;
    private DateTime _busyUntil = DateTime.MinValue;
    private DateTime _strollStartedAt;
    private TimeSpan _strollDuration;
    private Point _dragStartScreen;
    private double _dragStartLeft;
    private double _dragStartTop;
    private double _lastDragX;
    private double _strollStartLeft;
    private double _strollStartTop;
    private double _strollTargetLeft;

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
        menu.Items.Add(MenuItem("让菊花梨踱步", () => Dispatcher.BeginInvoke(() => StartStroll(100, 250, 1900, 3400))));
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
        if (roll < 30)
        {
            StartStroll(80, 220, 2200, 4200);
        }
        else if (roll < 48)
        {
            PlayIdleAction(PetAnimationState.Waving, PetSoundCue.Greeting, TimeSpan.FromMilliseconds(1500), 0.55);
        }
        else if (roll < 66)
        {
            PlayIdleAction(PetAnimationState.Jumping, PetSoundCue.Happy, TimeSpan.FromMilliseconds(1700), 0.5);
        }
        else if (roll < 84)
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

    private void StartStroll(int minDistance = 80, int maxDistance = 220, int minDurationMs = 2200, int maxDurationMs = 4200)
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
        _strollStartTop = Top;
        _strollTargetLeft = targetLeft;
        _animator?.SetState(direction > 0 ? PetAnimationState.RunningRight : PetAnimationState.RunningLeft);
        if (_random.NextDouble() < 0.28)
        {
            _soundPlayer.Play(PetSoundCue.Stroll);
        }

        _strollTimer.Start();
    }

    private void AdvanceStroll()
    {
        if (!_isStrolling)
        {
            _strollTimer.Stop();
            return;
        }

        var progress = Math.Clamp((DateTime.Now - _strollStartedAt).TotalMilliseconds / _strollDuration.TotalMilliseconds, 0, 1);
        var distance = Math.Abs(_strollTargetLeft - _strollStartLeft);
        var stepCount = Math.Max(3, (int)Math.Round(distance / 24));
        var stepPhase = progress * stepCount;
        var stepIndex = Math.Floor(stepPhase);
        var stepFraction = stepPhase - stepIndex;
        var easedStep = stepFraction * stepFraction * (3 - 2 * stepFraction);
        var walked = (stepIndex + easedStep) / stepCount;
        walked = Math.Clamp(walked, 0, 1);
        var direction = _strollTargetLeft >= _strollStartLeft ? 1 : -1;
        var sway = Math.Sin(stepPhase * Math.PI * 2) * 2.4;
        var bob = Math.Max(0, Math.Sin(stepPhase * Math.PI)) * 5.5;
        Left = _strollStartLeft + (_strollTargetLeft - _strollStartLeft) * walked + sway * direction;
        Top = _strollStartTop - bob;

        if (progress < 1)
        {
            return;
        }

        _isStrolling = false;
        _strollTimer.Stop();
        Top = _strollStartTop;
        Left = _strollTargetLeft;
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
        Top = _strollStartTop;
        _animator?.SetState(PetAnimationState.Idle);
    }

    private bool IsInteractionBusy()
    {
        return _dragging
            || _isStrolling
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
}
