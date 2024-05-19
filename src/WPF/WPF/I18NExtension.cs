using System.Collections.Specialized;
using System.ComponentModel;
using System.Dynamic;
using System.Windows.Data;
using System.Windows.Markup;
using Antelcat.I18N.Abstractions;
using MicrosoftPleaseFixBindingCollection = System.Collections.ObjectModel.Collection<System.Windows.Data.BindingBase>;

namespace System.Windows;

[MarkupExtensionReturnType(typeof(string))]
[ContentProperty(nameof(Keys))]
[Localizability(LocalizationCategory.None, Modifiability = Modifiability.Unmodifiable,
    Readability = Readability.Unreadable)]
public partial class I18NExtension
{
    public I18NExtension()
    {
        
    }
    static I18NExtension()
    {
        var target = new ExpandoObject();
        Target   = target;
        Notifier = new ResourceChangedNotifier(target);
        SourceBinding = new(nameof(Notifier.Source))
        {
            Source = Notifier,
            Mode   = BindingMode.OneWay,
        };
        lock (ResourceProvider.Providers)
        {
            foreach (var provider in ResourceProvider.Providers)
            {
                RegisterLanguageSource(provider);
            }

            ResourceProvider.Providers.CollectionChanged += (o, e) =>
            {
                if(e.Action != NotifyCollectionChangedAction.Add)return;
                foreach (var provider in e.NewItems?.OfType<ResourceProvider>() ?? [])
                {
                    RegisterLanguageSource(provider);
                }
            };
        }
    }

    private readonly DependencyObject proxy = new();
    
    #region Target

    private static readonly DependencyProperty KeyProperty = DependencyProperty.RegisterAttached
    (
        nameof(Key),
        typeof(object),
        typeof(I18NExtension),
        new PropertyMetadata(default)
    );


    private static readonly DependencyProperty TargetPropertyProperty = DependencyProperty.RegisterAttached
    (
        "TargetProperty",
        typeof(DependencyProperty),
        typeof(I18NExtension),
        new PropertyMetadata(default(DependencyProperty))
    );

    public override partial object ProvideValue(IServiceProvider serviceProvider)
    {
        if (serviceProvider.GetService(typeof(IProvideValueTarget)) is not IProvideValueTarget provideValueTarget)
            return this;
        if (provideValueTarget.TargetObject.GetType().FullName ==
            $"{nameof(System)}.{nameof(Windows)}.SharedDp") return this;
        if (provideValueTarget.TargetObject is not DependencyObject targetObject)
            return this;

        if (provideValueTarget.TargetProperty is not DependencyProperty targetProperty) return this;

        CheckArgument();

        var bindingBase = CreateBinding();
        Binding = bindingBase;

        BindingOperations.SetBinding(targetObject, targetProperty, bindingBase);

        if (bindingBase is MultiBinding)
        {
            SetTarget(targetObject, targetProperty);
        }

        return bindingBase.ProvideValue(serviceProvider);
    }

    private static void SetTargetProperty(DependencyObject element, DependencyProperty value)
        => element.SetValue(TargetPropertyProperty, value);

    private static DependencyProperty GetTargetProperty(DependencyObject element)
        => (DependencyProperty)element.GetValue(TargetPropertyProperty)!;

    #endregion

    /// <summary>
    /// The args of <see cref="string.Format(string,object[])"/>, accepts <see cref="LanguageBinding"/> or <see cref="Binding"/>
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    [DefaultValue(null)]
    public MicrosoftPleaseFixBindingCollection Keys { get; } = [];

    private void I18NExtension_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        switch (sender)
        {
            case FrameworkElement element:
            {
                element.DataContextChanged -= I18NExtension_DataContextChanged;
                ResetBinding(element);
                break;
            }
            case FrameworkContentElement element:
            {
                element.DataContextChanged -= I18NExtension_DataContextChanged;
                ResetBinding(element);
                break;
            }
        }
    }

    private void SetTarget(DependencyObject targetObject, DependencyProperty targetProperty)
    {
        switch (targetObject)
        {
            case FrameworkContentElement element:
                SetTargetProperty(element, targetProperty);
                element.DataContextChanged += I18NExtension_DataContextChanged;
                return;
            case FrameworkElement element:
                SetTargetProperty(element, targetProperty);
                element.DataContextChanged += I18NExtension_DataContextChanged;
                return;
        }
    }

    private static void SetBinding(DependencyObject element, DependencyProperty targetProperty, BindingBase binding)
    {
        BindingOperations.SetBinding(element, targetProperty, binding);
    }

    private MultiBinding CreateMultiBinding()
    {
        return new MultiBinding
        {
            Mode                = BindingMode.OneWay,
            ConverterParameter  = ConverterParameter,
            UpdateSourceTrigger = UpdateSourceTrigger.Explicit,
        };
    }


    private BindingBase CreateBinding()
    {
        Binding? keyBinding = null;
        if (Key is not string key) return MapMultiBinding(keyBinding);
        keyBinding = new Binding(key)
        {
            Source        = Target,
            Mode          = BindingMode.OneWay,
            FallbackValue = key,
        };
        if (Keys is { Count: > 0 }) return MapMultiBinding(keyBinding);
        keyBinding.UpdateSourceTrigger = UpdateSourceTrigger.Explicit;
        keyBinding.Converter           = Converter;
        keyBinding.ConverterParameter  = ConverterParameter;
        return keyBinding;
    }

    private static partial void RegisterCultureChanged(ResourceProvider provider)
    {
        CultureChanged += culture =>
        {
            provider.Culture = culture;
        };
    }
}