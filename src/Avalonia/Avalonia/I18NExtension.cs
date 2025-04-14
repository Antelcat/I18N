using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using Antelcat.I18N.Avalonia.Internals;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml.XamlIl.Runtime;
using Avalonia.Metadata;
using EventArgs = System.EventArgs;
using ResourceProvider = Antelcat.I18N.Abstractions.ResourceProvider;

namespace Avalonia.Markup.Xaml.MarkupExtensions;

public partial class I18NExtension
{
    public I18NExtension() { }
    
    static I18NExtension()
    {
        var target = new LanguageDictionary();
        Target   = target;
        Notifier = new ResourceChangedNotifier(target);
        SourceBinding = new(nameof(ResourceChangedNotifier.Source))
        {
            Source = Notifier,
            Mode   = BindingMode.OneWay,
        };

        BindingPlugins.PropertyAccessors.Insert(0, NotifierPropertyAccessorPlugin.Instance);

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

    private readonly        AvaloniaObject proxy   = new();
    private static readonly List<Window>   windows = [];

    #region Target

    private static readonly AvaloniaProperty KeyProperty = AvaloniaProperty.RegisterAttached
        <I18NExtension, AvaloniaObject, object>(nameof(Key));
    
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
            if (serviceProvider.GetService(typeof(IAvaloniaXamlIlParentStackProvider
                )) is IAvaloniaXamlIlParentStackProvider

                {
                    Parents: { } stack
                })
            {
                if (stack.FirstOrDefault() is StyledElement control)
                {
                    void Initialized(object _, EventArgs __)
                    {
                        Notifier.ForceUpdate();
                        control.Initialized -= Initialized;
                    }

                    control.Initialized += Initialized;
                }
            }

        }
    }

    private static MultiBinding CreateMultiBinding() =>
        new()
        {
            Mode     = BindingMode.OneWay,
            Priority = BindingPriority.LocalValue,
            Bindings = { SourceBinding }
        };
 

    private static partial void RegisterCultureChanged(ResourceProvider provider)
    {
        CultureChanged += culture =>
        {
            provider.Culture = culture;
        };
    }

    partial class ResourceChangedNotifier
    {
        public void ForceUpdate() => OnPropertyChanged(nameof(Source));
    }

}