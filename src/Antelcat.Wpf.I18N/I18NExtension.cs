using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using Antelcat.Wpf.I18N.Abstractions;

// ReSharper disable once CheckNamespace
namespace System.Windows;

[MarkupExtensionReturnType(typeof(string))]
[ContentProperty(nameof(Keys))]
[Localizability(LocalizationCategory.None, Modifiability = Modifiability.Unmodifiable,
    Readability = Readability.Unreadable)]
public class I18NExtension : MarkupExtension, IAddChild
{
    private static readonly IDictionary<string, object?> Target;
    private static readonly ResourceChangedNotifier      Notifier;
    private static event CultureChangedHandler?          CultureChanged;

    private delegate void CultureChangedHandler(CultureInfo culture);

    /// <summary>
    /// Change the culture
    /// </summary>
    public static CultureInfo Culture
    {
        set => CultureChanged?.Invoke(value);
    }

    static I18NExtension()
    {
        var target = new ExpandoObject();
        Target   = target;
        Notifier = new ResourceChangedNotifier(target);
        foreach (var type in Assembly.GetEntryAssembly()?.GetTypes() ?? Type.EmptyTypes)
        {
            if (!type.IsSubclassOf(typeof(ResourceProviderBase))) continue;
            RegisterLanguageSource(FormatterServices.GetUninitializedObject(type) as ResourceProviderBase);
        }
    }

    #region Target

    private static readonly DependencyProperty KeyProperty = DependencyProperty.RegisterAttached(
        nameof(Key),
        typeof(object),
        typeof(I18NExtension),
        new PropertyMetadata(default));


    private static readonly DependencyProperty TargetPropertyProperty = DependencyProperty.RegisterAttached(
        "TargetProperty",
        typeof(DependencyProperty),
        typeof(I18NExtension),
        new PropertyMetadata(default(DependencyProperty)));

    private static void SetTargetProperty(DependencyObject element, DependencyProperty value)
        => element.SetValue(TargetPropertyProperty, value);

    private static DependencyProperty GetTargetProperty(DependencyObject element)
        => (DependencyProperty)element.GetValue(TargetPropertyProperty);

    #endregion

    public static string? Translate(string key, string? fallbackValue = null)
    {
        return Target.TryGetValue(key, out var value)
            ? value as string ?? fallbackValue
            : fallbackValue;
    }

    private static void RegisterLanguageSource(ResourceProviderBase? provider)
    {
        if (provider is null) return;

        CultureChanged += culture => provider.Culture = culture;

        var props = provider.GetType().GetProperties();

        provider.PropertyChanged += (o, e) => Update(o!, e.PropertyName!);
        Notifier.RegisterProvider(provider);
        foreach (var prop in props) Update(provider, prop.Name);

        return;

        void Update(object source, string propertyName)
        {
            var val = Array.Find(props, x => x.Name.Equals(propertyName))?.GetValue(source, null);

            if (val != null) Target[propertyName] = val;
        }
    }

    public I18NExtension()
    {
    }

    public I18NExtension(string key) => Key = key;
    public I18NExtension(Binding binding) => Key = binding;

    private readonly DependencyObject proxy = new();

    /// <summary>
    /// Resource key, accepts <see cref="string"/> or <see cref="Binding"/>.   
    /// If Keys not null, Key will be the template of <see cref="string.Format(string,object[])"/>
    /// </summary>
    [DefaultValue(null)]
    public object? Key
    {
        get => proxy.GetValue(KeyProperty);
        set => proxy.SetValue(KeyProperty, value);
    }

    /// <summary>
    /// The args of <see cref="string.Format(string,object[])"/>, accepts <see cref="LanguageBinding"/> or <see cref="Binding"/>
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    [DefaultValue(null)]
    public MicrosoftPleaseFixBindingCollection Keys => keys ??= new MicrosoftPleaseFixBindingCollection();

    private MicrosoftPleaseFixBindingCollection? keys;

    /// <summary>
    /// Same as <see cref="Binding"/>.<see cref="Binding.Converter"/>
    /// </summary>
    [DefaultValue(null)]
    public IValueConverter? Converter { get; set; }

    /// <summary>
    /// Same as <see cref="Binding"/>.<see cref="Binding.ConverterParameter"/>
    /// </summary>
    [DefaultValue(null)]
    public object? ConverterParameter { get; set; }

    private BindingBase CreateBinding()
    {
        Binding? keyBinding = null;
        if (Key is string key)
        {
            keyBinding = new Binding(key)
            {
                Source        = Target,
                Mode          = BindingMode.OneWay,
                FallbackValue = key,
            };
            if (keys is not { Count: > 0 })
            {
                keyBinding.UpdateSourceTrigger = UpdateSourceTrigger.Explicit;
                keyBinding.Converter           = Converter;
                keyBinding.ConverterParameter  = ConverterParameter;
                return keyBinding;
            }
        }

        var ret = new MultiBinding
        {
            Mode                = BindingMode.OneWay,
            UpdateSourceTrigger = UpdateSourceTrigger.Explicit,
            ConverterParameter  = ConverterParameter,
        };

        var source = new Binding(nameof(Notifier.Source))
        {
            Source = Notifier,
            Mode   = BindingMode.OneWay
        };
        var isBindingList = new List<bool>();
        ret.Bindings.Add(source);
        ret.Bindings.Add(keyBinding ?? Key as Binding);
        isBindingList.Add(keyBinding == null);
        foreach (var bindingBase in Keys)
        {
            switch (bindingBase)
            {
                case LanguageBinding languageBinding:
                    if (languageBinding.Key is null)
                        throw new ArgumentNullException($"Language key should be specified");
                    ret.Bindings.Add(new Binding(languageBinding.Key)
                    {
                        Source        = Target,
                        Mode          = BindingMode.OneWay,
                        FallbackValue = languageBinding.Key
                    });
                    break;
                case Binding propBinding:
                    ret.Bindings.Add(propBinding);
                    break;
                default:
                    throw new ArgumentException(
                        $"{nameof(Keys)} only accept {typeof(LanguageBinding)} or {typeof(Binding)} current type is {bindingBase.GetType()}");
            }

            isBindingList.Add(bindingBase is not LanguageBinding);
        }

        ret.Converter = new MultiValueLangConverter(isBindingList.ToArray())
        {
            Converter          = Converter,
            ConverterParameter = ConverterParameter
        };
        return ret;
    }

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        if (serviceProvider.GetService(typeof(IProvideValueTarget)) is not IProvideValueTarget provideValueTarget)
            return this;
        if (provideValueTarget.TargetObject.GetType().FullName == "System.Windows.SharedDp") return this;
        if (provideValueTarget.TargetObject is not DependencyObject targetObject) return this;
        if (provideValueTarget.TargetProperty is not DependencyProperty targetProperty) return this;

        if (Key is null && (keys is null || keys.Count == 0))
            throw new ArgumentNullException($"{nameof(Key)} or {nameof(Keys)} cannot both be null");
        if (Key is null && Keys is { Count: 1 })
        {
            Key  = Keys[0];
            keys = null;
        }

        var bindingBase = CreateBinding();
        BindingOperations.SetBinding(targetObject, targetProperty, bindingBase);
        if (bindingBase is MultiBinding)
        {
            SetTarget(provideValueTarget.TargetObject, targetProperty);
        }

        return bindingBase.ProvideValue(serviceProvider);
    }

    private void LangExtension_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        switch (sender)
        {
            case FrameworkElement element:
            {
                element.DataContextChanged -= LangExtension_DataContextChanged;
                ResetBinding(element);
                break;
            }
            case FrameworkContentElement element:
            {
                element.DataContextChanged -= LangExtension_DataContextChanged;
                ResetBinding(element);
                break;
            }
        }
    }

    private void SetTarget(object targetObject, DependencyProperty targetProperty)
    {
        switch (targetObject)
        {
            case FrameworkContentElement element:
                SetTargetProperty(element, targetProperty);
                element.DataContextChanged += LangExtension_DataContextChanged;
                return;
            case FrameworkElement element:
                SetTargetProperty(element, targetProperty);
                element.DataContextChanged += LangExtension_DataContextChanged;
                return;
        }
    }

    private void ResetBinding(
        DependencyObject element)
    {
        if (Key is string
            && (keys is null || keys.All(x => x is LanguageBinding))) return;
        var targetProperty = GetTargetProperty(element);
        SetTargetProperty(element, null!);
        var binding = CreateBinding();
        BindingOperations.SetBinding(element, targetProperty, binding);
    }

    public void AddChild(object value)
    {
        if (value is not Binding binding) return;
        Keys.Add(binding);
    }

    public void AddText(string key)
    {
        Keys.Add(new LanguageBinding(key));
    }


    /// <summary>
    /// use <see cref="string.Format(string,object[])"/> to generate final text
    /// </summary>
    private class MultiValueLangConverter : IMultiValueConverter
    {
        public IValueConverter? Converter          { get; set; }
        public object?          ConverterParameter { get; set; }

        private readonly bool[] isBindingList;

        public MultiValueLangConverter(bool[] isBindingList)
        {
            this.isBindingList = isBindingList;
        }

        public object? Convert(object?[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var template = isBindingList[0]
                ? GetValue(values[0]!, values[1] as string)
                : values[1] as string;
            if (string.IsNullOrEmpty(template))
                return Converter?.Convert(template, targetType, ConverterParameter, culture) ?? template;
            if (values.Length <= 2) return template!;

            for (var i = 1; i < isBindingList.Length; i++)
            {
                if (values[i] == null)
                {
                    values[i] = string.Empty;
                    continue;
                }

                if (isBindingList[i])
                {
                    values[i + 1] = GetValue(values[0]!, (values[i + 1] as string)!);
                }
            }

            var val = string.Format(template!, values.Skip(2).ToArray());
            return Converter?.Convert(val, targetType, ConverterParameter, culture) ?? val;
        }

        private static string GetValue(object source, string? key)
        {
            return key == null
                ? string.Empty
                : ((IDictionary<string, object?>)source).TryGetValue(key, out var value)
                    ? value as string ?? key
                    : key;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    /// <summary>
    /// You know why, are you? <see cref="Collection{BindingBase}"/>
    /// </summary>
    public class MicrosoftPleaseFixBindingCollection : Collection<BindingBase>
    {
    }


    /// <summary>
    /// <see cref="ResourceChangedNotifier"/> is singleton notifier for
    /// multibinding in case of avoid extra property notifier
    /// </summary>
    private class ResourceChangedNotifier : INotifyPropertyChanged
    {
        public ExpandoObject Source { get; }

        public ResourceChangedNotifier(ExpandoObject source)
        {
            Source = source;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void RegisterProvider(ResourceProviderBase provider)
        {
            lastRegister = provider;
            provider.ChangeCompleted += (_, _) =>
            {
                if (lastRegister != provider) return;
                OnPropertyChanged(nameof(Source));
            };
        }

        private ResourceProviderBase? lastRegister;
    }
}