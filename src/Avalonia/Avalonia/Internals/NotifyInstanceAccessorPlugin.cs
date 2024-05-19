using System.Collections.Concurrent;
using System.ComponentModel;
using Avalonia.Data;
using Avalonia.Data.Core.Plugins;

namespace Antelcat.I18N.Avalonia.Internals;

public class NotifyInstanceAccessorPlugin<T>(T instance) : IPropertyAccessorPlugin
    where T : INotifyPropertyChanged
{
    private static readonly ConcurrentDictionary<string, NotifyAccessor> Accessors = [];

    public bool Match(object obj, string propertyName) => ReferenceEquals(obj, instance);

    public IPropertyAccessor? Start(WeakReference<object?> reference, string propertyName) =>
        Accessors.GetOrAdd(propertyName, _ => new NotifyAccessor(
            propertyName,
            GetPropertyType(propertyName),
            () => GetValue(instance, propertyName), 
            OnPropertyChanged,
            OnSubscription)
        {
            Instance = instance
        });

    protected virtual Type GetPropertyType(string propertyName) => typeof(object);
    protected virtual object? GetValue(T target, string propertyName) => null;

    protected virtual void OnPropertyChanged(Action<object?> subscription, object? value) => subscription(value);

    protected virtual void OnSubscription(Action<object?> subscription, IPropertyAccessor accessor) { }

    private class NotifyAccessor(
        string propertyName,
        Type propertyType,
        Func<object?> valueGetter,
        Action<Action<object?>,object?> onPropertyChanged,
        Action<Action<object?>, IPropertyAccessor> onSubscription) : IPropertyAccessor
    {
        public T? Instance
        {
            get => instance;
            set
            {
                if (instance != null) instance.PropertyChanged -= OnPropertyChanged;
                if (value    == null) return;
                instance                 =  value;
                instance.PropertyChanged += OnPropertyChanged;
            }
        }

        private T? instance;

        private readonly HashSet<Action<object?>> subscriptions = [];

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != propertyName) return;
            var value = Value;
            foreach (var subscription in subscriptions)
            {
                onPropertyChanged(subscription, value);
            }
        }

        public void Dispose() => Accessors.TryRemove(propertyName, out _);

        public bool SetValue(object? value, BindingPriority priority) => throw new NotImplementedException();

        public void Subscribe(Action<object?> listener)
        {
            subscriptions.Add(listener);
            onSubscription(listener, this);
        }

        public void Unsubscribe()
        {
            subscriptions.Clear();
            Dispose();
        }

        public Type?   PropertyType { get; } = propertyType;
        public object? Value        => valueGetter();
    }
}