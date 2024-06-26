﻿using System.Globalization;
using System.Reflection;
using Antelcat.I18N.Avalonia.Demo.Models;
using Avalonia.Markup.Xaml.MarkupExtensions;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Antelcat.I18N.Avalonia.Demo.ViewModels;

public partial class ViewModel : ObservableObject
{
    public ViewModel()
    {
        selectedKey    =  AvailableKeys.FirstOrDefault();
        InputText      =  AvailableKeys.FirstOrDefault();
    }
    public CultureInfo Culture
    {
        get => culture;
        set
        {
            if (culture.EnglishName.Equals(value.EnglishName)) return;
            culture               = value;
            I18NExtension.Culture = value;
        }
    }

    private static CultureInfo culture = new("zh");

    public IList<CultureInfo> AvailableCultures { get; } = new List<CultureInfo>
    {
        new("zh"),
        new("en")
    };

    public IList<string> AvailableKeys { get; } =
        new LangKeys.__ResourcesProvider().Keys()
            .ToList();
    
    [ObservableProperty] private string? selectedKey;
    
    [ObservableProperty] private string? inputText;
}
