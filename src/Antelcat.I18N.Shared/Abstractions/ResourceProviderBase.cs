using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Antelcat.I18N.Abstractions;

public abstract class ResourceProviderBase : INotifyPropertyChanged
{
    internal static readonly ObservableCollection<ResourceProviderBase> Providers = new();

    protected static void RegisterProvider(ResourceProviderBase provider)
    {
        lock (Providers)
        {
            if (Providers.Contains(provider)) return;
            Providers.Add(provider);
        }
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;

    public event StatementCompletedEventHandler? ChangeCompleted;
    
    protected void OnPropertyChanged(
    #if NET45_OR_GREATER || NET || NETSTANDARD
        [CallerMemberName] 
    #endif
        string? propertyName = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    
    protected void OnChangeCompleted() => ChangeCompleted?.Invoke(this, null);
    
    public abstract CultureInfo? Culture { get; set; }
}