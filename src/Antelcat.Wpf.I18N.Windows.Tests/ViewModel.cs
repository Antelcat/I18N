using CommunityToolkit.Mvvm.ComponentModel;

namespace Antelcat.Wpf.I18N.Windows.Tests;

public partial class ViewModel : ObservableObject
{
    [ObservableProperty] private string language = "English";
}