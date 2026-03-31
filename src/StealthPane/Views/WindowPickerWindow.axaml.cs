using Avalonia.Controls;
using Avalonia.Input;
using StealthPane.Services;
using StealthPane.ViewModels;

namespace StealthPane.Views;

// ReSharper disable once PartialTypeWithSinglePart
public partial class WindowPickerWindow : Window
{
    private readonly WindowPickerViewModel viewModel;

    public WindowPickerWindow() : this([]) { }

    public WindowPickerWindow(List<WindowInfo> windows)
    {
        viewModel = new WindowPickerViewModel(windows);
        DataContext = viewModel;

        InitializeComponent();

        viewModel.ResultTask.ContinueWith(_ =>
            Avalonia.Threading.Dispatcher.UIThread.Post(Close),
            TaskContinuationOptions.ExecuteSynchronously);
    }

    public Task<WindowInfo?> GetSelectionAsync() => viewModel.ResultTask;

    private void OnListDoubleTapped(object? sender, TappedEventArgs e)
    {
        viewModel.SelectCommand.Execute(null);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            viewModel.CancelCommand.Execute(null);
            e.Handled = true;
        }

        base.OnKeyDown(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        viewModel.OnClosed();
        base.OnClosed(e);
    }
}
