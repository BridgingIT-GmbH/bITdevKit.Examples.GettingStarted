# BridgingIT DevKit Templates

This repository contains custom .NET templates based on BridgingIT DevKit (bIT DevKit) that follows the Onion Architecture principles. These templates help you quickly scaffold new solutions and modules following this architecture approach.

## Templates Overview

### 1. DevKit Solution Template (`devkitsolution`)

The solution template creates a complete solution structure with an initial module. It sets up the foundation for your application following Onion Architecture principles.

### 2. DevKit Module Template (`devkitmodule`)

The module template helps you add new functional modules to your solution. Each module is structured following the Onion Architecture pattern with separate projects for different concerns.

Each module contains the following projects:
- `[ModuleName].Application.csproj`
- `[ModuleName].Domain.csproj`
- `[ModuleName].Infrastructure.csproj`
- `[ModuleName].IntegrationTests.csproj`
- `[ModuleName].Presentation.csproj`
- `[ModuleName].UnitTests.csproj`

## Installation

### Prerequisites

- .NET 9 SDK or later

### Install the Templates

To install the templates from NuGet.org:

```bash
# Install the BridgingIT DevKit Templates package
dotnet new install BridgingIT.DevKit.Templates
```

### Verify Installation

After installation, verify the templates are available:

```bash
dotnet new list | grep -E "(devkitsolution|devkitmodule)"
```

You should see:
```
BridgingIT DevKit Solution  devkitsolution  [C#]     Solution
BridgingIT DevKit Module    devkitmodule    [C#]     Module
```

### Update Templates

To update to the latest version:

```bash
# Uninstall current version
dotnet new uninstall BridgingIT.DevKit.Templates

# Install latest version
dotnet new install BridgingIT.DevKit.Templates
```

## Creating a New Solution

To create a new solution with an initial module:

```bash
dotnet new devkitsolution --SolutionName YourCompany.YourProduct --ModuleName Core -o YourProductDirectory
```

Parameters:
- `--SolutionName`: The name of your solution (default: DevKit.Examples.GettingStarted)
- `--ModuleName`: The name of the initial module (default: Core)
- `-o`: Output directory for the solution

Example:
```bash
dotnet new devkitsolution --SolutionName MeineFirma.MeinProjekt.WebApp --ModuleName Core -o MeinProjekt
```

## Adding a New Module

To add a new module to an existing solution:

```bash
dotnet new devkitmodule -n ModuleName -o src/Modules/ModuleName
```

Parameters:
- `-n` or `--ModuleName`: The name of the new module
- `-o`: Output directory for the module

Example:
```bash
dotnet new devkitmodule -n Orders -o src/Modules/Orders
```

After adding a new module, the template will automatically add the generated projects to your solution file.

## Project Structure

The template creates a solution following Onion Architecture principles with the test projects included within the same directory as their corresponding implementation projects:

```
YourSolution/
├── src/
│   ├── Modules/
│   │   ├── Core/
│   │   │   ├── Core.Application.csproj
│   │   │   ├── Core.Domain.csproj
│   │   │   ├── Core.Infrastructure.csproj
│   │   │   ├── Core.IntegrationTests.csproj
│   │   │   ├── Core.Presentation.csproj
│   │   │   └── Core.UnitTests.csproj
│   │   ├── Orders/
│   │   │   ├── Orders.Application.csproj
│   │   │   ├── Orders.Domain.csproj
│   │   │   ├── Orders.Infrastructure.csproj
│   │   │   ├── Orders.IntegrationTests.csproj
│   │   │   ├── Orders.Presentation.csproj
│   │   │   └── Orders.UnitTests.csproj
│   └── Presentation.Web.Server/
│       └── Presentation.Web.Server.csproj
└── YourSolution.sln
```

## Available Templates

| Template Name | Short Name | Description |
|---------------|------------|-------------|
| BridgingIT DevKit Solution | `devkitsolution` | Creates a complete solution with initial module following Onion Architecture |
| BridgingIT DevKit Module | `devkitmodule` | Adds a new module to an existing solution |

## Template Parameters

### DevKit Solution Template

| Parameter | Description | Default Value |
|-----------|-------------|---------------|
| `--SolutionName` | Name of the solution | `DevKit.Examples.GettingStarted` |
| `--ModuleName` | Name of the initial module | `Core` |

### DevKit Module Template

| Parameter | Description | Default Value |
|-----------|-------------|---------------|
| `-n, --ModuleName` | Name of the new module | `NewModule` |

## Troubleshooting

If you encounter any issues with the templates:

### Template Installation Issues

1. **Templates not found after installation:**
   ```bash
   # Check if templates are installed
   dotnet new list

   # If not found, try reinstalling
   dotnet new install BridgingIT.DevKit.Templates --force
   ```

2. **Permission errors during installation:**
   ```bash
   # On Linux/Mac, you might need sudo for global installation
   sudo dotnet new install BridgingIT.DevKit.Templates
   ```

3. **Outdated templates:**
   ```bash
   # Uninstall and reinstall to get latest version
   dotnet new uninstall BridgingIT.DevKit.Templates
   dotnet new install BridgingIT.DevKit.Templates
   ```

### Project Creation Issues

1. **Missing .NET SDK:**
   - Make sure you have .NET 9 SDK or later installed
   - Verify with: `dotnet --version`

2. **Projects not added to solution automatically:**
   ```bash
   # Add projects manually to solution
   dotnet sln YourSolution.sln add src/Modules/ModuleName/*.csproj
   ```

3. **Template parameters not working:**
   ```bash
   # Check available parameters for a template
   dotnet new devkitsolution --help
   dotnet new devkitmodule --help
   ```

### NuGet Package Issues

1. **Package not found:**
   - Ensure you have internet connection
   - Check NuGet.org is accessible
   - Try clearing NuGet cache: `dotnet nuget locals all --clear`

2. **Version conflicts:**
   ```bash
   # Install specific version if needed
   dotnet new install BridgingIT.DevKit.Templates::1.0.1-preview
   ```

## Uninstalling Templates

To uninstall the templates:

```bash
dotnet new uninstall BridgingIT.DevKit.Templates
```

## Development and Local Testing

For developers who want to test templates locally from source:

```bash
# Clone the repository
git clone https://github.com/bridgingIT/bIT.devkit-examples-gettingstarted.git
cd bIT.devkit-examples-gettingstarted

# Install templates from local source
dotnet new install .

# Test template creation
dotnet new devkitsolution --SolutionName TestSolution --ModuleName TestCore -o TestOutput
```

## Custom Modifications

You can customize these templates by modifying the template configuration files:
- Solution template: `.template.config/template.json` in the solution template directory
- Module template: `src/Modules/CoreModule/.template.config/template.json` in the module template directory

For more information about .NET template development, see the [official documentation](https://docs.microsoft.com/en-us/dotnet/core/tools/custom-templates).

## Support and Issues

- **GitHub Issues**: [Report issues or request features](https://github.com/BridgingIT-GmbH/bITdevKit/issues)
- **NuGet Package**: [BridgingIT.DevKit.Templates on NuGet.org](https://www.nuget.org/packages/BridgingIT.DevKit.Templates)
- **Documentation**: [BridgingIT DevKit Documentation](https://github.com/BridgingIT-GmbH/bITdevKit)
~~~~
## License

This project is licensed under the MIT License - see the LICENSE file for details.
