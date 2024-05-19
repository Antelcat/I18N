using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Antelcat.I18N.Abstractions;

public abstract class ResourceProvider : INotifyPropertyChanged
{
    internal static readonly ObservableCollection<ResourceProvider> Providers = new();

    public abstract string this[string key] { get; }

    public abstract IEnumerable<string> Keys();
    
    protected static void RegisterProvider(ResourceProvider provider)
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