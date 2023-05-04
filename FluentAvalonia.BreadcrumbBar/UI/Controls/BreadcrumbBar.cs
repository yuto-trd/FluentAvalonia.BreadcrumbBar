using System.Collections;
using System.Collections.Specialized;

using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Metadata;

using FluentAvalonia.Core;

namespace FluentAvalonia.UI.Controls;

[TemplatePart("PART_ItemsRepeater", typeof(ItemsRepeater))]
[TemplatePart("PART_Ellipsis", typeof(BreadcrumbBarItem))]
[TemplatePart("PART_Grid", typeof(Grid))]
public class BreadcrumbBar : TemplatedControl
{
    public static readonly DirectProperty<BreadcrumbBar, IEnumerable> ItemsSourceProperty =
        AvaloniaProperty.RegisterDirect<BreadcrumbBar, IEnumerable>(nameof(ItemsSource),
            x => x.ItemsSource, (x, v) => x.ItemsSource = v);

    public static readonly StyledProperty<IDataTemplate?> ItemTemplateProperty =
        AvaloniaProperty.Register<BreadcrumbBar, IDataTemplate?>(nameof(ItemTemplate));

    private IEnumerable _items = new AvaloniaList<object>();
    private ItemsRepeater? _itemsRepeater;
    private BreadcrumbElementFactory? _itemsRepeaterElementFactory;
    private readonly BreadcrumbBarDataProvider _dataProvider;
    private BreadcrumbBarItem? _ellipsisBreadcrumbBarItem;
    private BreadcrumbBarItem? _lastBreadcrumbBarItem;
    private Grid? _grid;
    private bool _appliedTemplate;
    private bool _layoutInitialized;
    private ItemsRepeater? _ellipsisItemsRepeater;
    private Flyout? _ellipsisFlyout;

    private readonly float _topNavigationRecoveryGracePeriodWidth = 5f;

    public BreadcrumbBar()
    {
        _itemsRepeaterElementFactory = new BreadcrumbElementFactory();
        _dataProvider = new BreadcrumbBarDataProvider(this);
        _dataProvider.OnRawDataChanged((args) => OnDataSourceChanged(args));
    }

    [Content]
    public IEnumerable ItemsSource
    {
        get => _items;
        set
        {
            var old = _items;
            if (SetAndRaise(ItemsSourceProperty, ref _items, value))
            {
                if (_items is INotifyCollectionChanged oldINCC)
                {
                    oldINCC.CollectionChanged -= OnItemsCollectionChanged;
                }
                if (value is INotifyCollectionChanged newINCC)
                {
                    newINCC.CollectionChanged += OnItemsCollectionChanged;
                }
                UpdateItemsRepeaterItemsSource();
            }
        }
    }

    public IDataTemplate? ItemTemplate
    {
        get => GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    public event TypedEventHandler<BreadcrumbBar, BreadcrumbBarItemClickedEventArgs>? ItemClicked;

    private double MeasureGridDesiredWidth(Size availableSize) =>
        LayoutHelper.MeasureChild(_grid, availableSize, new Thickness()).Width;

    private double MeasureItemsRepeaterDesiredWidth(Size availableSize) =>
        LayoutHelper.MeasureChild(_itemsRepeater, availableSize, new Thickness()).Width;

    private bool HasBreadcrumbBarItemNotInPrimaryList() =>
        _dataProvider.PrimaryListSize != _dataProvider.Size;

    internal void RaiseItemClickedEvent(object? content, int index)
    {
        if (content is not null)
        {
            ItemClicked?.Invoke(this, new BreadcrumbBarItemClickedEventArgs(index, content));
        }
    }

    internal void OpenFlyout()
    {
        object[] hiddenElements = _dataProvider.GetOverflowItems().ToArray();
        Array.Reverse(hiddenElements);

        if (_ellipsisItemsRepeater is not null)
        {
            _ellipsisItemsRepeater.ItemsSource = hiddenElements;
        }

        if (_ellipsisBreadcrumbBarItem is not null)
        {
            _ellipsisFlyout?.ShowAt(_ellipsisBreadcrumbBarItem);
        }

    }

    internal void CloseFlyout()
    {
        _ellipsisFlyout?.Hide();
    }

    private void InstantiateFlyout()
    {
        if (_ellipsisFlyout != null && _ellipsisItemsRepeater != null)
        {
            return;
        }

        var ellipsisItemsRepeater = new ItemsRepeater
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            ItemTemplate = _itemsRepeaterElementFactory
        };

        ellipsisItemsRepeater.ElementPrepared += OnFlyoutElementPreparedEvent;
        ellipsisItemsRepeater.ElementIndexChanged += OnFlyoutElementIndexChangedEvent;

        _ellipsisItemsRepeater = ellipsisItemsRepeater;
        _ellipsisFlyout = new Flyout();
        _ellipsisFlyout.FlyoutPresenterClasses.Set("BreadcrumbBarEllipsisFlyout", true);

        // Set the repeater as the content.
        _ellipsisFlyout.Content = ellipsisItemsRepeater;
        _ellipsisFlyout.Placement = PlacementMode.Bottom;
    }

    private void OnFlyoutElementPreparedEvent(object? sender, ItemsRepeaterElementPreparedEventArgs e)
    {
        if (e.Element is BreadcrumbBarItem item)
        {
            item.SetIsEllipsisDropDownItem(true);

            // Set the parent breadcrumb reference for raising click events
            item.SetParentBreadcrumb(this);

            // Set the item index to fill the Index parameter in the ClickedEventArgs
            var itemIndex = _dataProvider.ConvertOverflowIndexToIndex((_ellipsisItemsRepeater?.ItemsSourceView?.Count ?? 0) - e.Index - 1);
            item.SetIndex(itemIndex);

            item.SetLast(false);
        }
    }

    private void OnFlyoutElementIndexChangedEvent(object? sender, ItemsRepeaterElementIndexChangedEventArgs e)
    {
        if (e.Element is BreadcrumbBarItem item)
        {
            var index = _dataProvider.ConvertOverflowIndexToIndex(e.OldIndex);
            item.SetIndex(index);
        }
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        _appliedTemplate = false;

        base.OnApplyTemplate(e);
        _itemsRepeater = e.NameScope.Get<ItemsRepeater>("PART_ItemsRepeater");

        _itemsRepeater.Layout = new StackLayout()
        {
            DisableVirtualization = true,
            Orientation = Orientation.Horizontal,
        };
        _itemsRepeater.ItemTemplate = _itemsRepeaterElementFactory;

        _itemsRepeater.ElementPrepared += OnElementPreparedEvent;
        _itemsRepeater.ElementIndexChanged += OnElementIndexChangedEvent;
        _itemsRepeater.ElementClearing += OnElementClearingEvent;

        _itemsRepeater.Loaded += OnBreadcrumbBarItemsRepeaterLoaded;

        _ellipsisBreadcrumbBarItem = e.NameScope.Get<BreadcrumbBarItem>("PART_Ellipsis");
        _ellipsisBreadcrumbBarItem.SetEllipsis(true);
        _ellipsisBreadcrumbBarItem.SetParentBreadcrumb(this);
        _ellipsisBreadcrumbBarItem.IsVisible = false;

        _grid = e.NameScope.Get<Grid>("PART_Grid");

        InstantiateFlyout();

        _appliedTemplate = true;

        UpdateItemsRepeaterItemsSource();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ItemTemplateProperty)
        {
            _itemsRepeaterElementFactory?.UserElementFactory(ItemTemplate);
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (double.IsInfinity(availableSize.Width))
        {
            // We have infinite space, so move all items to primary list
            _dataProvider.MoveAllItemsToPrimaryList();
        }
        else
        {
            // Determine if TopNav is in Overflow
            if (HasBreadcrumbBarItemNotInPrimaryList())
            {
                var desWidth = MeasureGridDesiredWidth(Size.Infinity);
                if (desWidth > availableSize.Width)
                {
                    ShrinkBreadcrumbBarSize(desWidth, availableSize);
                }
                else if (desWidth < availableSize.Width)
                {
                    var fullyRecoverWidth = _dataProvider.WidthRequiredToRecoveryAllItemsToPrimary();
                    if (availableSize.Width >= desWidth + fullyRecoverWidth + _topNavigationRecoveryGracePeriodWidth)
                    {
                        // It's possible to recover from Overflow to Normal state, so we restart the MeasureOverride from first step
                        ResetAndRearrangeBreadcrumbBarItems(availableSize);
                    }
                    else
                    {
                        var moveItems = FindMovableItemsRecoverToPrimaryList(availableSize.Width - desWidth);
                        _dataProvider.MoveItemsToPrimaryList(moveItems);
                    }
                }
            }
            else
            {
                var desWidth = MeasureGridDesiredWidth(Size.Infinity);
                if (desWidth > availableSize.Width)
                    ResetAndRearrangeBreadcrumbBarItems(availableSize);
            }

            if (!_layoutInitialized)
            {
                _layoutInitialized = true;
            }
        }

        return base.MeasureOverride(availableSize);
    }

    private void ResetAndRearrangeBreadcrumbBarItems(Size availableSize)
    {
        if (HasBreadcrumbBarItemNotInPrimaryList())
            _dataProvider.MoveAllItemsToPrimaryList();

        ArrangeBreadcrumbBarItems(availableSize);
    }

    private void ArrangeBreadcrumbBarItems(Size availableSize)
    {
        if (_ellipsisBreadcrumbBarItem is not null)
        {
            _ellipsisBreadcrumbBarItem.IsVisible = false;
            var desWidth = MeasureGridDesiredWidth(Size.Infinity);
            if (!(desWidth < availableSize.Width))
            {
                _ellipsisBreadcrumbBarItem.IsVisible = true;
                var desWidthForOB = MeasureGridDesiredWidth(Size.Infinity);
                _dataProvider.OverflowButtonWidth = desWidthForOB - desWidth;

                ShrinkBreadcrumbBarSize(desWidthForOB, availableSize);
            }
        }
    }

    private void ShrinkBreadcrumbBarSize(double desWidth, Size availableSize)
    {
        UpdateBreadcrumbBarWidthCache();

        var possibleWidthForPrimaryList = MeasureItemsRepeaterDesiredWidth(Size.Infinity) - (desWidth - availableSize.Width);
        if (possibleWidthForPrimaryList >= 0)
        {
            // Remove all items which is not visible except first item and selected item.
            var itemToBeRemoved = FindMovableItemsBeyondAvailableWidth(possibleWidthForPrimaryList);
            // should keep at least one item in primary
            KeepAtLeastOneItemInPrimaryList(itemToBeRemoved, true);
            _dataProvider.MoveItemsOutOfPrimaryList(itemToBeRemoved);
        }
    }

    private IList<int> FindMovableItemsRecoverToPrimaryList(double availableWidth)
    {
        List<int> toBeMoved = new List<int>();
        var size = _dataProvider.Size;

        int i = size - 1;
        while (i >= 0 && availableWidth > 0)
        {
            if (!_dataProvider.IsItemInPrimaryList(i))
            {
                var wid = _dataProvider.GetWidthForItem(i);
                if (availableWidth >= wid)
                {
                    toBeMoved.Add(i);
                    availableWidth -= wid;
                }
                else
                {
                    break;
                }
            }
            i--;
        }

        // Keep at one item is not in primary list. Two possible reason: 
        //  1, Most likely it's caused by m_topNavigationRecoveryGracePeriod
        //  2, virtualization and it doesn't have cached width
        if (i == size && !(toBeMoved.Count == 0))
        {
            toBeMoved.RemoveAt(toBeMoved.Count - 1);
        }

        return toBeMoved;
    }

    private IList<int> FindMovableItemsToBeRemovedFromPrimaryList(double widthAtLeastToBeRemoved)
    {
        List<int> toBeMoved = new List<int>();
        int i = 0;
        while (i < _dataProvider.Size && widthAtLeastToBeRemoved > 0)
        {
            if (_dataProvider.IsItemInPrimaryList(i))
            {
                toBeMoved.Add(i);
                widthAtLeastToBeRemoved -= _dataProvider.GetWidthForItem(i);
            }
            i++;
        }

        return toBeMoved;
    }

    private IList<int> FindMovableItemsBeyondAvailableWidth(double availableWidth)
    {
        List<int> toBeMoved = new List<int>();
        if (_itemsRepeater != null)
        {
            int size = _dataProvider.PrimaryListSize;

            double requiredWidth = 0;

            for (int i = size - 1; i >= 0; i--)
            {
                bool shouldMove = true;
                if (requiredWidth <= availableWidth)
                {
                    var cont = _itemsRepeater.TryGetElement(i);
                    if (cont != null)
                    {
                        requiredWidth += cont.DesiredSize.Width;
                        shouldMove = requiredWidth > availableWidth;
                    }
                    else
                    {
                        // item in virtualized but not realized
                    }
                }

                if (shouldMove)
                {
                    toBeMoved.Add(i);
                }
            }
        }

        return _dataProvider.ConvertPrimaryIndexToIndex(toBeMoved);
    }

    private void KeepAtLeastOneItemInPrimaryList(IList<int> itemInPrimaryToBeRemoved, bool shouldKeepFirst)
    {
        if (itemInPrimaryToBeRemoved.Count > 0 && itemInPrimaryToBeRemoved.Count == _dataProvider.PrimaryListSize)
        {
            if (shouldKeepFirst)
            {
                itemInPrimaryToBeRemoved.RemoveAt(0);
            }
            else
            {
                itemInPrimaryToBeRemoved.RemoveAt(itemInPrimaryToBeRemoved.Count - 1);
            }
        }
    }

    private void UpdateBreadcrumbBarWidthCache()
    {
        var size = _dataProvider.PrimaryListSize;
        if (_itemsRepeater != null)
        {
            for (int i = 0; i < size; i++)
            {
                if (_itemsRepeater.TryGetElement(i) is Control c)
                {
                    _dataProvider.UpdateWidthForPrimaryItem(i, c.DesiredSize.Width);
                }
                else
                {
                    break;
                }
            }
        }
    }

    private void UpdateItemsRepeaterItemsSource()
    {
        _dataProvider.SetDataSource(ItemsSource);

        UpdateItemsRepeaterItemsSource(_itemsRepeater!, _dataProvider.GetPrimaryItems());

        if (_appliedTemplate)
        {
            InvalidateMeasure();
        }
    }

    private static void UpdateItemsRepeaterItemsSource(ItemsRepeater ir, IEnumerable source)
    {
        if (ir != null)
        {
            ir.ItemsSource = source;
        }
    }

    private void OnDataSourceChanged(NotifyCollectionChangedEventArgs args)
    {
        CloseFlyout();

        // Assume that raw data doesn't change very often for navigationview.
        // So here is a simple implementation and for each data item change, it request a layout change
        // update this in the future if there is performance problem

        // If it's Uninitialized, it means that we didn't start the layout yet.

        if (_layoutInitialized)
        {
            _dataProvider.MoveAllItemsToPrimaryList();
        }
    }

    private void OnElementPreparedEvent(object? sender, ItemsRepeaterElementPreparedEventArgs e)
    {
        if (e.Element is BreadcrumbBarItem item)
        {
            item.SetIsEllipsisDropDownItem(false);

            // Set the parent breadcrumb reference for raising click events
            item.SetParentBreadcrumb(this);

            // Set the item index to fill the Index parameter in the ClickedEventArgs
            var itemIndex = _dataProvider.ConvertPrimaryIndexToIndex(e.Index);
            item.SetIndex(itemIndex);

            if (itemIndex == (_dataProvider.Size - 1))
            {
                item.SetLast(true);
                _lastBreadcrumbBarItem = item;
            }
            else
            {
                item.SetLast(false);
            }
        }
    }

    private void OnElementIndexChangedEvent(object? sender, ItemsRepeaterElementIndexChangedEventArgs e)
    {
        if (e.Element is BreadcrumbBarItem item)
        {
            var index = _dataProvider.ConvertPrimaryIndexToIndex(e.OldIndex);
            item.SetIndex(index);
        }
    }

    private void OnElementClearingEvent(object? sender, ItemsRepeaterElementClearingEventArgs e)
    {
        if (e.Element is BreadcrumbBarItem item)
        {
            item.SetEllipsis(false);
            item.SetLast(false);
        }
    }

    private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        ForceUpdateLastElement();
    }

    private void OnBreadcrumbBarItemsRepeaterLoaded(object? sender, RoutedEventArgs e)
    {
        if (_itemsRepeater is not null)
        {
            _itemsRepeater.Loaded -= OnBreadcrumbBarItemsRepeaterLoaded;
        }
        ForceUpdateLastElement();
    }

    private void ResetLastBreadcrumbBarItem()
    {
        if (_lastBreadcrumbBarItem != null)
            _lastBreadcrumbBarItem.SetLast(false);
    }

    private void ForceUpdateLastElement()
    {
        if (_itemsRepeater!.ItemsSourceView is not null)
        {
            var itemCount = _dataProvider.Size;

            var newLastItem = _itemsRepeater.TryGetElement(itemCount - 1) as BreadcrumbBarItem;
            UpdateLastElement(newLastItem);

            // If the given collection is empty, then reset the last element visual properties
            if (itemCount == 0)
                ResetLastBreadcrumbBarItem();
        }
        else
        {
            // Or if the ItemsSource was null, also reset the last breadcrumb Item
            ResetLastBreadcrumbBarItem();
        }
    }

    private void UpdateLastElement(BreadcrumbBarItem? newLastBreadcrumbBarItem)
    {
        // If the element is the last element in the array,
        // then we reset the visual properties for the previous
        // last element
        ResetLastBreadcrumbBarItem();

        newLastBreadcrumbBarItem?.SetLast(true);

        _lastBreadcrumbBarItem = newLastBreadcrumbBarItem;
    }
}
