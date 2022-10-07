using System;
using System.Threading;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

using FluentAvalonia.UI.Controls;

namespace SampleApp.Views;

public partial class MainWindow : CoreWindow
{
    private CancellationTokenSource? _cts;

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

        BreadcrumbBar.ItemClicked += BreadcrumbBar_ItemClicked;
    }

    private async void BreadcrumbBar_ItemClicked(BreadcrumbBar sender, BreadcrumbBarItemClickedEventArgs args)
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();

        InfoBar.Content = $"Index: {args.Index}, Item: {args.Item}";
        InfoBar.IsOpen = true;
        try
        {
            await Task.Delay(5000, _cts.Token);
            InfoBar.IsOpen = false;
        }
        catch (OperationCanceledException)
        {
        }
    }
}
