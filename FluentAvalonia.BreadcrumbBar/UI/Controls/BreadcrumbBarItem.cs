using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;

namespace FluentAvalonia.UI.Controls;

[TemplatePart("PART_LayoutRoot", typeof(Grid))]
[TemplatePart("PART_ItemButton", typeof(Button))]
[TemplatePart("PART_ContentPresenter", typeof(ContentPresenter))]
[TemplatePart("PART_EllipsisTextBlock", typeof(TextBlock))]
[TemplatePart("PART_ChevronTextBlock", typeof(TextBlock))]
[PseudoClasses(":last", ":ellipsis", ":ellipsis-dropdown")]
public class BreadcrumbBarItem : ContentControl
{
    private BreadcrumbBar _parent;
    private int _itemIndex;
    private Button _button;
    private Flyout _ellipsisFlyout;
    private BreadcrumbElementFactory _ellipsisElementFactory;
    private IDataTemplate _ellipsisDropDownItemDataTemplate;
    private ItemsRepeater _ellipsisItemsRepeater;
    private BreadcrumbBarItem _ellipsisItem;

    public BreadcrumbBarItem()
    {

    }

    internal bool CreatedByBreadcrumbElementFactory { get; set; }

    internal bool IsEllipsisDropDownItem => PseudoClasses.Contains(":ellipsis-dropdown");

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _button = e.NameScope.Find<Button>("PART_ItemButton");
        _button.Click += OnButtonClick;
    }


    private void RaiseItemClickedEvent(object content, int index)
    {
        _parent?.RaiseItemClickedEvent(content, index);
    }

    private void OnButtonClick(object sender, RoutedEventArgs e)
    {
        if (_parent != null && PseudoClasses.Contains(":ellipsis"))
        {
            // Open Flyout

            List<object> hiddenElements = _parent.HiddenElements();
            hiddenElements.Reverse();

            if (_ellipsisDropDownItemDataTemplate is { })
            {
                _ellipsisElementFactory.UserElementFactory(_ellipsisDropDownItemDataTemplate);
            }

            if (_ellipsisItemsRepeater is { })
            {
                _ellipsisItemsRepeater.Items = hiddenElements;
            }

            OpenFlyout();
        }
        else if (IsEllipsisDropDownItem
            && _ellipsisItem is { })
        {
            // Once an element has been clicked, close the flyout
            _ellipsisItem.CloseFlyout();
            _ellipsisItem.RaiseItemClickedEvent(Content, _itemIndex - 1);
        }
        else
        {
            RaiseItemClickedEvent(Content, _itemIndex - 1);
        }
    }

    private void OpenFlyout()
    {
        _ellipsisFlyout?.ShowAt(this);
    }

    private void CloseFlyout()
    {
        _ellipsisFlyout?.Hide();
    }

    private void InstantiateFlyout()
    {
        if (_ellipsisFlyout != null && _ellipsisItemsRepeater != null)
        {
            return;
        }

        // Only if the element has been created visually, instantiate the flyout
        // Create ItemsRepeater and set the DataTemplate 
        var ellipsisItemsRepeater = new ItemsRepeater();
        ellipsisItemsRepeater.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;

        _ellipsisElementFactory = new BreadcrumbElementFactory();
        ellipsisItemsRepeater.ItemTemplate = _ellipsisElementFactory;

        if (_ellipsisDropDownItemDataTemplate is { })
        {
            _ellipsisElementFactory.UserElementFactory(_ellipsisDropDownItemDataTemplate);
        }

        ellipsisItemsRepeater.ElementPrepared += OnFlyoutElementPreparedEvent;
        ellipsisItemsRepeater.ElementIndexChanged += OnFlyoutElementIndexChangedEvent;

        _ellipsisItemsRepeater = ellipsisItemsRepeater;
        _ellipsisFlyout = new Flyout();
        _ellipsisFlyout.FlyoutPresenterClasses.Set("BreadcrumbBarEllipsisFlyout", true);

        // Set the repeater as the content.
        _ellipsisFlyout.Content = ellipsisItemsRepeater;
        _ellipsisFlyout.Placement = FlyoutPlacementMode.Bottom;
    }

    private void OnFlyoutElementPreparedEvent(object sender, ItemsRepeaterElementPreparedEventArgs e)
    {
        if (e.Element is BreadcrumbBarItem ellipsisDropDownItem)
        {
            ellipsisDropDownItem.SetIsEllipsisDropDownItem(true /*isEllipsisDropDownItem*/);
        }

        UpdateFlyoutIndex(e.Element, e.Index);
    }

    private void OnFlyoutElementIndexChangedEvent(object sender, ItemsRepeaterElementIndexChangedEventArgs e)
    {
        UpdateFlyoutIndex(e.Element, e.NewIndex);
    }

    private void UpdateFlyoutIndex(IControl element, int index)
    {
        if (_ellipsisItemsRepeater is { })
        {
            if (_ellipsisItemsRepeater.ItemsSourceView is { } itemSourceView)
            {
                int itemCount = itemSourceView.Count;

                if (element is BreadcrumbBarItem ellipsisDropDownItem)
                {
                    ellipsisDropDownItem.SetEllipsisItem(this);
                    ellipsisDropDownItem.SetIndex(itemCount - index);
                }
            }
        }
    }

    internal void SetParentBreadcrumb(BreadcrumbBar parent)
    {
        _parent = parent;
    }

    private void SetEllipsisItem(BreadcrumbBarItem ellipsisItem)
    {
        _ellipsisItem = ellipsisItem;
    }

    internal void SetEllipsisDropDownItemDataTemplate(IDataTemplate newDataTemplate)
    {
        _ellipsisDropDownItemDataTemplate = newDataTemplate;
    }

    internal void SetEllipsis(bool value)
    {
        if (value)
        {
            InstantiateFlyout();
        }

        PseudoClasses.Set(":ellipsis", value);
    }

    internal void SetLast(bool value)
    {
        PseudoClasses.Set(":last", value);
    }

    internal void SetIsEllipsisDropDownItem(bool value)
    {
        PseudoClasses.Set(":ellipsis-dropdown", value);
    }

    internal void SetIndex(int itemIndex)
    {
        _itemIndex = itemIndex;
    }
}
