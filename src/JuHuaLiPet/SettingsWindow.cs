using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace JuHuaLiPet;

internal sealed class SettingsWindow : Window
{
    private readonly SettingsStore _settingsStore;
    private readonly Action<double> _applyPetScale;
    private readonly Action<double> _applyVolume;
    private readonly Action _testSound;
    private readonly TextBlock _scaleValue;
    private readonly TextBlock _volumeValue;
    private readonly Slider _scaleSlider;
    private readonly Slider _volumeSlider;

    public SettingsWindow(
        SettingsStore settingsStore,
        Action<double> applyPetScale,
        Action<double> applyVolume,
        Action testSound)
    {
        _settingsStore = settingsStore;
        _applyPetScale = applyPetScale;
        _applyVolume = applyVolume;
        _testSound = testSound;

        Title = "菊花梨设置";
        Width = 390;
        Height = 310;
        ResizeMode = ResizeMode.NoResize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Topmost = true;
        ShowInTaskbar = false;
        Background = Brushes.Transparent;

        var root = new Border
        {
            Margin = new Thickness(0),
            Padding = new Thickness(20),
            CornerRadius = new CornerRadius(8),
            BorderThickness = new Thickness(1),
            BorderBrush = new SolidColorBrush(Color.FromRgb(255, 205, 98)),
            Background = new LinearGradientBrush(
                Color.FromRgb(255, 253, 246),
                Color.FromRgb(255, 239, 203),
                90)
        };

        var stack = new StackPanel();
        root.Child = stack;

        stack.Children.Add(new TextBlock
        {
            Text = "菊花梨设置",
            FontSize = 22,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(91, 60, 24)),
            Margin = new Thickness(0, 0, 0, 4)
        });
        stack.Children.Add(new TextBlock
        {
            Text = "调整大小和叫声音量，改动会自动保存。",
            FontSize = 13,
            Foreground = new SolidColorBrush(Color.FromRgb(112, 94, 68)),
            Margin = new Thickness(0, 0, 0, 18)
        });

        (_scaleSlider, _scaleValue) = AddSlider(
            stack,
            "宠物大小",
            _settingsStore.Settings.PetScale,
            0.65,
            1.7,
            value =>
            {
                _settingsStore.Settings.PetScale = value;
                _settingsStore.Save();
                _applyPetScale(value);
            });

        (_volumeSlider, _volumeValue) = AddSlider(
            stack,
            "叫声音量",
            _settingsStore.Settings.Volume,
            0,
            1,
            value =>
            {
                _settingsStore.Settings.Volume = value;
                _settingsStore.Save();
                _applyVolume(value);
            });

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 16, 0, 0)
        };

        buttons.Children.Add(ActionButton("试听", () =>
        {
            _applyVolume(_volumeSlider.Value);
            _testSound();
        }, true));
        buttons.Children.Add(ActionButton("恢复默认", Reset));
        buttons.Children.Add(ActionButton("完成", Close));
        stack.Children.Add(buttons);

        Content = root;
    }

    private (Slider Slider, TextBlock ValueLabel) AddSlider(
        Panel parent,
        string title,
        double value,
        double minimum,
        double maximum,
        Action<double> changed)
    {
        var panel = new Border
        {
            Padding = new Thickness(14),
            Margin = new Thickness(0, 0, 0, 12),
            CornerRadius = new CornerRadius(8),
            Background = new SolidColorBrush(Color.FromRgb(255, 250, 238))
        };

        var stack = new StackPanel();
        panel.Child = stack;

        var header = new DockPanel { Margin = new Thickness(0, 0, 0, 8) };
        var label = new TextBlock
        {
            Text = title,
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(72, 54, 28))
        };
        var valueLabel = new TextBlock
        {
            Text = FormatPercent(value),
            FontSize = 13,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(207, 111, 24))
        };

        DockPanel.SetDock(valueLabel, Dock.Right);
        header.Children.Add(valueLabel);
        header.Children.Add(label);
        stack.Children.Add(header);

        var slider = new Slider
        {
            Minimum = minimum,
            Maximum = maximum,
            Value = value,
            TickFrequency = 0.05,
            IsSnapToTickEnabled = false
        };
        slider.ValueChanged += (_, e) => changed(e.NewValue);
        slider.ValueChanged += (_, e) => valueLabel.Text = FormatPercent(e.NewValue);
        stack.Children.Add(slider);

        parent.Children.Add(panel);
        return (slider, valueLabel);
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
            BorderThickness = new Thickness(0),
            Background = new SolidColorBrush(accent ? Color.FromRgb(255, 176, 48) : Color.FromRgb(255, 243, 218)),
            Foreground = new SolidColorBrush(accent ? Colors.White : Color.FromRgb(91, 60, 24))
        };
        button.Click += (_, _) => action();
        return button;
    }

    private void Reset()
    {
        _scaleSlider.Value = 1.0;
        _volumeSlider.Value = 0.9;
    }

    private static string FormatPercent(double value)
    {
        return $"{value * 100:0}%";
    }
}
