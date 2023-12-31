using System.ComponentModel;
using System.Dynamic;
using System.Globalization;
using System.Diagnostics;
#if WPF
using System.Windows.Data;
using System.Windows.Markup;
#elif AVALONIA
using DependencyObject = Avalonia.StyledElement;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Metadata;
#endif

using Antelcat.I18N.Abstractions;

// ReSharper disable once CheckNamespace
#if WPF
namespace System.Windows;
#elif AVALONIA
namespace Avalonia.Markup.Xaml.MarkupExtensions;
#endif

[DebuggerDisplay("Key = {Key}, Keys = {Keys}")]
public partial class I18NExtension : MarkupExtension, IAddChild
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

    public static string? Translate(string key, string? fallbackValue = null)
    {
        return Target.TryGetValue(key, out var value)
            ? value as string ?? fallbackValue
            : fallbackValue;
    }

    private static Action RegisterLanguageSource(ResourceProvider provider,
        bool lazyInit)
    {
        CultureChanged += culture => provider.Culture = culture;

        var props = provider.GetType().GetProperties();

        provider.PropertyChanged += (o, e) => Update(o!, e.PropertyName!);
        Notifier.RegisterProvider(provider);

        void LazyInitAction()
        {
            foreach (var prop in props) Update(provider, prop.Name);
        }

        if (!lazyInit)
        {
            LazyInitAction();
        }

        return LazyInitAction;

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
    public I18NExtension(BindingBase binding) => Key = binding;

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
    /// Same as <see cref="Binding"/>.<see cref="Binding.Converter"/>
    /// </summary>
    [DefaultValue(null)]
    public IValueConverter? Converter { get; set; }

    /// <summary>
    /// Same as <see cref="Binding"/>.<see cref="Binding.ConverterParameter"/>
    /// </summary>
    [DefaultValue(null)]
    public object? ConverterParameter { get; set; }

    private MultiBinding MapMultiBinding(Binding? keyBinding)
    {
        var ret = CreateMultiBinding();

        var source = new Binding(nameof(Notifier.Source))
        {
            Source = Notifier,
            Mode   = BindingMode.OneWay
        };
        var isBindingList = new List<bool>();
        ret.Bindings.Add(source);
        ret.Bindings.Add(keyBinding ?? (Key as BindingBase)!);
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
                case BindingBase propBinding:
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

    private void CheckArgument()
    {
        if (Key is null && Keys.Count == 0)
            throw new ArgumentNullException($"{nameof(Key)} or {nameof(Keys)} cannot both be null");
        if (Key is not null || Keys is not { Count: 1 }) return;
        Key = Keys[0];
        Keys.Clear();
    }
    
    public override object ProvideValue(IServiceProvider serviceProvider) => ProvideValueInternal(serviceProvider);

    private void ResetBinding(DependencyObject element)
    {
        if (Key is string && Keys.All(x => x is LanguageBinding)) return;
        var targetProperty = GetTargetProperty(element);
        SetTargetProperty(element, null!);
        var binding = CreateBinding();
        SetBinding(element, targetProperty, binding);
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
    private class MultiValueLangConverter(
#if !NET40
        IReadOnlyList<bool>
#else
        IList<bool>
#endif
            isBindingList) : IMultiValueConverter
    {
        public IValueConverter? Converter          { get; set; }
        public object?          ConverterParameter { get; set; }

        public object? Convert(
#if WPF
            object?[]
#elif AVALONIA
            IList<object?>
#endif
                values, Type targetType, object? parameter, CultureInfo culture)
        {
            var source = values[0]!;
            var count = values.
#if WPF
                    Length
#elif AVALONIA
                    Count
#endif
                ;
            var args = new object?[count - 2];
            var template = isBindingList[0]
                ? GetValue(source, values[1]?.ToString())
                : values[1]?.ToString();
            if (string.IsNullOrEmpty(template) || count <= 2)
                return Converter?.Convert(template, targetType, ConverterParameter, culture) ?? template;

            for (var i = 1; i < isBindingList.Count; i++)
            {
                var curr = values[i + 1];
                if (curr == null)
                {
                    args[i - 1] = string.Empty;
                    continue;
                }

                args[i - 1] = isBindingList[i]
                    ? GetValue(source, curr.ToString())
                    : curr.ToString();
            }

            var val = string.Format(template!, args);
            return Converter?.Convert(val, targetType, ConverterParameter, culture) ?? val;
        }

        private static string GetValue(object source, string? key) =>
            key == null
                ? string.Empty
                : ((IDictionary<string, object?>)source).TryGetValue(key, out var value)
                    ? value as string ?? key
                    : key;

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => throw new NotSupportedException();
    }

    /// <summary>
    /// <see cref="ResourceChangedNotifier"/> is singleton notifier for
    /// multibinding in case of avoid extra property notifier
    /// </summary>
    private partial class ResourceChangedNotifier(ExpandoObject source) : INotifyPropertyChanged
    {
        public ExpandoObject Source => source;

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public void RegisterProvider(ResourceProvider provider)
        {
            lastRegister = provider;
            provider.ChangeCompleted += (_, _) =>
            {
                if (lastRegister != provider) return;
                OnPropertyChanged(nameof(Source));
            };
        }

        private ResourceProvider? lastRegister;
    }
}