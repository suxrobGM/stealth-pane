using Avalonia;
using Avalonia.Controls;

namespace StealthCode.Controls;

// ReSharper disable once PartialTypeWithSinglePart
public partial class HotkeyTextBox : UserControl
{
    public static readonly StyledProperty<string> HotkeyProperty =
        AvaloniaProperty.Register<HotkeyTextBox, string>(nameof(Hotkey), "");

    public HotkeyTextBox()
    {
        InitializeComponent();

        Input.PropertyChanged += (_, e) =>
        {
            if (e.Property == TextBox.TextProperty)
            {
                Hotkey = Input.Text ?? "";
            }
        };

        HotkeyProperty.Changed.AddClassHandler<HotkeyTextBox>((box, e) =>
        {
            var value = (string?)e.NewValue ?? "";
            if (box.Input.Text != value)
            {
                box.Input.Text = value;
            }

            box.Validate(value);
        });
    }

    public string Hotkey
    {
        get => GetValue(HotkeyProperty);
        set => SetValue(HotkeyProperty, value);
    }

    private void Validate(string hotkey)
    {
        var error = GetValidationError(hotkey);
        ErrorText.Text = error;
        ErrorText.IsVisible = error.Length > 0;
    }

    private static string GetValidationError(string hotkey)
    {
        if (string.IsNullOrWhiteSpace(hotkey))
        {
            return "Hotkey cannot be empty";
        }

        var parts = hotkey.Split('+', StringSplitOptions.TrimEntries);
        if (parts.Length < 2)
        {
            return "Must be Modifier+Key (e.g. Ctrl+Shift+C)";
        }

        var hasModifier = false;
        var hasKey = false;

        foreach (var part in parts)
        {
            var upper = part.ToUpperInvariant();
            if (upper is "CTRL" or "ALT" or "SHIFT" or "WIN")
            {
                hasModifier = true;
            }
            else if (upper.Length == 1 && char.IsLetterOrDigit(upper[0]))
            {
                hasKey = true;
            }
            else if (upper.StartsWith('F') && int.TryParse(upper.AsSpan(1), out var f) && f is >= 1 and <= 24)
            {
                hasKey = true;
            }
            else if (upper is "SPACE" or "ENTER" or "RETURN" or "TAB" or "ESCAPE" or "ESC"
                     or "BACKSPACE" or "BACK" or "DELETE" or "DEL" or "INSERT" or "INS"
                     or "HOME" or "END" or "PAGEUP" or "PGUP" or "PAGEDOWN" or "PGDN"
                     or "UP" or "DOWN" or "LEFT" or "RIGHT" or "PRINTSCREEN" or "PRTSC")
            {
                hasKey = true;
            }
            else
            {
                return $"Unknown key: {part}";
            }
        }

        if (!hasModifier)
        {
            return "Must include a modifier (Ctrl, Alt, Shift, Win)";
        }

        if (!hasKey)
        {
            return "Must include a key (e.g. C, F5, Space)";
        }

        return "";
    }
}
