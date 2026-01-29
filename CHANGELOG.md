# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased] - 2026-01-29

### Added

- **Git Commit Skill**: New developer skill for creating conventional commits with automatic type/scope analysis, intelligent staging, and standardized message generation following the Conventional Commits specification
- **Agent Skills Usage Policy**: Comprehensive guidelines in AGENTS.md documenting when and how to use available skills, with clear priority order to ensure consistent use of standardized workflows
- **Value Object Creator Skill**: New developer skill for creating domain value objects with validation, equality, and Result<T> pattern following DDD principles
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

### Fixed

- **Domain Events**: Corrected CustomerUpdatedDomainEvent registration to properly use Customer aggregate
- **Endpoint URLs**: Fixed customer creation endpoint URL in CustomerEndpoints
- **Test Stability**: Database readiness checks in EndpointTestFixture for reliable test execution
- **Code Formatting**: Added missing line breaks in Address, Customer, and CustomerNumber classes
- **Concurrency Handling**: Removed inappropriate ConcurrencyVersion updates from test models
- **OpenAPI Documentation**: Improved Swagger/OpenAPI documentation generation

### Removed

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

### Changed

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
