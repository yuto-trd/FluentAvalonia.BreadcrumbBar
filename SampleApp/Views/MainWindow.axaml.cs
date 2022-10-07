using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace SampleApp.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
#if DEBUG
        this.AttachDevTools();
#endif

        Large.GetObservable(ToggleButton.IsCheckedProperty)
            .Subscribe(x =>
        {
            if (x == true)
            {
                BreadcrumbBar.Resources["BreadcrumbBarChevronPadding"] = new Thickness(4, 4, 4, 0);
                BreadcrumbBar.Resources["BreadcrumbBarItemFontWeight"] = FontWeight.SemiBold;
                BreadcrumbBar.Resources["BreadcrumbBarItemThemeFontSize"] = 24d;
                BreadcrumbBar.Resources["BreadcrumbBarChevronFontSize"] = 16d;
            }
            else
            {
                BreadcrumbBar.Resources.Remove("BreadcrumbBarChevronPadding");
                BreadcrumbBar.Resources.Remove("BreadcrumbBarItemFontWeight");
                BreadcrumbBar.Resources.Remove("BreadcrumbBarItemThemeFontSize");
                BreadcrumbBar.Resources.Remove("BreadcrumbBarChevronFontSize");
            }
        });
    }
}
