using System;
using System.Collections;

using Avalonia.Controls;

namespace FluentAvalonia.UI.Controls;

internal class BreadcrumbIterable : IEnumerable
{
    private readonly IEnumerable _items;

    public BreadcrumbIterable(IEnumerable items)
    {
        _items = items;
    }

    public IEnumerator GetEnumerator()
    {
        return new BreadcrumbIterator(_items);
    }
}

internal class BreadcrumbIterator : IEnumerator
{
    private int _currentIndex;
    private ItemsSourceView _breadcrumbItemsSourceView;
    private int _size;

    public BreadcrumbIterator(IEnumerable items)
    {
        _currentIndex = -1;

        if (items != null)
        {
            _breadcrumbItemsSourceView = new ItemsSourceView(items);

            // Add 1 to account for the leading null/ellipsis element
            _size = _breadcrumbItemsSourceView.Count + 1;
        }
        else
        {
            _size = 1;
        }
    }

    public object Current
    {
        get
        {
            if (_currentIndex == 0)
            {
                return null;
            }
            else if (HasCurrent())
            {
                return _breadcrumbItemsSourceView.GetAt(_currentIndex - 1);
            }
            else
            {
                throw new IndexOutOfRangeException();
            }
        }
    }

    private bool HasCurrent()
    {
        return _currentIndex < _size;
    }

    public int GetMany(Span<object> items)
    {
        int howMany = 0;
        if (HasCurrent())
        {
            do
            {
                if (howMany >= items.Length)
                    break;

                items[howMany] = Current;
                howMany++;
            } while (MoveNext());
        }

        return howMany;
    }

    public bool MoveNext()
    {
        if (HasCurrent())
        {
            ++_currentIndex;
            return HasCurrent();
        }
        else
        {
            throw new IndexOutOfRangeException();
        }
    }

    public void Reset()
    {
        _currentIndex = -1;
    }
}
