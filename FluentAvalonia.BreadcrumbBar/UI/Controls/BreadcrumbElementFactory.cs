using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Logging;

namespace FluentAvalonia.UI.Controls;

public class BreadcrumbElementFactory : ElementFactory
{
    private IElementFactory? _itemTemplateWrapper;
    private readonly List<BreadcrumbBarItem> _breadcrumbPool = new(4);

    public void UserElementFactory(object? newValue)
    {
        _itemTemplateWrapper = newValue as IElementFactory;
        if (_itemTemplateWrapper is null)
        {
            // ItemTemplate set does not implement IElementFactoryShim. We also want to support DataTemplate.
            if (newValue is IDataTemplate dt)
            {
                _itemTemplateWrapper = new ItemTemplateWrapper(dt);
            }
            else if (newValue is DataTemplateSelector dts)
            {
                _itemTemplateWrapper = new ItemTemplateWrapper(dts);
            }
        }
    }

    protected override Control GetElementCore(ElementFactoryGetArgs args)
    {
        var newContent = args.Data;

        if (newContent is BreadcrumbBarItem breadcrumbItem)
        {
            return breadcrumbItem;
        }

        if (_itemTemplateWrapper != null)
        {
            newContent = _itemTemplateWrapper.GetElement(args);
        }

        if (newContent is BreadcrumbBarItem breadcrumbItem2)
        {
            return breadcrumbItem2;
        }

        // Get or create a wrapping container for the data
        BreadcrumbBarItem breadcrumbItem3;
        if (_breadcrumbPool.Count > 0)
        {
            breadcrumbItem3 = _breadcrumbPool[^1];
            _breadcrumbPool.RemoveAt(_breadcrumbPool.Count - 1);
        }
        else
        {
            breadcrumbItem3 = new BreadcrumbBarItem();
        }

        breadcrumbItem3.CreatedByBreadcrumbElementFactory = true;

        if (_itemTemplateWrapper != null)
        {
            if (_itemTemplateWrapper is ItemTemplateWrapper itw)
            {
                var tempArgs = new ElementFactoryRecycleArgs
                {
                    Element = newContent as Control
                };
                _itemTemplateWrapper.RecycleElement(tempArgs);

                breadcrumbItem3.Content = args.Data;
                breadcrumbItem3.ContentTemplate = itw;
                return breadcrumbItem3;
            }
        }

        breadcrumbItem3.Content = newContent;
        return breadcrumbItem3;
    }

    protected override void RecycleElementCore(ElementFactoryRecycleArgs args)
    {
        if (args.Element != null)
        {
            bool isEllipsisDropDownItem = false; // Use of isEllipsisDropDownItem is workaround for
                                                 // crashing bug when attempting to show ellipsis dropdown after clicking one of its items.

            if (args.Element is BreadcrumbBarItem breadcrumbItem)
            {
                // Check whether we wrapped the element in a NavigationViewItem ourselves.
                // If yes, we are responsible for recycling it.
                if (breadcrumbItem.CreatedByBreadcrumbElementFactory)
                {
                    breadcrumbItem.CreatedByBreadcrumbElementFactory = false;
                    UnlinkElementFromParent(args);
                    args.Element = null;

                    _breadcrumbPool.Add(breadcrumbItem);

                    isEllipsisDropDownItem = breadcrumbItem.IsEllipsisDropDownItem;

                    // Retrieve the proper element that requires recycling for a user defined item template
                    // and update the args correspondingly
                    if (_itemTemplateWrapper != null)
                    {
                        // TODO: Retrieve the element and add to the args
                    }
                }
            }

            if (_itemTemplateWrapper != null && isEllipsisDropDownItem)
            {
                _itemTemplateWrapper.RecycleElement(args);
            }
            else
            {
                UnlinkElementFromParent(args);
            }
        }
    }

    private static void UnlinkElementFromParent(ElementFactoryRecycleArgs args)
    {
        // We want to unlink the containers from the parent repeater
        // in case we are required to move it to a different repeater.
        if (args.Parent is Panel p && args.Element is not null)
        {
            p.Children.Remove(args.Element);
        }
    }
}

internal class ItemTemplateWrapper : ElementFactory
{
    // Internal property to RecyclePool, we'll expose here
    public static readonly AttachedProperty<IDataTemplate> OriginTemplateProperty =
        AvaloniaProperty.RegisterAttached<ItemTemplateWrapper, Control, IDataTemplate>("OriginTemplate");

    private readonly IDataTemplate? _dataTemplate;
    private readonly DataTemplateSelector? _dataTemplateSelector;

    public ItemTemplateWrapper(IDataTemplate? dataTemplate) => _dataTemplate = dataTemplate;

    public ItemTemplateWrapper(DataTemplateSelector dts) => _dataTemplateSelector = dts;

    protected override Control GetElementCore(ElementFactoryGetArgs args)
    {
        var selectedTemplate = _dataTemplate ?? _dataTemplateSelector?.SelectTemplate(args.Data);

        // Check if selected template we got is valid
        if (selectedTemplate == null)
        {
            // Null template, use other SelectTemplate method
            selectedTemplate = _dataTemplateSelector?.SelectTemplate(args.Data, null);

            // WinUI errors out here, we'll just use FuncDataTemplate.Default
            if (selectedTemplate == null)
            {
                selectedTemplate = FuncDataTemplate.Default;
                Logger.TryGet(LogEventLevel.Information, "NavigationViewItemsFactory")?
                    .Log("", $"No DataTemplate found for type {args.Data?.GetType()}. Using default instead");
            }
        }

        var recPool = RecyclePool.GetPoolInstance(selectedTemplate);
        Control? element = null;

        if (recPool != null)
        {
            element = recPool.TryGetElement(string.Empty, args.Parent);
        }

        if (element == null)
        {
            // no element was found in recycle pool, create a new element
            element = selectedTemplate.Build(args.Data);

            // Template returned null, so insert empty element to render nothing
            // We shouldn't encounter this here, but just in case
            element ??= new Rectangle();

            element.SetValue(OriginTemplateProperty, selectedTemplate);
        }

        // I believe DataTemplate.LoadContent() in WinUI also applies the DataContext, so we'll do
        // that here. If we don't, for some reason, we can get additional elements in the ItemsRepeater
        // For example, comment out the line below & run the sample app, scroll to the DataBinding
        // NavView example, open dev tools and scope to one of the NVIs to find the parent ItemsRepeater
        // You'll see there are 4 NVIs, which is correct, but you'll see more than one NVISeparator
        // only one is correctly arranged, any additional are arranged offscreen
        // I cannot figure out why this is happening, and I have no idea whether its a bug in WinUI,
        // the Avalonia port, or something I'm doing. This seems to fix it though, so YEET
        element.DataContext = args.Data;
        return element;
    }

    protected override void RecycleElementCore(ElementFactoryRecycleArgs args)
    {
        var element = args.Element;
        if (element != null)
        {
            var selectedTemplate = _dataTemplate ?? element.GetValue(OriginTemplateProperty);
            if (selectedTemplate is not null)
            {
                var recPool = RecyclePool.GetPoolInstance(selectedTemplate);
                if (recPool is null)
                {
                    recPool = new RecyclePool();
                    RecyclePool.SetPoolInstance(selectedTemplate, recPool);
                }

                recPool.PutElement(element, string.Empty, args.Parent);
            }
        }

    }
}
