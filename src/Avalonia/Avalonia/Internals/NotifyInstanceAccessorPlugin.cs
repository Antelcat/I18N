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

    public IPropertyAccessor? Start(WeakReference<object?> reference, string propertyName)
    {
        var notifier = reference.TryGetTarget(out var target)
            ? target is T t ? t : throw new InvalidCastException(nameof(reference))
            : throw new NullReferenceException(nameof(reference));
        return Accessors.GetOrAdd(propertyName, _ => new NotifyAccessor(
            propertyName,
            GetPropertyType(propertyName),
            () => GetValue(notifier, propertyName),
            OnPropertyChanged,
            OnSubscription)
        {
            Instance = notifier
        });
    }

    protected virtual Type GetPropertyType(string propertyName) => typeof(object);
    protected virtual object? GetValue(T target, string propertyName) => null;

    protected virtual void OnPropertyChanged(Action<object?> subscription, object? value) => subscription(value);

    protected virtual void OnSubscription(Action<object?> subscription, IPropertyAccessor accessor) { }

    private class NotifyAccessor(
        string propertyName,
        Type propertyType,
        Func<object?> valueGetter,
        Action<Action<object?>, object?> onPropertyChanged,
        Action<Action<object?>, IPropertyAccessor> onSubscription) : IPropertyAccessor
    {
        public T? Instance
        {
            get;
            set
            {
                if (field != null) field.PropertyChanged -= OnPropertyChanged;
                if (value == null) return;
                field                 =  value;
                field.PropertyChanged += OnPropertyChanged;
            }
        }

        private readonly HashSet<Action<object?>> subscriptions = [];

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != propertyName) return;
            var value = Value;
            lock (subscriptions)
            {
                foreach (var subscription in subscriptions)
                {
                    onPropertyChanged(subscription, value);
                }
            }
        }

        ~NotifyAccessor()
        {
            Dispose();
        }

        public void Dispose()
        {
            subscriptions.Clear();
            Accessors.TryRemove(propertyName, out _);
        }

        public bool SetValue(object? value, BindingPriority priority) => throw new NotImplementedException();

        public void Subscribe(Action<object?> listener)
        {
            lock (subscriptions)
            {
                subscriptions.Add(listener);
            }

            onSubscription(listener, this);
        }

        public void Unsubscribe()
        {
            lock (subscriptions)
            {
                subscriptions.Clear();
            }

            Dispose();
        }

        public Type?   PropertyType { get; } = propertyType;
        public object? Value        => valueGetter();
    }
}