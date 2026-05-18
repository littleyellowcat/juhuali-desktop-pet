using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace JuHuaLiPet;

internal sealed class MiniKeyboardWindow : Window
{
    private static readonly Brush PanelBrush = new SolidColorBrush(Color.FromRgb(255, 248, 232));
    private static readonly Brush KeyBrush = new SolidColorBrush(Color.FromRgb(255, 238, 201));
    private static readonly Brush KeyBorderBrush = new SolidColorBrush(Color.FromRgb(236, 197, 126));
    private static readonly Brush ActiveKeyBrush = new SolidColorBrush(Color.FromRgb(255, 172, 42));
    private static readonly Brush InkBrush = new SolidColorBrush(Color.FromRgb(63, 47, 28));

    private readonly Dictionary<Key, Border> _keyPanels = [];
    private readonly DispatcherTimer _highlightTimer = new() { Interval = TimeSpan.FromMilliseconds(240) };
    private Border? _activeKey;

    public MiniKeyboardWindow()
    {
        Title = "菊花梨迷你键盘";
        Width = 704;
        Height = 240;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.Manual;
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        ShowInTaskbar = false;
        ShowActivated = false;
        Topmost = true;
        Content = BuildContent();
        Loaded += (_, _) => PlaceNearBottom();
        MouseLeftButtonDown += (_, e) =>
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                DragMove();
            }
        };
        _highlightTimer.Tick += (_, _) => ClearHighlight();
    }

    public static Key NormalizeKey(Key key)
    {
        return key switch
        {
            Key.RightShift => Key.LeftShift,
            Key.RightCtrl => Key.LeftCtrl,
            Key.RightAlt => Key.LeftAlt,
            Key.System => Key.LeftAlt,
            _ => key
        };
    }

    public void PlaceNearBottom()
    {
        var workArea = SystemParameters.WorkArea;
        Left = workArea.Left + Math.Max(16, (workArea.Width - Width) / 2);
        Top = workArea.Bottom - Height - 18;
    }

    public bool TryPressKey(Key key, out Point keyCenterOnScreen)
    {
        key = NormalizeKey(key);
        if (!_keyPanels.TryGetValue(key, out var panel))
        {
            keyCenterOnScreen = default;
            return false;
        }

        Highlight(panel);
        keyCenterOnScreen = panel.PointToScreen(new Point(panel.ActualWidth / 2, panel.ActualHeight / 2));
        return true;
    }

    private UIElement BuildContent()
    {
        var border = new Border
        {
            Padding = new Thickness(16),
            CornerRadius = new CornerRadius(8),
            Background = PanelBrush,
            BorderBrush = new SolidColorBrush(Color.FromRgb(245, 184, 75)),
            BorderThickness = new Thickness(1)
        };

        var root = new Grid();
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        root.Children.Add(new TextBlock
        {
            Text = "迷你键盘",
            FontSize = 15,
            FontWeight = FontWeights.Bold,
            Foreground = InkBrush,
            Margin = new Thickness(2, 0, 0, 8)
        });

        var canvas = new Canvas { Width = 660, Height = 174 };
        Grid.SetRow(canvas, 1);
        root.Children.Add(canvas);
        border.Child = root;

        AddRow(canvas, 0, 0, [("Esc", Key.Escape, 48), ("1", Key.D1, 42), ("2", Key.D2, 42), ("3", Key.D3, 42), ("4", Key.D4, 42), ("5", Key.D5, 42), ("6", Key.D6, 42), ("7", Key.D7, 42), ("8", Key.D8, 42), ("9", Key.D9, 42), ("0", Key.D0, 42), ("Back", Key.Back, 70)]);
        AddRow(canvas, 22, 42, [("Tab", Key.Tab, 58), ("Q", Key.Q, 42), ("W", Key.W, 42), ("E", Key.E, 42), ("R", Key.R, 42), ("T", Key.T, 42), ("Y", Key.Y, 42), ("U", Key.U, 42), ("I", Key.I, 42), ("O", Key.O, 42), ("P", Key.P, 42)]);
        AddRow(canvas, 48, 84, [("A", Key.A, 42), ("S", Key.S, 42), ("D", Key.D, 42), ("F", Key.F, 42), ("G", Key.G, 42), ("H", Key.H, 42), ("J", Key.J, 42), ("K", Key.K, 42), ("L", Key.L, 42), ("Enter", Key.Return, 78)]);
        AddRow(canvas, 0, 126, [("Ctrl", Key.LeftCtrl, 58), ("Alt", Key.LeftAlt, 52), ("Space", Key.Space, 260), ("Shift", Key.LeftShift, 70), ("Z", Key.Z, 42), ("X", Key.X, 42), ("C", Key.C, 42), ("V", Key.V, 42)]);

        return border;
    }

    private void AddRow(Canvas canvas, double startX, double y, IEnumerable<(string Label, Key Key, double Width)> keys)
    {
        var x = startX;
        foreach (var (label, key, width) in keys)
        {
            AddKey(canvas, label, key, x, y, width);
            x += width + 8;
        }
    }

    private void AddKey(Canvas canvas, string label, Key key, double x, double y, double width)
    {
        var panel = new Border
        {
            Width = width,
            Height = 34,
            CornerRadius = new CornerRadius(6),
            Background = KeyBrush,
            BorderBrush = KeyBorderBrush,
            BorderThickness = new Thickness(1),
            Child = new TextBlock
            {
                Text = label,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = InkBrush,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };

        Canvas.SetLeft(panel, x);
        Canvas.SetTop(panel, y);
        canvas.Children.Add(panel);
        _keyPanels[key] = panel;
    }

    private void Highlight(Border panel)
    {
        ClearHighlight();
        _activeKey = panel;
        panel.Background = ActiveKeyBrush;
        panel.BorderBrush = ActiveKeyBrush;
        if (panel.Child is TextBlock label)
        {
            label.Foreground = Brushes.White;
        }

        _highlightTimer.Stop();
        _highlightTimer.Start();
    }

    private void ClearHighlight()
    {
        _highlightTimer.Stop();
        if (_activeKey == null)
        {
            return;
        }

        _activeKey.Background = KeyBrush;
        _activeKey.BorderBrush = KeyBorderBrush;
        if (_activeKey.Child is TextBlock label)
        {
            label.Foreground = InkBrush;
        }

        _activeKey = null;
    }
}
