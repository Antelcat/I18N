using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.XamlIl.Runtime;
using Avalonia.Metadata;
using EventArgs = System.EventArgs;
using ResourceProvider = Antelcat.I18N.Abstractions.ResourceProvider;

[assembly: XmlnsDefinition("https://github.com/avaloniaui", "Antelcat.I18N.Avalonia")]

namespace Antelcat.I18N.Avalonia;

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
    public Collection<BindingBase> Keys { get; } = [];


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