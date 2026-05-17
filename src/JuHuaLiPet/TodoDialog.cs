using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace JuHuaLiPet;

internal sealed class TodoDialog : Window
{
    private static readonly Brush Ink = new SolidColorBrush(Color.FromRgb(58, 47, 34));
    private static readonly Brush Muted = new SolidColorBrush(Color.FromRgb(116, 98, 74));
    private static readonly Brush Accent = new SolidColorBrush(Color.FromRgb(245, 143, 35));

    private readonly TextBox _titleBox;
    private readonly DatePicker _datePicker;
    private readonly TextBox _timeBox;

    public TodoDialog()
    {
        Title = "添加待办";
        Width = 460;
        Height = 360;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Topmost = true;
        Background = new SolidColorBrush(Color.FromRgb(255, 247, 229));

        var dueAt = DateTime.Now.AddMinutes(30);
        _titleBox = TextBox();
        _datePicker = DatePicker(dueAt.Date);
        _timeBox = TextBox(dueAt.ToString("HH:mm", CultureInfo.InvariantCulture), bottomMargin: 0);

        var root = new Grid { Margin = new Thickness(22) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var header = new StackPanel();
        header.Children.Add(new TextBlock
        {
            Text = "添加待办",
            FontSize = 24,
            FontWeight = FontWeights.Bold,
            Foreground = Ink
        });
        header.Children.Add(new TextBlock
        {
            Text = "告诉菊花梨什么时候提醒你。",
            FontSize = 13,
            Foreground = Muted,
            Margin = new Thickness(0, 4, 0, 18)
        });
        root.Children.Add(header);

        var form = new Border
        {
            Padding = new Thickness(14),
            CornerRadius = new CornerRadius(8),
            Background = new SolidColorBrush(Color.FromRgb(255, 250, 239)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(247, 212, 145)),
            BorderThickness = new Thickness(1)
        };
        Grid.SetRow(form, 1);

        var fields = new StackPanel();
        fields.Children.Add(Label("待办内容"));
        fields.Children.Add(_titleBox);

        var dateTimeGrid = new Grid { Margin = new Thickness(0, 0, 0, 8) };
        dateTimeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        dateTimeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
        dateTimeGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) });

        var dateStack = new StackPanel();
        dateStack.Children.Add(Label("日期"));
        dateStack.Children.Add(_datePicker);
        Grid.SetColumn(dateStack, 0);
        dateTimeGrid.Children.Add(dateStack);

        var timeStack = new StackPanel();
        timeStack.Children.Add(Label("时间"));
        timeStack.Children.Add(_timeBox);
        Grid.SetColumn(timeStack, 2);
        dateTimeGrid.Children.Add(timeStack);

        fields.Children.Add(dateTimeGrid);
        fields.Children.Add(new TextBlock
        {
            Text = "使用 24 小时制，例如 09:05 或 21:30。",
            FontSize = 12,
            Foreground = Muted,
            Margin = new Thickness(0, 4, 0, 0)
        });
        form.Child = fields;
        root.Children.Add(form);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 16, 0, 0)
        };
        Grid.SetRow(buttons, 2);
        buttons.Children.Add(ActionButton("取消", () => DialogResult = false));
        buttons.Children.Add(ActionButton("添加", Confirm, accent: true));
        root.Children.Add(buttons);

        Content = root;
        Loaded += (_, _) => _titleBox.Focus();
    }

    public TodoItem? CreatedItem { get; private set; }

    private static TextBlock Label(string text)
    {
        return new TextBlock
        {
            Text = text,
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            Foreground = Ink,
            Margin = new Thickness(0, 0, 0, 0)
        };
    }

    private static TextBox TextBox(string text = "")
    {
        return TextBox(text, bottomMargin: 12);
    }

    private static TextBox TextBox(string text, double bottomMargin)
    {
        return new TextBox
        {
            Text = text,
            Height = 34,
            FontSize = 14,
            Padding = new Thickness(8, 5, 8, 5),
            Margin = new Thickness(0, 8, 0, bottomMargin)
        };
    }

    private static DatePicker DatePicker(DateTime selectedDate)
    {
        return new DatePicker
        {
            SelectedDate = selectedDate,
            Height = 34,
            FontSize = 14,
            Margin = new Thickness(0, 8, 0, 0)
        };
    }

    private Button ActionButton(string text, Action action, bool accent = false)
    {
        var button = new Button
        {
            Content = text,
            MinWidth = 82,
            Height = 34,
            Margin = new Thickness(8, 0, 0, 0),
            Padding = new Thickness(12, 0, 12, 0),
            Background = accent ? Accent : new SolidColorBrush(Color.FromRgb(255, 239, 208)),
            Foreground = accent ? Brushes.White : Ink,
            BorderBrush = accent ? Accent : new SolidColorBrush(Color.FromRgb(247, 212, 145)),
            BorderThickness = new Thickness(1),
            IsDefault = accent
        };
        button.Click += (_, _) => action();
        return button;
    }

    private void Confirm()
    {
        var title = _titleBox.Text.Trim();
        if (title.Length == 0)
        {
            MessageBox.Show(this, "先写一下要提醒什么。", "菊花梨", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (_datePicker.SelectedDate == null ||
            !TimeSpan.TryParseExact(_timeBox.Text.Trim(), @"h\:mm", CultureInfo.InvariantCulture, out var time))
        {
            MessageBox.Show(this, "时间格式要像 9:05 或 21:30。", "菊花梨", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var dueAt = _datePicker.SelectedDate.Value.Date + time;
        if (dueAt <= DateTime.Now)
        {
            MessageBox.Show(this, "这个时间已经过去啦，给菊花梨一个未来时间。", "菊花梨", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        CreatedItem = new TodoItem
        {
            Title = title,
            DueAt = dueAt,
            IsDone = false,
            Notified = false
        };
        DialogResult = true;
    }
}
