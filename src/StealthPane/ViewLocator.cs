using Avalonia.Controls;
using Avalonia.Controls.Templates;
using StealthPane.ViewModels;
using StealthPane.Views;

namespace StealthPane;

public sealed class ViewLocator : IDataTemplate
{
    private static readonly Dictionary<Type, Func<Control>> ViewRegistry = new()
    {
        { typeof(SettingsViewModel), () => new SettingsView() }
    };

    public Control? Build(object? param)
    {
        if (param is null)
        {
            return null;
        }

        var viewModelType = param.GetType();

        if (ViewRegistry.TryGetValue(viewModelType, out var factory))
        {
            var control = factory();
            control.DataContext = param;
            return control;
        }

        return new TextBlock { Text = "Not Found: " + viewModelType.Name };
    }

    public bool Match(object? data)
    {
        return data is ViewModelBase;
    }
}
