using System.Collections;
using System.Collections.Generic;
using System.Windows.Media;

namespace Antelcat.Wpf.I18N.Demo.ViewModels;

public class CodeViewModel
{
    public string Text { get; init; } = string.Empty;

    public SolidColorBrush Color { get; set; } = Brushes.White;
    
    public static implicit operator CodeViewModel((string text, SolidColorBrush color) tuple)
    {
        return new()
        {
            Text  = tuple.text,
            Color = tuple.color
        };
    }
}

public class CodeFragmentViewModel
{
    public IList<CodeViewModel> Codes { get; } = new List<CodeViewModel>(); 
}