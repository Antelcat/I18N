using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Data;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml.MarkupExtensions;

namespace Antelcat.I18N.Avalonia.Internals;

internal class NotifierPropertyAccessorPlugin
    : IPropertyAccessorPlugin
{
    [field: MaybeNull, AllowNull]
    internal static IPropertyAccessorPlugin Instance
    {
        get
        {
            if (field is not null) return field;
            field = new NotifierPropertyAccessorPlugin();
            return field;
        }
    }

    public bool Match(object obj, string propertyName) =>
        propertyName is nameof(I18NExtension.ResourceChangedNotifier.Source) &&
        obj is I18NExtension.ResourceChangedNotifier;


    public IPropertyAccessor? Start(WeakReference<object?> reference, string propertyName) =>
        reference.TryGetTarget(out var t)
        && t is I18NExtension.ResourceChangedNotifier n
            ? new NotifyAccessor(n)
            : null;


    private class NotifyAccessor : IPropertyAccessor
    {
        private readonly I18NExtension.ResourceChangedNotifier notifier;
        public NotifyAccessor(I18NExtension.ResourceChangedNotifier notifier)
        {
            this.notifier                 =  notifier;
            this.notifier.PropertyChanged += OnPropertyChanged;
        }
        private event Action<object?>? Subscriptions;

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (Subscriptions is null) return;
            Subscriptions(null);
            Subscriptions(Value);
        }

        ~NotifyAccessor() => Dispose();

        public void Dispose() => Unsubscribe();

        public bool SetValue(object? value, BindingPriority priority) => throw new NotImplementedException();

        public void Subscribe(Action<object?> listener) => Subscriptions += listener;

        public void Unsubscribe()
        {
            Subscriptions            =  null;
            notifier.PropertyChanged -= OnPropertyChanged;
            Console.WriteLine("Unsub");
        }

        public Type?   PropertyType { get; } = typeof(INotifyPropertyChanged);
        public object? Value        => notifier.Source;
    }
}
