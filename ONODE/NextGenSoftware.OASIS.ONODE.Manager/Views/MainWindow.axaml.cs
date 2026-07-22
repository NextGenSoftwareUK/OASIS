using Avalonia.Controls;
using NextGenSoftware.OASIS.ONODE.Manager.ViewModels;

namespace NextGenSoftware.OASIS.ONODE.Manager.Views;

public partial class MainWindow : Window
{
    public MainWindow() => InitializeComponent();
    public MainWindow(MainWindowViewModel vm) : this() => DataContext = vm;
}
