using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reactive.Linq;

using Reactive.Bindings;

namespace SampleApp.ViewModels;
public class MainViewModel
{
    public MainViewModel()
    {
        Items = new()
        {
            "Home",
            "Documents",
            "Folder1",
        };

        Text.SetValidateAttribute(() => Text);

        Add = new(Text.ObserveHasErrors.Select(b => !b));
        Add.Do(x => Items.Add(x))
            .Subscribe(_ => Text.Value = "");
    }

    public ReactiveCollection<string> Items { get; }

    [Required]
    public ReactiveProperty<string> Text { get; } = new();

    public ReactiveCommand<string> Add { get; }
}
