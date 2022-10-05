using System;

namespace FluentAvalonia.UI.Controls;

public class BreadcrumbBarItemClickedEventArgs : EventArgs
{
    internal BreadcrumbBarItemClickedEventArgs(int index, object item)
    {
        Index = index;
        Item = item;
    }

    public int Index { get; }

    public object Item { get; }
}
