using Avalonia.Data;
using Avalonia.Data.Core.Plugins;
using System.ComponentModel;
using System.Dynamic;
using System.Reflection;

namespace Antelcat.I18N.WPF.Avalonia.Internals;

internal class ExpandoObjectPropertyAccessorPlugin(ExpandoObject target) : IPropertyAccessorPlugin
{
    public bool Match(object obj, string propertyName) => obj == target;

    public IPropertyAccessor Start(WeakReference<object?> reference, string propertyName)
    {
        return ExpandoAccessor.Create(propertyName);
    }

    public static void Register(ExpandoObject target)
    {
        if (Assembly.GetAssembly(typeof(IPropertyAccessorPlugin))
                .GetType("Avalonia.Data.Core.ExpressionObserver")
                .GetField("PropertyAccessors", BindingFlags.Public | BindingFlags.Static)!
                .GetValue(null) is IList<IPropertyAccessorPlugin> { } plugins)
        {
            plugins.Add(new ExpandoObjectPropertyAccessorPlugin(target));
            ExpandoAccessor.Source = target;
        }
    }

    private class ExpandoAccessor : IPropertyAccessor
    {
        private static readonly Dictionary<string, ExpandoAccessor> Accessors = new();

        public static ExpandoObject? Source
        {
            get => source;
            set
            {
                if (source != null)
                {
                    ((INotifyPropertyChanged)source).PropertyChanged -= OnPropertyChanged;
                }

                if (value == null) return;
                source = value;

                ((INotifyPropertyChanged)source).PropertyChanged += OnPropertyChanged;
            }
        }

        private static   ExpandoObject? source;
        private readonly string         propertyName;

        readonly List<Action<object?>> subscriptions = new();

        public static ExpandoAccessor Create(string propertyName)
        {
            if (Accessors.TryGetValue(propertyName, out var accessor)) return accessor;
            accessor = new ExpandoAccessor(propertyName);
            Accessors.Add(propertyName, accessor);
            return accessor;
        }

        private ExpandoAccessor(string propertyName)
        {
            this.propertyName = propertyName;
        }

        public void Dispose()
        {
            lock (Accessors)
            {
                Accessors.Remove(propertyName);
            }
        }

        private static void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!Accessors.TryGetValue(e.PropertyName, out var accessor)) return;
            var val = ((IDictionary<string, object?>)Source!)[e.PropertyName];
            foreach (var action in accessor.subscriptions)
            {
                action(val);
            }
        }

        public bool SetValue(object? value, BindingPriority priority)
        {
            throw new NotSupportedException();
        }

        public void Subscribe(Action<object?> listener)
        {
            subscriptions.Add(listener);
        }

        public void Unsubscribe()
        {
            Dispose();
        }

        public Type?   PropertyType { get; } = typeof(string);
        public object? Value        => ((IDictionary<string, object?>)Source!)[propertyName];
    }
}
