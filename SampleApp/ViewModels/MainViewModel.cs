using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reactive.Linq;

using Reactive.Bindings;

namespace SampleApp.ViewModels;

public class ItemViewModel
{
    public ItemViewModel(string text)
    {
        Text = text;
    }

    public string Text { get; set; }
}

public class MainViewModel
{
    public MainViewModel()
    {
        Items = new()
        {
            new("Home"),
            new("Documents"),
            new("Folder1"),
        };

        Text.SetValidateAttribute(() => Text);

        Add = new(Text.ObserveHasErrors.Select(b => !b));
        Add.Do(x => Items.Add(new(x)))
            .Subscribe(_ => Text.Value = "");
    }

    public ReactiveCollection<ItemViewModel> Items { get; }

    [Required]
    public ReactiveProperty<string> Text { get; } = new();

    public ReactiveCommand<string> Add { get; }
}
