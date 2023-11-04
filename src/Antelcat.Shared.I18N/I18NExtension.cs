using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Dynamic;
using System.Globalization;
using System.Reflection;
using System.Runtime.Serialization;
using System.Diagnostics;
#if WPF
using MicrosoftPleaseFixBindingCollection = System.Collections.ObjectModel.Collection<System.Windows.Data.BindingBase>;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Data;
using System.Windows.Markup;
#elif AVALONIA
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Data.Core.Plugins;
using Avalonia.Metadata;
using Avalonia.Threading;
using DependencyObject = Avalonia.AvaloniaObject;
using DependencyProperty = Avalonia.AvaloniaProperty;
using DependencyPropertyChangedEventArgs = System.EventArgs;
#endif

using Antelcat.
#if WPF
    Wpf
#elif AVALONIA
    Avalonia
#endif
    .I18N.Abstractions;
using Avalonia.Markup.Xaml.XamlIl.Runtime;

// ReSharper disable once CheckNamespace
#if WPF
namespace System.Windows;
#elif AVALONIA
namespace Avalonia.Markup.Xaml.MarkupExtensions;
#endif

#if WPF
[MarkupExtensionReturnType(typeof(string))]
[ContentProperty(nameof(Keys))]
[Localizability(LocalizationCategory.None, Modifiability = Modifiability.Unmodifiable,
    Readability = Readability.Unreadable)]
#endif
[DebuggerDisplay("Key = {Key}, Keys = {Keys}")]
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
#if AVALONIA
        ExpandoObjectPropertyAccessorPlugin.Register(target); //Register accessor plugin for ExpandoObject
        var updateActions = new List<Action>();
#endif
        foreach (var type in Assembly.GetEntryAssembly()?.GetTypes() ?? Type.EmptyTypes)
        {
            if (!type.IsSubclassOf(typeof(ResourceProviderBase))) continue;
#if WPF
            RegisterLanguageSource(FormatterServices.GetUninitializedObject(type) as ResourceProviderBase, false, out _);
#elif AVALONIA
            if (RegisterLanguageSource(FormatterServices.GetUninitializedObject(type) as ResourceProviderBase, true,
                    out var redo))
            {
                updateActions.Add(redo!);
            }
#endif
        }
#if AVALONIA
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            foreach (var action in updateActions)
            {
                action();
            }
        });
#endif
    }

    #region Target

    private static readonly DependencyProperty KeyProperty = DependencyProperty.RegisterAttached
#if AVALONIA
        <I18NExtension, DependencyObject, object>
#endif
        (
            nameof(Key)
#if WPF
        ,
        typeof(object),
        typeof(I18NExtension),
        new PropertyMetadata(default)
#endif
        );


    private static readonly DependencyProperty TargetPropertyProperty = DependencyProperty.RegisterAttached
#if AVALONIA
        <I18NExtension, DependencyObject, DependencyProperty>
#endif
        (
            "TargetProperty"
#if WPF
        ,
        typeof(DependencyProperty),
        typeof(I18NExtension),
        new PropertyMetadata(default(DependencyProperty))
#endif
        );

    private static void SetTargetProperty(DependencyObject element, DependencyProperty value)
        => element.SetValue(TargetPropertyProperty, value);

    private static DependencyProperty GetTargetProperty(DependencyObject element)
        => (DependencyProperty)element.GetValue(TargetPropertyProperty)!;

    #endregion

    public static string? Translate(string key, string? fallbackValue = null)
    {
        return Target.TryGetValue(key, out var value)
            ? value as string ?? fallbackValue
            : fallbackValue;
    }

    private static bool RegisterLanguageSource(ResourceProviderBase? provider, 
        bool lazyInit,
        out Action? lazyInitAction)
    {
        lazyInitAction = null;
        if (provider is null) return false;

        CultureChanged += culture => provider.Culture = culture;

        var props = provider.GetType().GetProperties();

        provider.PropertyChanged += (o, e) => Update(o!, e.PropertyName!);
        Notifier.RegisterProvider(provider);
        lazyInitAction = () =>
        {
            foreach (var prop in props) Update(provider, prop.Name);
        };
        if (!lazyInit)
        {
            lazyInitAction();
        }

        return true;

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
#if AVALONIA
    [Content]
#endif
    public
#if WPF
        MicrosoftPleaseFixBindingCollection
#elif AVALONIA
        Collection<IBinding>
#endif
        Keys { get; } = new();

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
#if WPF
        BindingBase
#elif AVALONIA
        IBinding
#endif
        CreateBinding()
    {
        Binding? keyBinding = null;
        if (Key is string key)
        {
            keyBinding = new Binding(key)
            {
                Source        = Target,
                Mode          = BindingMode.OneWay,
                FallbackValue = key,
#if AVALONIA
                Priority = BindingPriority.LocalValue
#endif
            };
            if (Keys is not { Count: > 0 })
            {
#if WPF
                keyBinding.UpdateSourceTrigger = UpdateSourceTrigger.Explicit;
#endif
                keyBinding.Converter          = Converter;
                keyBinding.ConverterParameter = ConverterParameter;
                return keyBinding;
            }
        }

        var ret = new MultiBinding
        {
            Mode               = BindingMode.OneWay,
            ConverterParameter = ConverterParameter,
#if WPF
            UpdateSourceTrigger = UpdateSourceTrigger.Explicit,
#elif AVALONIA
            Priority        = BindingPriority.LocalValue,
            TargetNullValue = string.Empty
#endif
        };

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

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        if (serviceProvider.GetService(typeof(IProvideValueTarget)) is not IProvideValueTarget provideValueTarget)
            return this;
#if WPF
        if (provideValueTarget.TargetObject.GetType().FullName ==
            $"{nameof(System)}.{nameof(Windows)}.SharedDp") return this;
#endif
        if (provideValueTarget.TargetObject is not DependencyObject targetObject)
#if WPF
            return this;
#elif AVALONIA
        {
            if (provideValueTarget.TargetObject is not I18NExtension) return this;
            var reflect = provideValueTarget.GetType().GetField("ParentsStack");
            if (reflect is null) return this;
            var parentsStack = reflect.GetValue(provideValueTarget) as IList<object>;
            if (parentsStack is null) return this;
            targetObject = (parentsStack.Last() as DependencyObject)!;
        } 
#endif
        if (provideValueTarget.TargetProperty is not DependencyProperty targetProperty) return this;

        if (Key is null && Keys.Count == 0)
            throw new ArgumentNullException($"{nameof(Key)} or {nameof(Keys)} cannot both be null");
        if (Key is null && Keys is { Count: 1 })
        {
            Key = Keys[0];
            Keys.Clear();
        }

        var bindingBase = CreateBinding();
#if WPF
        BindingOperations.SetBinding
            (targetObject, targetProperty, bindingBase);
#endif

        if (bindingBase is MultiBinding)
        {
            SetTarget(targetObject, targetProperty);
        }

        return bindingBase
#if WPF
            .ProvideValue(serviceProvider)
#endif
            ;
    }


    private void I18NExtension_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
#if WPF
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
#elif AVALONIA
        if (sender is not StyledElement element) return;
        element.DataContextChanged -= I18NExtension_DataContextChanged;
        ResetBinding(element);
#endif
    }

    private void SetTarget(DependencyObject targetObject, DependencyProperty targetProperty)
    {
#if WPF
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
#elif AVALONIA
        if (targetObject is not StyledElement element) return;
        SetTargetProperty(element, targetProperty);
        element.DataContextChanged += I18NExtension_DataContextChanged;
#endif
    }

    private void ResetBinding(DependencyObject element)
    {
        if (Key is string && Keys.All(x => x is LanguageBinding)) return;
        var targetProperty = GetTargetProperty(element);
        SetTargetProperty(element, null!);
        var binding = CreateBinding();
#if WPF
        BindingOperations.SetBinding(element, targetProperty, binding);
#elif AVALONIA
        element.Bind(targetProperty, binding);
#endif
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
            var res    = new object?[values.Count - 2];
            var template = isBindingList[0]
                ? GetValue(source, values[1] as string)
                : values[1] as string;
            if (string.IsNullOrEmpty(template) ||
                values.
#if WPF
                    Length
#elif AVALONIA
                    Count
#endif
                <= 2) return Converter?.Convert(template, targetType, ConverterParameter, culture) ?? template;

            for (var i = 1; i < isBindingList.Count; i++)
            {
                if (values[i + 1] == null)
                {
                    res[i - 1] = string.Empty;
                    continue;
                }

                res[i - 1] = isBindingList[i]
                    ? GetValue(source, values[i + 1] as string)
                    : values[i + 1] as string;
            }

            var val = string.Format(template!, res);
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
    /// <see cref="ResourceChangedNotifier"/> is singleton notifier for
    /// multibinding in case of avoid extra property notifier
    /// </summary>
    private class ResourceChangedNotifier(ExpandoObject source) : INotifyPropertyChanged
    {
        public ExpandoObject Source => source;

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

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

#if AVALONIA
    private class ExpandoObjectPropertyAccessorPlugin(ExpandoObject target) : IPropertyAccessorPlugin
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
#endif
}