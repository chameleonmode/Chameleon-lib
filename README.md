# Chameleon.lib

Chameleon.lib is a .NET library project that provides a set of tools and utilities for building flexible and modular applications. It consists of several components designed to work together seamlessly.

## Project Structure

The project is organized into the following main components:

1. **Chameleon.lib.Common**: Contains common utilities and base classes used across the project.
2. **Chameleon.lib.CommunityToolkit**: Includes additional tools and extensions built on top of the Common library.
3. **Chameleon.lib.Core**: Provides core functionality and services for the Chameleon library.
4. **Chameleon.lib.Playwright**: Implements Playwright-related features and automation scripts.
5. **Chameleon.lib.Tests**: Contains unit and integration tests for the Chameleon library.

## Key Features

- **Inversion of Control (IoC)**: The project uses dependency injection for managing object creation and lifetime.
- **Configuration Management**: Includes tools for reading and writing application configurations.
- **Automation Scripts**: Supports creation and execution of automation scripts, particularly using Playwright.
- **MVVM Support**: Includes base classes for implementing the Model-View-ViewModel pattern.

## Technologies and Frameworks

- .NET 8.0
- Microsoft.Extensions.Configuration
- Microsoft.Extensions.DependencyInjection
- Microsoft.Extensions.Logging
- Playwright (for web automation)

## Getting Started

1. Clone the repository
2. Open the solution file `Chameleon.lib.sln` in Visual Studio or your preferred IDE
3. Restore NuGet packages
4. Build the solution

## Usage

To use Chameleon.lib in your project, you can reference the required assemblies or install the NuGet packages (if published).

### Setting up IoC and Configuration

```csharp
using Chameleon.lib.Common;
using Chameleon.lib.Core;
using Chameleon.lib.Playwright.Interfaces;
using Chameleon.lib.Playwright.Services;
using Chameleon.lib.Core.Automation.Interfaces;
using Chameleon.lib.Core.Automation.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

// Configure IoC and services
IoC.Instance.Configure(() => {
    return new WritableConfiguration(new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddEnvironmentVariables()
        .Build(), Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json"));
}, (services) => {
    _ = services
        // Core services
        .AddSingleton<IAutomationScriptApi, AutomationScriptApi>()
        .AddSingleton<IAutomationScriptRepository, AutomationScriptRepository>()
        .AddSingleton<IAutomationService, AutomationService>()
        // Playwright services
        .AddSingleton<ICompileScriptService, CompileScriptService>()
        .AddSingleton<IPlaywriteBrowserService, PlaywriteBrowserService>()
        .AddSingleton<IPlaywrightScriptRepository, PlaywrightScriptRepository>()
        .AddSingleton<IChromeiumPlaywrightBrowser, ChromeiumPlaywrightBrowser>();
});

// Initialize IoC
IoC.Instance.Init((bool isDebug) => {
    // Additional setup code
});
```

### Using Playwright for Web Automation

Chameleon.lib includes integration with Playwright for web automation. Here's how you can use it:

1. Running a bundled script:

```csharp
var repo = IoC.GetService<IPlaywrightScriptRepository>();
var playBrowserService = IoC.GetService<IPlaywriteBrowserService>();

await playBrowserService!.RunScript(new PlaywriteRunScriptOptions {
    Port = port, // Specify the port for the browser
    BundledScript = repo!.BundledScripts[0], // Choose a bundled script
    Script = new AutomationScriptDescription {
        Parameters = [
            new AutomationParameterValue { Name = "keyword", Value = "example" },
            new AutomationParameterValue { Name = "targetUrl", Value = "example.com" },
            // Add other parameters as needed
        ]
    }
}, CancellationToken.None);
```

2. Running a script from a file:

```csharp
var playBrowserService = IoC.GetService<IPlaywriteBrowserService>();
await playBrowserService!.RunScript(new PlaywriteRunScriptOptions {
    Port = port, // Specify the port for the browser
    Script = new AutomationScriptDescription {
        FilePath = @"path\to\your\script.cs"
    }
}, CancellationToken.None);
```

Remember to dispose of the Playwright instance when you're done:

```csharp
playBrowserService.Playwright?.Dispose();
```

## Contributing

Contributions to Chameleon.lib are welcome. Please ensure that you write tests for new features and bug fixes.

## License

This project is licensed under the terms specified in the `LICENSE.txt` file.