using System.Globalization;
using System.Runtime.CompilerServices;
using Antelcat.I18N.Avalonia.Demo.Models;
using Antelcat.I18N.Avalonia.Demo.ViewModels;
using Avalonia.Controls;
using Avalonia.Threading;

namespace Antelcat.I18N.Avalonia.Demo.Views
{
    public partial class MainWindow : Window
    {
        private static bool gen;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MainWindow()
        {
            InitializeComponent();
            if (gen) return;
            gen = true;
            Task.Delay(2000).ContinueWith(t =>
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    new MainWindow
                    {
                        DataContext = new ViewModel()
                    }.Show();
                });
            });
        }
    }
}
