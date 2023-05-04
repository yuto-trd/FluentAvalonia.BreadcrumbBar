# FluentAvalonia.BreadcrumbBar

[![Nuget](https://img.shields.io/nuget/vpre/FluentAvalonia.BreadcrumbBar?label=FluentAvalonia.BreadcrumbBar%20%28nuget%29)](https://www.nuget.org/packages/FluentAvalonia.BreadcrumbBar/)
[![Avalonia](https://img.shields.io/badge/Avalonia-11.0.999--cibuild0034035-blue)](https://github.com/AvaloniaUI/Avalonia)
![Runtime](https://img.shields.io/badge/-dotnet6-blue)

Control library which adds a WinUI BreadcrumbBar to the FluentAvalonia package.

[![Normal style](https://raw.githubusercontent.com/indigo-san/FluentAvalonia.BreadcrumbBar/main/Images/Normal.png)](https://github.com/indigo-san/FluentAvalonia.BreadcrumbBar/blob/main/SampleApp)

## Usage
Reference the Nuget package and include the Style in your application
```xml
<!-- in App.axaml -->
<!-- Define xmlns:sty="using:FluentAvalonia.Styling" -->

<App.Styles>
    <sty:FluentAvaloniaTheme />
    <StyleInclude Source="avares://FluentAvalonia.BreadcrumbBar/Styling/Styles.axaml" />
</App.Styles>
```

**[Large style](https://github.com/indigo-san/FluentAvalonia.BreadcrumbBar/blob/main/SampleApp/Views/MainWindow.axaml.cs#L29)**

![Large style](https://raw.githubusercontent.com/indigo-san/FluentAvalonia.BreadcrumbBar/main/Images/Large.png)
