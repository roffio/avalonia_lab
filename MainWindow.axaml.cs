using Avalonia.Controls;
using avalonia_test.ViewModels;

namespace avalonia_test;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }
}