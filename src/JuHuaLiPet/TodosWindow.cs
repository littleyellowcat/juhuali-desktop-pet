using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace JuHuaLiPet;

internal sealed class TodosWindow : Window
{
    private static readonly Brush Ink = new SolidColorBrush(Color.FromRgb(58, 47, 34));
    private static readonly Brush Muted = new SolidColorBrush(Color.FromRgb(116, 98, 74));
    private static readonly Brush WarmPanel = new SolidColorBrush(Color.FromRgb(255, 250, 239));
    private static readonly Brush WarmBorder = new SolidColorBrush(Color.FromRgb(247, 212, 145));
    private static readonly Brush SelectedBorder = new SolidColorBrush(Color.FromRgb(255, 173, 42));
    private static readonly Brush Accent = new SolidColorBrush(Color.FromRgb(245, 143, 35));

    private readonly TodoStore _store;
    private readonly StackPanel _listPanel = new();
    private readonly TextBlock _summaryText = new();
    private readonly Button _doneButton;
    private readonly Button _snoozeButton;
    private readonly Button _deleteButton;
    private TodoItem? _selected;

    public TodosWindow(TodoStore store)
    {
        _store = store;
        Title = "菊花梨的待办";
        Width = 560;
        Height = 500;
        MinWidth = 500;
        MinHeight = 420;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Topmost = true;
        Background = new SolidColorBrush(Color.FromRgb(255, 247, 229));

        var root = new Grid { Margin = new Thickness(18) };
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        root.Children.Add(BuildHeader());

        var scroll = new ScrollViewer
        {
            Content = _listPanel,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Margin = new Thickness(0, 16, 0, 14)
        };
        Grid.SetRow(scroll, 1);
        root.Children.Add(scroll);

        var actions = new DockPanel();
        Grid.SetRow(actions, 2);
        _deleteButton = ActionButton("删除", Delete);
        DockPanel.SetDock(_deleteButton, Dock.Left);
        actions.Children.Add(_deleteButton);

        var rightActions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        _snoozeButton = ActionButton("10 分钟后提醒", Snooze);
        _doneButton = ActionButton("完成", MarkDone, accent: true);
        rightActions.Children.Add(_snoozeButton);
        rightActions.Children.Add(_doneButton);
        rightActions.Children.Add(ActionButton("关闭", Close));
        actions.Children.Add(rightActions);
        root.Children.Add(actions);

        Content = root;
        Refresh();
    }

    private UIElement BuildHeader()
    {
        var header = new DockPanel();
        var titleStack = new StackPanel();
        titleStack.Children.Add(new TextBlock
        {
            Text = "待办事项",
            FontSize = 24,
            FontWeight = FontWeights.Bold,
            Foreground = Ink
        });
        _summaryText.FontSize = 13;
        _summaryText.Foreground = Muted;
        _summaryText.Margin = new Thickness(0, 4, 0, 0);
        titleStack.Children.Add(_summaryText);
        header.Children.Add(titleStack);
        return header;
    }

    private Button ActionButton(string text, Action action, bool accent = false)
    {
        var button = new Button
        {
            Content = text,
            MinWidth = 84,
            Height = 34,
            Margin = new Thickness(8, 0, 0, 0),
            Padding = new Thickness(12, 0, 12, 0),
            Background = accent ? Accent : new SolidColorBrush(Color.FromRgb(255, 239, 208)),
            Foreground = accent ? Brushes.White : Ink,
            BorderBrush = accent ? Accent : WarmBorder,
            BorderThickness = new Thickness(1)
        };
        button.Click += (_, _) => action();
        return button;
    }

    private TodoItem? Selected => _selected;

    private void MarkDone()
    {
        if (Selected == null)
        {
            return;
        }

        Selected.IsDone = true;
        Selected.Notified = true;
        _store.Save();
        Refresh(Selected.Id);
    }

    private void Snooze()
    {
        if (Selected == null)
        {
            return;
        }

        Selected.DueAt = DateTime.Now.AddMinutes(10);
        Selected.Notified = false;
        _store.Save();
        Refresh(Selected.Id);
    }

    private void Delete()
    {
        if (Selected == null)
        {
            return;
        }

        var nextSelectionId = Selected.Id;
        _store.Items.Remove(Selected);
        _store.Save();
        Refresh(nextSelectionId);
    }

    private void Refresh(Guid? preferredSelectionId = null)
    {
        _listPanel.Children.Clear();

        var orderedItems = _store.Items
            .OrderBy(item => item.IsDone)
            .ThenBy(item => item.DueAt)
            .ToList();

        var pendingCount = orderedItems.Count(item => !item.IsDone);
        var dueCount = orderedItems.Count(item => !item.IsDone && item.DueAt <= DateTime.Now);
        _summaryText.Text = pendingCount == 0
            ? "现在没有待办，菊花梨可以安心溜达。"
            : $"{pendingCount} 个待办，{dueCount} 个已经到点。";

        _selected = orderedItems.FirstOrDefault(item => item.Id == preferredSelectionId)
            ?? orderedItems.FirstOrDefault(item => !item.IsDone)
            ?? orderedItems.FirstOrDefault();

        if (orderedItems.Count == 0)
        {
            _listPanel.Children.Add(EmptyState());
        }
        else
        {
            foreach (var item in orderedItems)
            {
                _listPanel.Children.Add(TodoCard(item, item.Id == _selected?.Id));
            }
        }

        UpdateButtons();
    }

    private UIElement EmptyState()
    {
        return new Border
        {
            Padding = new Thickness(22),
            CornerRadius = new CornerRadius(8),
            BorderBrush = WarmBorder,
            BorderThickness = new Thickness(1),
            Background = WarmPanel,
            Child = new TextBlock
            {
                Text = "还没有待办。右键菊花梨，选择“添加待办...”开始安排吧。",
                FontSize = 14,
                Foreground = Muted,
                TextWrapping = TextWrapping.Wrap
            }
        };
    }

    private UIElement TodoCard(TodoItem item, bool selected)
    {
        var card = new Border
        {
            Padding = new Thickness(14),
            Margin = new Thickness(0, 0, 0, 10),
            CornerRadius = new CornerRadius(8),
            BorderThickness = selected ? new Thickness(2) : new Thickness(1),
            BorderBrush = selected ? SelectedBorder : WarmBorder,
            Background = item.IsDone
                ? new SolidColorBrush(Color.FromRgb(249, 247, 241))
                : WarmPanel,
            Cursor = System.Windows.Input.Cursors.Hand
        };
        card.MouseLeftButtonDown += (_, _) =>
        {
            _selected = item;
            Refresh(item.Id);
        };

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var textStack = new StackPanel();
        textStack.Children.Add(new TextBlock
        {
            Text = item.Title,
            FontSize = 16,
            FontWeight = FontWeights.SemiBold,
            Foreground = item.IsDone ? Muted : Ink,
            TextWrapping = TextWrapping.Wrap
        });
        textStack.Children.Add(new TextBlock
        {
            Text = item.DueAt.ToString("MM-dd HH:mm"),
            FontSize = 13,
            Foreground = Muted,
            Margin = new Thickness(0, 6, 0, 0)
        });
        grid.Children.Add(textStack);

        var status = StatusPill(item);
        Grid.SetColumn(status, 1);
        grid.Children.Add(status);

        card.Child = grid;
        return card;
    }

    private UIElement StatusPill(TodoItem item)
    {
        var (text, background, foreground) = item.IsDone
            ? ("已完成", Color.FromRgb(232, 236, 226), Color.FromRgb(83, 102, 73))
            : item.DueAt <= DateTime.Now
                ? ("到点了", Color.FromRgb(255, 225, 213), Color.FromRgb(181, 66, 34))
                : ("待提醒", Color.FromRgb(255, 238, 197), Color.FromRgb(160, 96, 24));

        return new Border
        {
            Padding = new Thickness(10, 4, 10, 4),
            CornerRadius = new CornerRadius(8),
            Background = new SolidColorBrush(background),
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(12, 0, 0, 0),
            Child = new TextBlock
            {
                Text = text,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(foreground)
            }
        };
    }

    private void UpdateButtons()
    {
        var hasSelection = Selected != null;
        _deleteButton.IsEnabled = hasSelection;
        _snoozeButton.IsEnabled = hasSelection && !Selected!.IsDone;
        _doneButton.IsEnabled = hasSelection && !Selected!.IsDone;
    }
}
