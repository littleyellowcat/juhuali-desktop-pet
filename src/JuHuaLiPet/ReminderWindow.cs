using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace JuHuaLiPet;

internal enum ReminderResult
{
    None,
    Done,
    Snooze,
    Dismiss
}

internal sealed class ReminderWindow : Window
{
    private static readonly Brush Ink = new SolidColorBrush(Color.FromRgb(58, 47, 34));
    private static readonly Brush Muted = new SolidColorBrush(Color.FromRgb(116, 98, 74));
    private static readonly Brush Accent = new SolidColorBrush(Color.FromRgb(245, 143, 35));
    private static readonly Brush WarmBorder = new SolidColorBrush(Color.FromRgb(247, 212, 145));

    public ReminderWindow(TodoItem item)
    {
        Title = "菊花梨提醒你";
        Width = 380;
        Height = 190;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.Manual;
        Topmost = true;
        ShowInTaskbar = false;
        Background = new SolidColorBrush(Color.FromRgb(255, 247, 229));

        var root = new Border
        {
            Padding = new Thickness(18),
            Background = new SolidColorBrush(Color.FromRgb(255, 250, 239)),
            BorderBrush = WarmBorder,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(8)
        };

        var stack = new StackPanel();
        stack.Children.Add(new TextBlock
        {
            Text = "提醒到点了",
            FontWeight = FontWeights.Bold,
            FontSize = 20,
            Foreground = Ink,
            Margin = new Thickness(0, 0, 0, 4)
        });
        stack.Children.Add(new TextBlock
        {
            Text = "主人，该做这件事啦：",
            FontSize = 13,
            Foreground = Muted,
            Margin = new Thickness(0, 0, 0, 10)
        });
        stack.Children.Add(new TextBlock
        {
            Text = item.Title,
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            Foreground = Ink,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 18)
        });

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        buttons.Children.Add(Button("5 分钟后", ReminderResult.Snooze));
        buttons.Children.Add(Button("知道了", ReminderResult.Dismiss));
        buttons.Children.Add(Button("完成", ReminderResult.Done, accent: true));
        stack.Children.Add(buttons);
        root.Child = stack;
        Content = root;
    }

    public ReminderResult Result { get; private set; }

    private Button Button(string text, ReminderResult result, bool accent = false)
    {
        var button = new Button
        {
            Content = text,
            MinWidth = 82,
            Height = 32,
            Margin = new Thickness(8, 0, 0, 0),
            Padding = new Thickness(12, 0, 12, 0),
            Background = accent ? Accent : new SolidColorBrush(Color.FromRgb(255, 239, 208)),
            Foreground = accent ? Brushes.White : Ink,
            BorderBrush = accent ? Accent : WarmBorder,
            BorderThickness = new Thickness(1)
        };
        button.Click += (_, _) =>
        {
            Result = result;
            Close();
        };
        return button;
    }
}
