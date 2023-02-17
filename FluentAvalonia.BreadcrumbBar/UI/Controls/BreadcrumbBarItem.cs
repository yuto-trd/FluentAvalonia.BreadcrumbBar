using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;

namespace FluentAvalonia.UI.Controls;

[TemplatePart("PART_LayoutRoot", typeof(Grid))]
[TemplatePart("PART_ItemButton", typeof(Button))]
[TemplatePart("PART_ContentPresenter", typeof(ContentPresenter))]
[TemplatePart("PART_EllipsisTextBlock", typeof(TextBlock))]
[TemplatePart("PART_ChevronTextBlock", typeof(FontIcon))]
[PseudoClasses(":last", ":ellipsis", ":ellipsis-dropdown")]
public class BreadcrumbBarItem : ContentControl
{
    private BreadcrumbBar _parent;
    private int _itemIndex;
    private Button _button;

    public BreadcrumbBarItem()
    {

    }

    internal bool CreatedByBreadcrumbElementFactory { get; set; }

    internal bool IsEllipsisDropDownItem => PseudoClasses.Contains(":ellipsis-dropdown");

    internal bool IsEllipsisButton => PseudoClasses.Contains(":ellipsis");

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _button = e.NameScope.Find<Button>("PART_ItemButton");
        _button.Click += OnButtonClick;
    }

    private void RaiseItemClickedEvent(object content, int index)
    {
        if (CreatedByBreadcrumbElementFactory)
        {
            _parent?.RaiseItemClickedEvent(content, index);
        }
        else
        {
            _parent?.RaiseItemClickedEvent(DataContext, index);
        }
    }

    private void OnButtonClick(object sender, RoutedEventArgs e)
    {
        if (_parent != null)
        {
            if (IsEllipsisButton)
            {
                _parent.OpenFlyout();
            }
            else if (IsEllipsisDropDownItem)
            {
                _parent.CloseFlyout();
                RaiseItemClickedEvent(Content, _itemIndex);
            }
            else
            {
                RaiseItemClickedEvent(Content, _itemIndex);
            }
        }
    }

    internal void SetParentBreadcrumb(BreadcrumbBar parent)
    {
        _parent = parent;
    }

    internal void SetEllipsis(bool value)
    {
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
