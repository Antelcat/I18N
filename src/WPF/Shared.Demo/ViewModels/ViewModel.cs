using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using Antelcat.I18N.WPF.Demo.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Antelcat.I18N.WPF.Demo.ViewModels;

public partial class ViewModel : ObservableObject
{
    public ViewModel()
    {
        selectedKey = AvailableKeys.FirstOrDefault();
    }
    public CultureInfo Culture
    {
        get => culture;
        set
        {
            if (culture.EnglishName.Equals(value.EnglishName)) return;
            culture               = value;
            I18NExtension.Culture = value;
            OnPropertyChanged();
        }
    }

    private CultureInfo culture = new("zh");

    public IList<CultureInfo> AvailableCultures { get; } = new List<CultureInfo>
    {
        new("zh"),
        new("en")
    };

    public IList<string> AvailableKeys { get; } =
        typeof(LangKeys)
            .GetFields(BindingFlags.Static | BindingFlags.Public)
            .Select(x => x.Name)
            .ToList();
    
    [ObservableProperty] private string? selectedKey;
    
    [ObservableProperty] private string? inputText;
}
