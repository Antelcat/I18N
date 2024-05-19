using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Dynamic;
using Antelcat.I18N.Avalonia.Internals;
using Avalonia.Data;
using Avalonia.Data.Core.Plugins;
using Avalonia.Metadata;
using ResourceProvider = Antelcat.I18N.Abstractions.ResourceProvider;

namespace Avalonia.Markup.Xaml.MarkupExtensions;

public partial class I18NExtension
{
    public I18NExtension() { }
    
    static I18NExtension()
    {
        var target = new ExpandoObject();
        Target   = target;
        Notifier = new ResourceChangedNotifier(target);
        SourceBinding = new(nameof(ResourceChangedNotifier.Source))
        {
            Source = Notifier,
            Mode   = BindingMode.OneWay,
        };

        //Register accessor plugin for ExpandoObject
        BindingPlugins.PropertyAccessors.Add(new ExpandoObjectPropertyAccessorPlugin(target));
        //Register accessor plugin for ExpandoObject
        BindingPlugins.PropertyAccessors.Insert(0, new NotifierPropertyAccessorPlugin(Notifier));

        lock (ResourceProvider.Providers)
        {
            foreach (var provider in ResourceProvider.Providers) RegisterLanguageSource(provider);

            ResourceProvider.Providers.CollectionChanged += (_, e) =>
            {
                if (e.Action != NotifyCollectionChangedAction.Add) return;
                foreach (var provider in e.NewItems.OfType<ResourceProvider>())
                {
                    RegisterLanguageSource(provider);
                }
            };
        }
    }

    private readonly AvaloniaObject proxy = new();

    #region Target

    private static readonly AvaloniaProperty KeyProperty = AvaloniaProperty.RegisterAttached
        <I18NExtension, AvaloniaObject, object>(nameof(Key));


    private static readonly AvaloniaProperty TargetPropertyProperty = AvaloniaProperty.RegisterAttached
        <I18NExtension, AvaloniaObject, AvaloniaProperty>("TargetProperty");

    private static void SetTargetProperty(AvaloniaObject element, AvaloniaProperty value)
        => element.SetValue(TargetPropertyProperty, value);

    private static AvaloniaProperty GetTargetProperty(AvaloniaObject element)
        => (AvaloniaProperty)element.GetValue(TargetPropertyProperty)!;

    #endregion

    /// <summary>
    /// The args of <see cref="string.Format(string,object[])"/>, accepts <see cref="LanguageBinding"/> or <see cref="Binding"/>
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    [DefaultValue(null)]
    [Content]
    public Collection<IBinding> Keys { get; } = [];


    public override partial object ProvideValue(IServiceProvider serviceProvider)
    {
        if (serviceProvider.GetService(typeof(IProvideValueTarget)) is not IProvideValueTarget)
            return this;
        CheckArgument();
        try
        {
            return CreateBinding();
        }
        finally
        {
            Task.Delay(500).ContinueWith(_ => { Notifier.ForceUpdate(); });
        }
    }


    private static void SetBinding(AvaloniaObject element, AvaloniaProperty targetProperty, IBinding binding)
    {
        element.Bind(targetProperty, binding);
        Notifier.ForceUpdate();
    }

    private static MultiBinding CreateMultiBinding() =>
        new()
        {
            Mode     = BindingMode.OneWay,
            Priority = BindingPriority.LocalValue,
        };

    private IBinding CreateBinding()
    {
        Binding? keyBinding = null;
        if (Key is not string key) return MapMultiBinding(keyBinding);
        keyBinding = new Binding(key)
        {
            Source        = Target,
            FallbackValue = key,
            Mode          = BindingMode.OneWay,
            Priority      = BindingPriority.LocalValue,
        };
        if (Keys is { Count: > 0 }) return MapMultiBinding(keyBinding);
        keyBinding.Converter          = Converter;
        keyBinding.ConverterParameter = ConverterParameter;
        return keyBinding;
    }

    private static partial void RegisterCultureChanged(ResourceProvider provider)
    {
        CultureChanged += culture =>
        {
            provider.Culture = culture;
            Notifier.ForceUpdate();
        };
    }

    partial class ResourceChangedNotifier
    {
        public void ForceUpdate() => OnPropertyChanged(nameof(Source));
    }
}