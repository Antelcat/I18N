using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Antelcat.Wpf.I18N.Abstractions;

public abstract class ResourceProviderBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(
    #if NET45_OR_GREATER || NET || NETSTANDARD
        [CallerMemberName] 
    #endif
        string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    public abstract CultureInfo? Culture { get; set; }
    
}