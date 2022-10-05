using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace FluentAvalonia.UI.Controls;

public class BreadcrumbLayout : NonVirtualizingLayout
{
    private Size _availableSize;
    private BreadcrumbBarItem _ellipsisButton;
    private bool _ellipsisIsRendered;
    private int _firstRenderedItemIndexAfterEllipsis;
    private int _visibleItemsCount;

    public bool EllipsisIsRendered => _ellipsisIsRendered;

    public int FirstRenderedItemIndexAfterEllipsis => _firstRenderedItemIndexAfterEllipsis;

    public int GetVisibleItemsCount => _visibleItemsCount;

    protected override Size MeasureOverride(NonVirtualizingLayoutContext context, Size availableSize)
    {
        _availableSize = availableSize;

        double accumulatedCrumbsWidth = 0;
        double accumulatedCrumbsHeight = 0;

        for (int i = 0; i < context.Children.Count; ++i)
        {
            var breadcrumbItem = (BreadcrumbBarItem)context.Children[i];
            breadcrumbItem.Measure(availableSize);

            accumulatedCrumbsWidth += breadcrumbItem.DesiredSize.Width;
            accumulatedCrumbsHeight = Math.Max(accumulatedCrumbsHeight, breadcrumbItem.DesiredSize.Height);
        }

        // Save a reference to the ellipsis button to avoid querying for it multiple times
        if (context.Children.Count > 0)
        {
            if (context.Children[0] is BreadcrumbBarItem ellipsisButton)
            {
                _ellipsisButton = ellipsisButton;
            }
        }

        if (accumulatedCrumbsWidth > availableSize.Width)
        {
            _ellipsisIsRendered = true;
        }
        else
        {
            _ellipsisIsRendered = false;
        }

        return new Size(accumulatedCrumbsWidth, accumulatedCrumbsHeight);
    }

    private void ArrangeItem(ILayoutable breadcrumbItem, ref double accumulatedWidths, double maxElementHeight)
    {
        var elementSize = breadcrumbItem.DesiredSize;
        var arrangeRect = new Rect(accumulatedWidths, 0, elementSize.Width, maxElementHeight);
        breadcrumbItem.Arrange(arrangeRect);

        accumulatedWidths += elementSize.Width;
    }

    private void ArrangeItem(NonVirtualizingLayoutContext context, int index, ref double accumulatedWidths, double maxElementHeight)
    {
        var element = context.Children[index];
        ArrangeItem(element, ref accumulatedWidths, maxElementHeight);
    }

    private void HideItem(ILayoutable breadcrumbItem)
    {
        var arrangeRect = Rect.Empty;
        breadcrumbItem.Arrange(arrangeRect);
    }

    private void HideItem(NonVirtualizingLayoutContext context, int index)
    {
        var element = context.Children[index];
        HideItem(element);
    }

    private int GetFirstBreadcrumbBarItemToArrange(NonVirtualizingLayoutContext context)
    {
        int itemCount = context.Children.Count;
        double accumLength = context.Children[itemCount - 1].DesiredSize.Width +
            _ellipsisButton.DesiredSize.Width;

        for (int i = itemCount - 2; i >= 0; --i)
        {
            double newAccumLength = accumLength + context.Children[i].DesiredSize.Width;
            if (newAccumLength > _availableSize.Width)
            {
                return i + 1;
            }
            accumLength = newAccumLength;
        }

        return 0;
    }

    private double GetBreadcrumbBarItemsHeight(NonVirtualizingLayoutContext context, int firstItemToRender)
    {
        double maxElementHeight = 0;

        if (_ellipsisIsRendered)
        {
            maxElementHeight = _ellipsisButton.DesiredSize.Height;
        }

        for (int i = firstItemToRender; i < context.Children.Count; ++i)
        {
            maxElementHeight = Math.Max(maxElementHeight, context.Children[i].DesiredSize.Height);
        }

        return maxElementHeight;
    }

    protected override Size ArrangeOverride(NonVirtualizingLayoutContext context, Size finalSize)
    {
        int itemCount = context.Children.Count;
        int firstElementToRender = 0;
        _firstRenderedItemIndexAfterEllipsis = itemCount - 1;
        _visibleItemsCount = 0;

        // If the ellipsis must be drawn, then we find the index (x) of the first element to be rendered, any element with
        // a lower index than x will be hidden (except for the ellipsis button) and every element after x (including x) will
        // be drawn. At the very least, the ellipis and the last item will be rendered
        if (_ellipsisIsRendered)
        {
            firstElementToRender = GetFirstBreadcrumbBarItemToArrange(context);
            _firstRenderedItemIndexAfterEllipsis = firstElementToRender;
        }

        double accumulatedWidths = 0;
        double maxElementHeight = GetBreadcrumbBarItemsHeight(context, firstElementToRender);

        // If there is at least one element, we may render the ellipsis item
        if (itemCount > 0)
        {
            var ellipsisButton = _ellipsisButton;

            if (_ellipsisIsRendered)
            {
                ArrangeItem(ellipsisButton, ref accumulatedWidths, maxElementHeight);
            }
            else
            {
                HideItem(ellipsisButton);
            }
        }

        // For each item, if the item has an equal or larger index to the first element to render, then
        // render it, otherwise, hide it and add it to the list of hidden items
        for (int i = 1; i < itemCount; ++i)
        {
            if (i < firstElementToRender)
            {
                HideItem(context, i);
            }
            else
            {
                ArrangeItem(context, i, ref accumulatedWidths, maxElementHeight);
                ++_visibleItemsCount;
            }
        }

        //_breadcrumb.ReIndexVisibleElementsForAccessibility();

        return finalSize;
    }
}
