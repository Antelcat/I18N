using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Reflection;
using System.Runtime.Serialization;
using System.Windows.Data;
using System.Windows.Markup;
using Antelcat.Wpf.I18N.Abstractions;

// ReSharper disable once CheckNamespace
namespace System.Windows;

[MarkupExtensionReturnType(typeof(string))]
public class I18NExtension : MarkupExtension
{
	private static readonly IDictionary<string, object?> Target = new ExpandoObject();

	private static event CultureChangedHandler? CultureChanged;

	private delegate void CultureChangedHandler(CultureInfo culture);

	public static CultureInfo Culture
	{
		set => CultureChanged?.Invoke(value);
	}

	static I18NExtension()
	{
		foreach (var type in Assembly.GetEntryAssembly()?.GetTypes() ?? Type.EmptyTypes)
		{
			if (!type.IsSubclassOf(typeof(ResourceProviderBase))) continue;
			RegisterLanguageSource(FormatterServices.GetUninitializedObject(type) as ResourceProviderBase);
		}
	}
	
	private static void RegisterLanguageSource(ResourceProviderBase? provider)
	{
		if (provider is null) return;
		
		CultureChanged += culture => provider.Culture = culture;
		
		var props  = provider.GetType().GetProperties();

		provider.PropertyChanged += (o, e) => Update(o!, e.PropertyName!);
		foreach (var prop in props) Update(provider, prop.Name);

		return;

		void Update(object o, string s)
		{
			var val = Array.Find(props, x => x.Name.Equals(s))?.GetValue(o, null);
			
			if (val != null) Target[s] = val;
		}
	}

	private readonly DependencyObject proxy = new();

	public static readonly DependencyProperty KeyProperty = DependencyProperty.RegisterAttached(
		nameof(Key),
		typeof(object),
		typeof(I18NExtension),
		new PropertyMetadata(default));

	public object? Key
	{
		get => proxy.GetValue(KeyProperty);
		set => proxy.SetValue(KeyProperty, value);
	}

	public static readonly DependencyProperty SourceProperty = DependencyProperty.RegisterAttached(
		nameof(Value),
		typeof(Binding),
		typeof(I18NExtension),
		new PropertyMetadata(default));

	public Binding? Value
	{
		get => (Binding)proxy.GetValue(SourceProperty);
		set => proxy.SetValue(SourceProperty, value);
	}

	private static readonly DependencyProperty TargetPropertyProperty = DependencyProperty.RegisterAttached(
		"TargetProperty",
		typeof(DependencyProperty),
		typeof(I18NExtension),
		new PropertyMetadata(default(DependencyProperty)));

	private static void SetTargetProperty(DependencyObject element, DependencyProperty value)
		=> element.SetValue(TargetPropertyProperty, value);

	private static DependencyProperty GetTargetProperty(DependencyObject element)
		=> (DependencyProperty)element.GetValue(TargetPropertyProperty);
	
	public IValueConverter? Converter { get; set; }

	public object? ConverterParameter { get; set; }

	public override object? ProvideValue(IServiceProvider serviceProvider)
	{
		if (serviceProvider.GetService(typeof(IProvideValueTarget)) is not IProvideValueTarget provideValueTarget)
			return this;
		if (provideValueTarget.TargetObject.GetType().FullName == "System.Windows.SharedDp") return this;
		if (provideValueTarget.TargetObject is not DependencyObject targetObject) return this;
		if (provideValueTarget.TargetProperty is not DependencyProperty targetProperty) return this;

		var target = Key ?? Value;
		switch (target)
		{
			case string key:
			{
				var binding = CreateLangBinding(key);
				BindingOperations.SetBinding(targetObject, targetProperty, binding);
				return binding.ProvideValue(serviceProvider);
			}
			case Binding keyBinding when targetObject is FrameworkElement element:
			{
				if (element.DataContext != null)
				{
					return SetLangBinding(element,
							targetProperty,
							keyBinding.Path,
							element.DataContext)?
						.ProvideValue(serviceProvider);
				}

				SetTargetProperty(element, targetProperty);
				element.DataContextChanged += LangExtension_DataContextChanged;

				break;
			}
			case Binding keyBinding when targetObject is FrameworkContentElement element:
			{
				if (element.DataContext != null)
				{
					return SetLangBinding(element,
							targetProperty,
							keyBinding.Path,
							element.DataContext)?
						.ProvideValue(serviceProvider);
				}

				SetTargetProperty(element, targetProperty);
				element.DataContextChanged += LangExtension_DataContextChanged;

				break;
			}
		}

		return string.Empty;
	}

	private void LangExtension_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
	{
		switch (sender)
		{
			case FrameworkElement element:
			{
				element.DataContextChanged -= LangExtension_DataContextChanged;
				ResetBinding(element, e.NewValue);
				break ;
			}
			case FrameworkContentElement element:
			{
				element.DataContextChanged -= LangExtension_DataContextChanged;
				ResetBinding(element, e.NewValue);
				break;
			}
		}
	}


	private void ResetBinding(
		DependencyObject element,
		object dataContext)
	{
		if (Key is not Binding keyBinding) return;

		var targetProperty = GetTargetProperty(element);
		SetTargetProperty(element, null!);
		SetLangBinding(element, targetProperty, keyBinding.Path, dataContext);
	}

	private BindingBase? SetLangBinding(
		DependencyObject targetObject,
		DependencyProperty? targetProperty,
		PropertyPath path,
		object dataContext)
	{
		if (targetProperty == null) return null;

		BindingOperations.SetBinding(targetObject,
			targetProperty,
			new Binding
			{
				Path   = path,
				Source = dataContext,
				Mode   = BindingMode.OneWay,
			});

		var key = targetObject.GetValue(targetProperty) as string;
		if (string.IsNullOrEmpty(key)) return null;

		var binding = CreateLangBinding(key);
		BindingOperations.SetBinding(targetObject, targetProperty, binding);
		return binding;
	}

	private BindingBase CreateLangBinding(string key)
	{
		var ret = new Binding(key)
		{
			Converter           = Converter,
			ConverterParameter  = ConverterParameter,
			Source              = TryFind(Target, key),
			Mode                = BindingMode.OneWay
		};
		if (Value == null)
		{
			ret.UpdateSourceTrigger = UpdateSourceTrigger.Explicit;
			return ret;
		}

		var multi = new MultiBinding
		{
			Converter           = new MultiValueLangConverter(Target, Converter, ConverterParameter, Key is string),
			Mode                = BindingMode.OneWay,
			UpdateSourceTrigger = UpdateSourceTrigger.Explicit,
			ConverterParameter  = ConverterParameter
		};
		multi.Bindings.Add(ret);
		multi.Bindings.Add(Value);
		return multi;
	}

	private static object TryFind(IDictionary<string, object?> target, string key)
	{
		if (!target.ContainsKey(key))
		{
			target[key] = key;
		}

		return target;
	}

	private class MultiValueLangConverter : IMultiValueConverter
	{
		private readonly IDictionary<string,object?> resource;
		private readonly IValueConverter?            converter;
		private readonly object?                     converterParameter;
		private readonly bool                        replace;
		public MultiValueLangConverter(IDictionary<string, object?> resource,
			IValueConverter? converter,
			object? converterParameter, 
			bool replace)
		{
			this.resource           = resource;
			this.converter          = converter;
			this.converterParameter = converterParameter;
			this.replace            = replace;
		}

		public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			var val = resource.TryGetValue(values[1].ToString() ?? string.Empty, out var ret)
				?  ret
				: values[1];
			if (replace)
			{
				val = string.Format(values[0] as string ?? string.Empty, val);
			}
			return converter?.Convert(val, targetType, converterParameter, culture) ?? val;
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}

}