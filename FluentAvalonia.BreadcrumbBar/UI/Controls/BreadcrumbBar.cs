using System.Collections;
using System.Collections.Specialized;

using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using Avalonia.Metadata;

using FluentAvalonia.Core;

namespace FluentAvalonia.UI.Controls;

[TemplatePart("PART_ItemsRepeater", typeof(ItemsRepeater))]
public class BreadcrumbBar : TemplatedControl
{
    public static readonly DirectProperty<BreadcrumbBar, IEnumerable> ItemsProperty =
        AvaloniaProperty.RegisterDirect<BreadcrumbBar, IEnumerable>(nameof(Items),
            x => x.Items, (x, v) => x.Items = v);

    public static readonly StyledProperty<IDataTemplate> ItemTemplateProperty =
        AvaloniaProperty.Register<BreadcrumbBar, IDataTemplate>(nameof(ItemTemplate));

    private IEnumerable _items = new AvaloniaList<object>();
    private ItemsRepeater _itemsRepeater;
    private BreadcrumbLayout _itemsRepeaterLayout;
    private BreadcrumbElementFactory _itemsRepeaterElementFactory;
    private ItemsSourceView<object> _breadcrumbItemsSourceView;
    private BreadcrumbBarItem _ellipsisBreadcrumbBarItem;
    private BreadcrumbIterable _itemsIterable;
    private BreadcrumbBarItem _lastBreadcrumbBarItem;

    public BreadcrumbBar()
    {
        _itemsRepeaterLayout = new BreadcrumbLayout();
        _itemsRepeaterElementFactory = new BreadcrumbElementFactory();
    }

    [Content]
    public IEnumerable Items
    {
        get => _items;
        set => SetAndRaise(ItemsProperty, ref _items, value);
    }

    public IDataTemplate ItemTemplate
    {
        get => GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    public event TypedEventHandler<BreadcrumbBar, BreadcrumbBarItemClickedEventArgs> ItemClicked;

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _itemsRepeater = e.NameScope.Find<ItemsRepeater>("PART_ItemsRepeater");

        _itemsRepeater.Layout = _itemsRepeaterLayout;
        _itemsRepeater.ItemTemplate = _itemsRepeaterElementFactory;

        _itemsRepeater.ElementPrepared += OnElementPreparedEvent;
        _itemsRepeater.ElementIndexChanged += OnElementIndexChangedEvent;
        _itemsRepeater.ElementClearing += OnElementClearingEvent;

        _itemsRepeater.Loaded += OnBreadcrumbBarItemsRepeaterLoaded;

        UpdateItemsRepeaterItemsSource();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ItemsProperty)
            UpdateItemsRepeaterItemsSource();
        else if (change.Property == ItemTemplateProperty)
        {
            _itemsRepeaterElementFactory.UserElementFactory(ItemTemplate);
            UpdateEllipsisBreadcrumbBarItemDropDownItemTemplate();
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        if (_ellipsisBreadcrumbBarItem is { })
            _ellipsisBreadcrumbBarItem.SetEllipsis(true);

        return base.MeasureOverride(availableSize);
    }

    internal List<object> HiddenElements()
    {
        // The hidden element list is generated in the BreadcrumbLayout during
        // the arrange method, so we retrieve the list from it
        if (_itemsRepeater is { })
        {
            if (_itemsRepeaterLayout is { })
            {
                if (_itemsRepeaterLayout.EllipsisIsRendered)
                {
                    var firstShownElement = _itemsRepeaterLayout.FirstRenderedItemIndexAfterEllipsis;
                    var hiddenCount = firstShownElement - 1;
                    var hiddenElements = new List<object>(hiddenCount);

                    if (_breadcrumbItemsSourceView is { })
                    {
                        for (var i = 0; i < hiddenCount; ++i)
                        {
                            hiddenElements.Add(_breadcrumbItemsSourceView.GetAt(i));
                        }
                    }

                    return hiddenElements;
                }
            }
        }

        // By default just return an empty list
        return new List<object>(0);
    }

    internal void RaiseItemClickedEvent(object content, int index)
    {
        ItemClicked?.Invoke(this, new BreadcrumbBarItemClickedEventArgs(index, content));
    }

    private void UpdateItemsRepeaterItemsSource()
    {
        if (_breadcrumbItemsSourceView != null)
            _breadcrumbItemsSourceView.CollectionChanged -= OnBreadcrumbBarItemsSourceCollectionChanged;

        _breadcrumbItemsSourceView = null;
        if (Items != null)
        {
            _breadcrumbItemsSourceView = ItemsSourceView<object>.GetOrCreate(Items);

            _breadcrumbItemsSourceView.CollectionChanged += OnBreadcrumbBarItemsSourceCollectionChanged;
        }
    }

    private void OnElementPreparedEvent(object sender, ItemsRepeaterElementPreparedEventArgs e)
    {
        if (e.Element is BreadcrumbBarItem item)
        {
            item.SetIsEllipsisDropDownItem(false /*isEllipsisDropDownItem*/);

            // Set the parent breadcrumb reference for raising click events
            item.SetParentBreadcrumb(this);

            // Set the item index to fill the Index parameter in the ClickedEventArgs
            var itemIndex = e.Index;
            item.SetIndex(itemIndex);

            // The first element is always the ellipsis item
            if (itemIndex == 0)
            {
                item.SetEllipsis(true);
                _ellipsisBreadcrumbBarItem = item;
                UpdateEllipsisBreadcrumbBarItemDropDownItemTemplate();
            }
            else
            {
                if (_breadcrumbItemsSourceView != null)
                {
                    var itemCount = _breadcrumbItemsSourceView.Count;

                    if (itemIndex == itemCount)
                        item.SetLast(true);
                    else
                    {
                        // Any other element just resets the visual properties
                        item.SetLast(false);
                    }
                }
            }
        }
    }

    private void OnElementIndexChangedEvent(object sender, ItemsRepeaterElementIndexChangedEventArgs e)
    {
        if (e.Element is BreadcrumbBarItem item)
            item.SetIndex(e.NewIndex);
    }

    private void OnElementClearingEvent(object sender, ItemsRepeaterElementClearingEventArgs e)
    {
        if (e.Element is BreadcrumbBarItem item)
        {
            item.SetEllipsis(false);
            item.SetLast(false);
        }
    }

    private void OnBreadcrumbBarItemsSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
    {
        _itemsIterable = new BreadcrumbIterable(Items);
        _itemsRepeater.Items = _itemsIterable;

        //Todo: ellipsisをテンプレート内に追加する
        //_itemsRepeater.Items = Items;

        ForceUpdateLastElement();
    }

    private void OnBreadcrumbBarItemsRepeaterLoaded(object sender, RoutedEventArgs e)
    {
        _itemsRepeater.Loaded -= OnBreadcrumbBarItemsRepeaterLoaded;

        OnBreadcrumbBarItemsSourceCollectionChanged(null, null);
    }

    private void UpdateEllipsisBreadcrumbBarItemDropDownItemTemplate()
    {
        // Copy the item template to the ellipsis item too
        if (_ellipsisBreadcrumbBarItem is { })
            _ellipsisBreadcrumbBarItem.SetEllipsisDropDownItemDataTemplate(ItemTemplate);
    }

    private void ResetLastBreadcrumbBarItem()
    {
        if (_lastBreadcrumbBarItem != null)
            _lastBreadcrumbBarItem.SetLast(false);
    }

    private void ForceUpdateLastElement()
    {
        if (_breadcrumbItemsSourceView != null)
        {
            var itemCount = _breadcrumbItemsSourceView.Count;

            var newLastItem = _itemsRepeater.TryGetElement(itemCount) as BreadcrumbBarItem;
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

    private void UpdateLastElement(BreadcrumbBarItem newLastBreadcrumbBarItem)
    {
        // If the element is the last element in the array,
        // then we reset the visual properties for the previous
        // last element
        ResetLastBreadcrumbBarItem();

        if (newLastBreadcrumbBarItem != null)
            newLastBreadcrumbBarItem.SetLast(true);

        _lastBreadcrumbBarItem = newLastBreadcrumbBarItem;
    }
}
