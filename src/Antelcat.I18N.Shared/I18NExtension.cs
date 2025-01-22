using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Diagnostics;
#if WPF
using System.Windows.Data;
using System.Windows.Markup;
#elif AVALONIA
using DependencyObject = Avalonia.AvaloniaObject;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Metadata;
#endif

using ResourceProvider = Antelcat.I18N.Abstractions.ResourceProvider;

// ReSharper disable once CheckNamespace
#if WPF
namespace System.Windows;
#elif AVALONIA
namespace Avalonia.Markup.Xaml.MarkupExtensions;
#endif

[DebuggerDisplay("Key = {Key}, Keys = {Keys}")]
public partial class I18NExtension : MarkupExtension, IAddChild
{
    private static readonly IDictionary<string, string> Target;
    private static readonly ResourceChangedNotifier     Notifier;
    private static event CultureChangedHandler?         CultureChanged;
    private static readonly Binding                     SourceBinding;

    private delegate void CultureChangedHandler(CultureInfo culture);

    /// <summary>
    /// Change the culture
    /// </summary>
    public static CultureInfo Culture
    {
        set => CultureChanged?.Invoke(value);
    }

    public static string? Translate(string key, string? fallbackValue = null) =>
        Target.TryGetValue(key, out var value)
            ? value ?? fallbackValue
            : fallbackValue;

    private static partial void RegisterCultureChanged(ResourceProvider provider);

    private static void RegisterLanguageSource(ResourceProvider provider)
    {
        RegisterCultureChanged(provider);

        var keys = provider.Keys();

        provider.PropertyChanged += (o, e) => Update(e.PropertyName);
        Notifier.RegisterProvider(provider);

        foreach (var key in keys) Update(key);

        void Update(string key) => Target[key] = provider[key];
    }


    public I18NExtension(string key) : this() => Key = key;
    public I18NExtension(BindingBase binding) : this() => Key = binding;

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

    private
#if AVALONIA
        IBinding
#elif WPF
        BindingBase
#endif
        CreateBinding() => Key is string key && Keys.Count == 0
        ? CreateKeyBinding(key)
        : MapMultiBinding();

    private Binding CreateKeyBinding(string key) =>
        new(nameof(Notifier.Source))
        {
            Source        = Notifier,
            Mode          = BindingMode.OneWay,
            FallbackValue = key,
            Converter = new StaticKeyConverter(key)
            {
                Converter          = Converter,
                ConverterParameter = ConverterParameter
            },
#if WPF
            UpdateSourceTrigger = UpdateSourceTrigger.Explicit,
#endif
        };

    private MultiBinding MapMultiBinding()
    {
        var                                ret           = CreateMultiBinding();
        var                                isBindingList = new List<bool>();
        List<string>                       keys          = [];
        List<MultiValueLangConverter.Mode> modes         = [];
        switch (Key)
        {
            case string key:
                keys.Add(key);
                modes.Add(MultiValueLangConverter.Mode.Key);
                break;
            case Binding binding:
                ret.Bindings.Add(binding);
                modes.Add(MultiValueLangConverter.Mode.Binding);
                break;
        }

        foreach (var bindingBase in Keys)
        {
            switch (bindingBase)
            {
                case LanguageBinding languageBinding:
                    if (languageBinding.Key is null)
                        throw new ArgumentNullException($"Language key should be specified");
                    keys.Add(languageBinding.Key);
                    modes.Add(MultiValueLangConverter.Mode.Key);
                    break;
                case not null:
                    ret.Bindings.Add(bindingBase);
                    modes.Add(MultiValueLangConverter.Mode.Binding);
                    break;
                default:
                    throw new ArgumentException(
                        $"{nameof(Keys)} only accept {typeof(LanguageBinding)} or {typeof(Binding)} current type is {bindingBase.GetType()}");
            }

            isBindingList.Add(bindingBase is not LanguageBinding);
        }

        ret.Converter = new MultiValueLangConverter(isBindingList.ToArray(), modes.ToArray())
        {
            Keys               = keys.ToArray(),
            Converter          = Converter,
            ConverterParameter = ConverterParameter,
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

    public override partial object ProvideValue(IServiceProvider serviceProvider);

    public void AddChild(object value)
    {
        if (value is not Binding binding) return;
        Keys.Add(binding);
    }

    public void AddText(string key)
    {
        Keys.Add(new LanguageBinding(key));
    }

    private class StaticKeyConverter(string key) : IValueConverter
    {
        public IValueConverter? Converter { get; set; }

        public object? ConverterParameter { get; set; }

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            Converter?.Convert(Target[key], targetType, ConverterParameter, culture) ?? Target[key];

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
            throw new NotSupportedException();
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
            isBindingList,
        MultiValueLangConverter.Mode[] modes) : IMultiValueConverter
    {
        public enum Mode
        {
            Key,
            Binding,
            Plain
        }

        public string[]         Keys               { get; set; } = [];
        public IValueConverter? Converter          { get; set; }
        public object?          ConverterParameter { get; set; }

        private object[] GetValues(IList<object?> bindingValues, out string template)
        {
            template = string.Empty;
            var ret          = new object[bindingValues.Count + Keys.Length - 1];
            var index        = 0;
            var keyIndex     = 0;
            var bindingIndex = 1; // first is trigger
            foreach (var mode in modes)
            {
                string value = null!;
                switch (mode)
                {
                    case Mode.Key:
                        var key = Keys[keyIndex++];
                        value = Target.TryGetValue(key, out var k) ? k ?? key : key;
                        break;
                    case Mode.Binding:
                        var binding = (bindingValues[bindingIndex++] as string)!;
                        value = Target.TryGetValue(binding, out var t) ? t ?? binding : binding;
                        break;
                }

                if (index == 0) template = value;
                else ret[index - 1]      = value;
                index++;
            }

            return ret;
        }

        public object? Convert(
#if WPF
            object?[]
#elif AVALONIA
            IList<object?>
#endif
                values, Type targetType, object? parameter, CultureInfo culture)
        {
            var    vs = GetValues(values, out var tem);
            string v;
            try
            {
                v = string.Format(tem, vs);
            }
            catch
            {
                v = tem;
            }

            return Converter?.Convert(v, targetType, ConverterParameter, culture) ?? v;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
            throw new NotSupportedException();
    }

    /// <summary>
    /// <see cref="ResourceChangedNotifier"/> is singleton notifier for
    /// multibinding in case of avoid extra property notifier
    /// </summary>
    internal partial class ResourceChangedNotifier(INotifyPropertyChanged source) : INotifyPropertyChanged
    {
        public INotifyPropertyChanged Source => source;

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public void RegisterProvider(ResourceProvider provider)
        {
            if (this.provider is not null) this.provider.ChangeCompleted -= ChangeCompleted;
            this.provider            =  provider;
            provider.ChangeCompleted += ChangeCompleted;
        }

        private ResourceProvider? provider;

        private void ChangeCompleted(object sender, EventArgs eventArgs)
        {
            if (provider != sender) return;
            OnPropertyChanged(nameof(Source));
        }
    }

    public class LanguageDictionary : IDictionary<string, string>, INotifyPropertyChanged
    {
        private readonly Dictionary<string, string>                dict = [];
        public           IEnumerator<KeyValuePair<string, string>> GetEnumerator() => dict.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)dict).GetEnumerator();

        public void Add(KeyValuePair<string, string> item)
        {
            dict.Add(item.Key, item.Value);
            OnPropertyChanged(item.Key);
        }

        public void Clear() => dict.Clear();

        public bool Contains(KeyValuePair<string, string> item) => dict.Contains(item);

        public void CopyTo(KeyValuePair<string, string>[] array, int arrayIndex)
        {
        }

        public bool Remove(KeyValuePair<string, string> item)
        {
            var value = dict.Remove(item.Key);
            OnPropertyChanged(item.Key);
            return value;
        }

        public int Count => dict.Count;

        public bool IsReadOnly => ((ICollection<KeyValuePair<string, string>>)dict).IsReadOnly;

        public bool ContainsKey(string key) => dict.ContainsKey(key);

        public void Add(string key, string value)
        {
            dict.Add(key, value);
            OnPropertyChanged(key);
        }

        public bool Remove(string key)
        {
            var value = dict.Remove(key);
            OnPropertyChanged(key);
            return value;
        }

        public bool TryGetValue(string key, out string value) => dict.TryGetValue(key, out value);

        public string this[string key]
        {
            get => dict[key];
            set
            {
                dict[key] = value;
                OnPropertyChanged(key);
            }
        }

        public ICollection<string> Keys => ((IDictionary<string, string>)dict).Keys;

        public ICollection<string>                Values => ((IDictionary<string, string>)dict).Values;
        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}