using System.ComponentModel;
using Avalonia.Data;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml.MarkupExtensions;

namespace Antelcat.I18N.Avalonia.Internals;

internal class NotifierPropertyAccessorPlugin(I18NExtension.ResourceChangedNotifier target)
    : IPropertyAccessorPlugin
{
    private static IPropertyAccessorPlugin? instance;
    
    internal static IPropertyAccessorPlugin Instance(I18NExtension.ResourceChangedNotifier target)
    {
        if (instance is not null) return instance;
        instance = new NotifierPropertyAccessorPlugin(target);
        return instance;
    }

    private NotifyAccessor? accessor;
    public bool Match(object obj, string propertyName) =>
        propertyName is nameof(I18NExtension.ResourceChangedNotifier.Source) && ReferenceEquals(obj, target);

    private object GetValue() => target.Source;

    public IPropertyAccessor? Start(WeakReference<object?> reference, string propertyName)
    {
        return Create();
        if (accessor is not null) return accessor;
        lock (target)
        {
            if (accessor is not null) return accessor;
            accessor = Create();
        }

        return accessor;
    }

    private NotifyAccessor Create()
    {
        var ret = new NotifyAccessor(GetValue);
        target.PropertyChanged += ret.OnPropertyChanged;
        return ret;
    }


    private class NotifyAccessor(Func<object?> valueGetter) : IPropertyAccessor
    {

        private event Action<object?>? Subscriptions;

        internal void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (Subscriptions is null) return;
            var value = Value;
            Subscriptions(null);
            Subscriptions(value);
        }

        ~NotifyAccessor() => Dispose();

        public void Dispose() => Unsubscribe();

        public bool SetValue(object? value, BindingPriority priority) => throw new NotImplementedException();

        public void Subscribe(Action<object?> listener) => Subscriptions += listener;

        public void Unsubscribe()
        {
            Subscriptions = null;
            Console.WriteLine("Unsub");
        }

        public Type?   PropertyType { get; } = typeof(INotifyPropertyChanged);
        public object? Value        => valueGetter();
    }
}
