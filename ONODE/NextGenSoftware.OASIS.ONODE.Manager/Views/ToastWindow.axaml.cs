using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace NextGenSoftware.OASIS.ONODE.Manager.Views;

public partial class ToastWindow : Window
{
    public ToastWindow()
    {
        InitializeComponent();
    }

    public static void Show(string title, string message, int durationMs = 4000)
    {
        Dispatcher.UIThread.Post(() =>
        {
            var toast = new ToastWindow();
            toast.TitleText.Text   = title;
            toast.MessageText.Text = message;

            // Position: bottom-right of primary screen
            var screen = toast.Screens.Primary;
            if (screen != null)
            {
                var bounds = screen.WorkingArea;
                toast.Position = new PixelPoint(
                    (int)(bounds.X + bounds.Width  - 316),
                    (int)(bounds.Y + bounds.Height - 88));
            }

            toast.Show();

            DispatcherTimer.RunOnce(() =>
            {
                if (toast.IsVisible) toast.Close();
            }, TimeSpan.FromMilliseconds(durationMs));
        });
    }

    void OnClose(object? sender, RoutedEventArgs e) => Close();
}
