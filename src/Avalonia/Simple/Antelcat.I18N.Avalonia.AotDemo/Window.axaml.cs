using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Antelcat.I18N.Avalonia.AotDemo;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        var vm = new ViewModel();
        DataContext =  vm;
        Loaded      += async (o, e) =>
        {
            await Task.Delay(1000);
            vm.Language =  "Chinese";
        };
    }
}

public partial class ViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string Language { get; set; } = "English";
}