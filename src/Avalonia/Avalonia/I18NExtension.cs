using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Dynamic;
using System.Reflection;
using System.Runtime.Serialization;
using Antelcat.I18N.Abstractions;
using Avalonia.Data;
using Avalonia.Metadata;
using Avalonia.Threading;

namespace Avalonia.Markup.Xaml.MarkupExtensions;

public partial class I18NExtension
{
    static I18NExtension()
    {
        var target = new ExpandoObject();
        Target   = target;
        Notifier = new ResourceChangedNotifier(target);
        ExpandoObjectPropertyAccessorPlugin.Register(target); //Register accessor plugin for ExpandoObject
        var updateActions = new List<Action>();
        /*foreach (var type in Assembly.GetEntryAssembly()?.GetTypes() ?? Type.EmptyTypes)
        {
            if (!type.IsSubclassOf(typeof(ResourceProviderBase))) continue;

            if (RegisterLanguageSource(FormatterServices.GetUninitializedObject(type) as ResourceProviderBase, true,
                    out var redo))
            {
                updateActions.Add(redo!);
            }
        }*/
        
        lock (ResourceProvider.Providers)
        {
            foreach (var provider in ResourceProvider.Providers)
            {
                var action = RegisterLanguageSource(provider, true);
                updateActions.Add(action);
            }

            ResourceProvider.Providers.CollectionChanged += (_, e) =>
            {
                if(e.Action != NotifyCollectionChangedAction.Add)return;
                foreach (var provider in e.NewItems.OfType<ResourceProvider>())
                {
                    RegisterLanguageSource(provider, false);
                }
            };
        }


        Dispatcher.UIThread.InvokeAsync(() =>
        {
            foreach (var action in updateActions)
            {
                action();
            }

            Notifier.ForceUpdate();
        });
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
    public Collection<IBinding> Keys { get; } = new();

    private object ProvideValueInternal(IServiceProvider serviceProvider)
    {
        if (serviceProvider.GetService(typeof(IProvideValueTarget)) is not IProvideValueTarget provideValueTarget)
            return this;

        if (provideValueTarget.TargetObject is not StyledElement targetObject) 
        {
            // Avalonia please fix TargetObject not match
            if (provideValueTarget.TargetObject is not I18NExtension) return this;
            var reflect = provideValueTarget.GetType().GetField("ParentsStack");
            if (reflect is null) return this;
            if (reflect.GetValue(provideValueTarget) is not IList<object> parentsStack) return this;
            targetObject = (parentsStack.Last() as StyledElement)!;
        }
        if (provideValueTarget.TargetProperty is not AvaloniaProperty targetProperty) return this;

        CheckArgument();

        var bindingBase = CreateBinding();

        if (bindingBase is MultiBinding)
        {
            SetTarget(targetObject, targetProperty);
        }

        return bindingBase;
    }

    private void SetTarget(AvaloniaObject targetObject, AvaloniaProperty targetProperty)
    {
        if (targetObject is not StyledElement element) return;
        SetTargetProperty(element, targetProperty);
        element.DataContextChanged += I18NExtension_DataContextChanged;
    }
    
    private void I18NExtension_DataContextChanged(object sender, EventArgs e)
    {
        if (sender is not StyledElement element) return;
        element.DataContextChanged -= I18NExtension_DataContextChanged;
        ResetBinding(element);
    }

    private static void SetBinding(AvaloniaObject element, AvaloniaProperty targetProperty, IBinding binding)
    {
        element.Bind(targetProperty, binding);
    }

    private MultiBinding CreateMultiBinding()
    {
        return new MultiBinding
        {
            Mode               = BindingMode.OneWay,
            ConverterParameter = ConverterParameter,
            Priority           = BindingPriority.LocalValue,
            TargetNullValue    = string.Empty
        };
    }
    
      private IBinding CreateBinding()
    {
        Binding? keyBinding = null;
        if (Key is not string key) return MapMultiBinding(keyBinding);
        keyBinding = new Binding(key)
        {
            Source        = Target,
            Mode          = BindingMode.OneWay,
            FallbackValue = key,
            Priority      = BindingPriority.LocalValue
        };
        if (Keys is { Count: > 0 }) return MapMultiBinding(keyBinding);
        keyBinding.Converter          = Converter;
        keyBinding.ConverterParameter = ConverterParameter;
        return keyBinding;

    }
    

    private partial class ResourceChangedNotifier
    {
        public void ForceUpdate() => OnPropertyChanged(nameof(Source));
    }
}