# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased] - 2026-02-20

### Added

- **BDK CLI Launchers**: Added dedicated launcher scripts for both PowerShell and Bash to simplify command execution in different environments
- **Agents Directive**: Added a directive to guide AI agent usage in the repository
- **Value Object Templates**: New templates and examples for domain value objects, including unit tests
- **BDK CLI Installation Script**: PowerShell installer for streamlined BDK CLI setup
- **Skill Discovery**: New find-skills skill and expanded skills list for faster capability discovery
- **dotnet-inspect Tooling**: New dotnet-inspect configuration and documentation
- **System Design Diagrams**: Comprehensive architecture and interaction diagrams in documentation
- **BdkUI Banner**: ASCII art banner for improved CLI visual identity
- **Testing Guides**: Unit and integration test authoring guides with templates and checklists
- **Docker Tasking**: Docker CLI integration with build/run/compose tasks, cleanup, and log management
- **OpenAPI Utilities**: Linting, client generation, and HTTP request file generation utilities
- **EF Core Tasking**: DbContext discovery with module-specific script and bundle tasks
- **BDK TUI**: Initial terminal UI support for BDK workflows
- **Git Commit Skill**: New developer skill for creating conventional commits with automatic type/scope analysis, intelligent staging, and standardized message generation following the Conventional Commits specification
- **Agent Skills Usage Policy**: Comprehensive guidelines in AGENTS.md documenting when and how to use available skills, with clear priority order to ensure consistent use of standardized workflows
- **Value Object Creator Skill**: New developer skill for creating domain value objects with validation, equality, and Result of T pattern following DDD principles
- **Document Co-authoring Skill**: Interactive workflow skill to guide collaborative documentation creation through structured refinement and verification
- **Development Container Support**: Added devcontainer configuration for consistent development environment setup
- **Address Management**: Full support for managing customer addresses including:
  - Add, update, and remove addresses with validation
  - Primary address designation with automatic single-primary enforcement
  - Duplicate address prevention
  - Localized validation messages
- **Enhanced Domain Validation**:
  - DateOfBirth validation with business rules in Customer aggregate
  - Duplicate address checks in AddAddress and UpdateAddress methods
  - EmailAddress value object with improved type safety
- **Comprehensive Code Review Guidelines**: Added detailed architecture and DDD pattern review checklists for maintaining code quality
- **Architecture Decision Records (ADRs)**: Complete set of ADRs documenting architectural decisions including:
  - Clean/Onion Architecture with strict layer boundaries
  - Result pattern for error handling
  - Repository pattern with decorator behaviors
  - CQRS with Requester/Notifier pattern
  - And 15+ additional architectural decisions
- **Enhanced Documentation**:
  - AGENTS.md with comprehensive guidance for AI-assisted development
  - Detailed CoreModule README
  - ADR quick reference guide
  - bITdevKit pattern documentation
- **Improved Testing**:
  - Testcontainer SQL integration tests
  - Enhanced API integration tests with proper logging
  - Architecture tests for Clean Architecture boundary enforcement
  - Additional validation scenarios for CustomerNumber and EmailAddress
- **Modern Web Interface**:
  - Bootstrap 5 upgrade with theme switcher (light/dark mode)
  - README endpoint for local development
  - Default file serving for better developer experience
  - Improved navigation and accessibility

### Changed

- **SDK Version**: Updated `global.json` to .NET SDK `10.0.103`
- **Project Guidance**: Expanded `AGENTS.md` with deeper architecture, testing, and workflow guidance for contributors
- **EF Core Documentation**: Expanded guidance with detailed commands, best practices, and clearer formatting
- **Agents Documentation**: Removed outdated skills section and updated file structure
- **BDK CLI Refactor**: Modularized CLI scripts, consolidated publish tasks, and improved RID selection
- **Documentation Digest**: Task updates to digest source code for LLM processing
- **OpenAPI Linting**: Ruleset inclusion and improved license report paths
- **Docker Output**: Enabled command output display and refined user messaging for Docker operations
- **Task Registry Cleanup**: Removed alias tasks for a cleaner structure
- **Documentation**: Added layer location references in README and updated ADR test command/category
- **Type Safety Improvements**: Customer now uses EmailAddress value object directly instead of strings for email validation
- **Customer Status**: Refactored from integer to string type for better readability and maintainability
- **Private Constructors**: Customer aggregate now enforces creation through factory methods only
- **Address Model**: Enhanced with localized validation and improved business rule enforcement
- **Single Primary Address**: Refactored address update logic to enforce single primary address constraint
- **Validation Messages**: Improved clarity and consistency across domain models
- **Package Updates**: Updated to BridgingIT.DevKit 10.0.2 and .NET 10 SDK
- **Docker Configuration**: Upgraded .NET SDK and ASP.NET runtime to version 10.0
- **Test Coverage**: Improved overall test coverage with enhanced reporting using Coverlet
- **Mapping Configuration**: Refactored Mapster configuration and logging order for better performance
- **Command Classes**: Improved clarity and consistency in command summaries and validation messages
- **Tooling Versions**: Updated dotnet tool versions in dotnet-tools.json
- **OpenAPI Utilities**: Improved OpenApiUtils logging and error handling
- **Dependencies and CLI**: Updated dependencies and enhanced CLI functionality

### Fixed

- **Documentation Formatting**: Removed an unnecessary fenced code block from `CODE_OF_CONDUCT.md`
- **Skill Metadata**: Corrected an invalid character in skill naming metadata (`SKILL.md`)
- **Workspace Formatting**: Corrected indentation for agent skills locations
- **Diagnostics Messaging**: Improved CPU/GC/ASP.NET trace messages and benchmark selection clarity
- **Task Discovery**: Enabled recursive project search for diagnostics and utility tasks
- **OpenAPI DTO Docs**: Corrected line breaks in customer DTO descriptions
- **Docker Defaults**: Updated default network name format
- **Bdk TUI**: Fixed Bun path handling and permissions
- **Domain Events**: Corrected CustomerUpdatedDomainEvent registration to properly use Customer aggregate
- **Endpoint URLs**: Fixed customer creation endpoint URL in CustomerEndpoints
- **Test Stability**: Database readiness checks in EndpointTestFixture for reliable test execution
- **Code Formatting**: Added missing line breaks in Address, Customer, and CustomerNumber classes
- **Concurrency Handling**: Removed inappropriate ConcurrencyVersion updates from test models
- **OpenAPI Documentation**: Improved Swagger/OpenAPI documentation generation

### Removed

- **Skill Documentation**: Removed obsolete `find-skills` documentation
- **Legacy TUI**: Removed old opentui implementation
- **Obsolete Docs**: Removed outdated BDK CLI README
- **Obsolete Code**:
  - Removed outdated Quartz migration files
  - Removed Process_UpdateEmailAddress_SuccessResult test
  - Cleaned up redundant content from AGENTS.md
- **Deprecated Patterns**: Removed direct CustomerStatus namespace references in favor of simplified usage

### Developer Experience

- **Better Tooling**: Enhanced pipeline with improved error handling and detailed logging
- **Skill System**: Multiple new skills for common development tasks (value objects, ADR writing, code reviews)
- **Architecture Guidance**: Comprehensive documentation for maintaining Clean Architecture and DDD patterns
- **Test Infrastructure**: Improved integration testing with Testcontainers for SQL Server

---

## [10.0.1] - 2025-11-18

### Changed (10.0.1)

- **Package Updates**: Updated all BridgingIT.DevKit packages to support .NET 10
- **Build Pipeline**: Updated Azure Pipelines configuration for .NET 10 compatibility
- **Development Tools**: Updated dotnet-tools.json with latest tool versions
- **SDK Version**: Updated global.json to .NET 10 SDK
- **Project Files**: Updated all project files (.csproj) to target .NET 10
- **Dependencies**: Updated Directory.Packages.props with latest package versions for .NET 10 compatibility

### Technical Details

This release focuses on migrating the entire solution to .NET 10, ensuring all components, tools, and dependencies are compatible with the latest .NET platform.

---

## [10.0.0] - 2025-11-12

Initial .NET 10 release. See git history for detailed changes from previous versions.

---

[Unreleased]: https://github.com/BridgingIT-GmbH/bITdevKit.Examples.GettingStarted/compare/10.0.1...HEAD
[10.0.1]: https://github.com/BridgingIT-GmbH/bITdevKit.Examples.GettingStarted/compare/10.0.0...10.0.1
[10.0.0]: https://github.com/BridgingIT-GmbH/bITdevKit.Examples.GettingStarted/releases/tag/10.0.0
