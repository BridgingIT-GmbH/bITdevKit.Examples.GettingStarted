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

To install the templates, clone this repository and run the following command from the repository root:

```bash
# Install the solution template
dotnet new install .
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

Unlike traditional architectures, in this template structure, the test projects are included within the same directory as their corresponding implementation projects:

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
└── YourSolution.sln
```

## Troubleshooting

If you encounter any issues with the templates:

1. Make sure you have the latest .NET SDK installed
2. Verify that the templates are installed correctly using `dotnet new list`
3. If projects are not added to the solution automatically, add them manually:
   ```bash
   dotnet sln YourSolution.sln add path/to/project.csproj
   ```

## Custom Modifications

You can customize these templates by modifying the template configuration files:
- Solution template: `.template.config/template.json` in the solution template directory
- Module template: `src/Modules/CoreModule/.template.config/template.json` in the module template directory

## License

This project is licensed under the MIT License - see the LICENSE file for details.