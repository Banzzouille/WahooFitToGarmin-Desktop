## ADDED Requirements

### Requirement: Supported .NET runtime

The application SHALL target .NET 10 and SHALL NOT depend on any out-of-support .NET runtime. The user-facing prerequisite documented in the README SHALL state the .NET 10 Desktop Runtime.

#### Scenario: Solution builds on the .NET 10 SDK

- **WHEN** `dotnet build` is run against the solution with only the .NET 10 SDK installed
- **THEN** every project in the solution compiles without errors

#### Scenario: No project targets an unsupported framework

- **WHEN** the target framework of every project file is inspected
- **THEN** no project declares `netcoreapp3.1` or `netstandard2.0`

#### Scenario: Documented prerequisite matches the target

- **WHEN** the README installation section is read
- **THEN** it instructs the user to install the .NET 10 Desktop Runtime and does not reference .NET Core 3.1

### Requirement: Platform-neutral core library

The core library SHALL target a platform-neutral framework and SHALL NOT reference WPF, Windows Forms, WinRT, or any other Windows-only API surface. Windows-specific code SHALL reside exclusively in the desktop application project.

#### Scenario: Core compiles without a Windows target framework

- **WHEN** the core library is built
- **THEN** its target framework is `net10.0` with no OS-specific platform suffix

#### Scenario: Core carries no Windows-only dependency

- **WHEN** the core library's compile-time references are inspected
- **THEN** none resolve to `PresentationFramework`, `System.Windows.Forms`, or a Windows SDK projection assembly

#### Scenario: Core is consumable from a non-Windows project

- **WHEN** a `net10.0` test project without a Windows platform suffix references the core library
- **THEN** the referencing project compiles and its tests execute

### Requirement: Desktop project targets the Windows platform explicitly

The desktop application project SHALL declare a Windows platform target framework sufficient to compile WPF and the Windows Runtime notification APIs it consumes, and SHALL use the base .NET SDK rather than the obsolete Windows Desktop SDK.

#### Scenario: Desktop project uses the base SDK

- **WHEN** the desktop project file is inspected
- **THEN** its `Sdk` attribute is `Microsoft.NET.Sdk` and WPF is enabled through the `UseWPF` property

#### Scenario: Windows Runtime notification APIs resolve

- **WHEN** the desktop project is compiled
- **THEN** references to `Windows.UI.Notifications` and `Windows.Data.Xml.Dom` resolve without error

### Requirement: Uniform language settings

Every project in the solution SHALL enable nullable reference types, implicit usings, and the latest supported language version. These settings SHALL be declared once in a shared build properties file rather than repeated per project.

#### Scenario: Settings are declared centrally

- **WHEN** the repository root is inspected
- **THEN** a `Directory.Build.props` file declares `Nullable`, `ImplicitUsings`, and `LangVersion`

#### Scenario: No project overrides the shared language settings

- **WHEN** each project file is inspected
- **THEN** none redeclares `Nullable`, `ImplicitUsings`, or `LangVersion`

#### Scenario: Nullable warnings do not fail the build

- **WHEN** the solution is built while nullable warnings are present in the desktop project
- **THEN** the build succeeds, because warnings are not treated as errors

### Requirement: Centrally managed package versions

Package versions SHALL be declared in a single central location. Individual project files SHALL reference packages by name only, without a version attribute, so that a given package resolves to one version across the whole solution.

#### Scenario: Versions live in one file

- **WHEN** the repository root is inspected
- **THEN** a `Directory.Packages.props` file enables central package management and declares every package version

#### Scenario: Project files declare no versions

- **WHEN** each project file's `PackageReference` elements are inspected
- **THEN** none carries a `Version` attribute

#### Scenario: A package resolves to a single version

- **WHEN** a package is referenced by more than one project
- **THEN** all projects resolve it to the same version

### Requirement: Deprecated dependencies are replaced

The solution SHALL NOT depend on `Microsoft.Toolkit.Mvvm` or `Newtonsoft.Json`. MVVM primitives SHALL come from `CommunityToolkit.Mvvm`, and JSON serialization SHALL use `System.Text.Json`.

#### Scenario: No reference to the retired MVVM package

- **WHEN** the solution's package references are inspected
- **THEN** `Microsoft.Toolkit.Mvvm` is absent and `CommunityToolkit.Mvvm` is present

#### Scenario: No reference to Newtonsoft.Json

- **WHEN** the solution's package references are inspected
- **THEN** `Newtonsoft.Json` is absent

#### Scenario: View models still expose the same bindable surface

- **WHEN** the application runs after the MVVM package migration
- **THEN** property change notification and command binding behave as before, with no change to any XAML binding path

### Requirement: Settings written by a previous version remain readable

Replacing the JSON serializer SHALL NOT invalidate settings files written by earlier versions of the application. The settings reader SHALL accept both the base64-wrapped form and the plain JSON form.

#### Scenario: Base64-wrapped settings file is restored

- **WHEN** a settings file written by version 1.1.0 in base64-wrapped form is read
- **THEN** every stored key and value is restored with its original value

#### Scenario: Plain JSON settings file is restored

- **WHEN** a settings file whose content begins with `{` is read
- **THEN** it is parsed directly as JSON and every key and value is restored

#### Scenario: Round-trip preserves all stored keys

- **WHEN** the settings for the watched folder, Garmin login, Garmin password, keep-uploaded-file flag, and theme are saved and then restored
- **THEN** all five values are returned unchanged

#### Scenario: Non-generic dictionary content survives serialization

- **WHEN** settings held in a non-generic dictionary are persisted
- **THEN** the written file contains every entry, and restoring it yields the same entries rather than an empty or malformed document

### Requirement: Version reporting independent of assembly file path

The application SHALL determine its displayed version from assembly metadata rather than from the file path of the executing assembly, so that version reporting works under single-file deployment.

#### Scenario: Version is displayed on the settings page

- **WHEN** the settings page is opened
- **THEN** the application version is displayed and is non-empty

#### Scenario: Version resolution does not read the assembly location

- **WHEN** the version resolution code is inspected
- **THEN** it does not call `Assembly.Location` or `FileVersionInfo.GetVersionInfo`

### Requirement: Automated test project

The solution SHALL contain an automated test project that targets a platform-neutral framework, references the core library, and runs without a Windows-only dependency.

#### Scenario: Tests run from the command line

- **WHEN** `dotnet test` is run against the solution
- **THEN** the test project is discovered and its tests execute

#### Scenario: Test project is platform-neutral

- **WHEN** the test project file is inspected
- **THEN** its target framework is `net10.0` with no OS-specific platform suffix, and it does not reference the desktop application project

### Requirement: No change to observable application behaviour

This platform migration SHALL NOT alter any user-facing behaviour. The folder watcher, the keep-uploaded-file option, toast notifications, theme selection, navigation, settings persistence, and the Garmin upload path SHALL behave exactly as they did before.

#### Scenario: Watched folder still triggers an upload

- **WHEN** a `.fit` file appears in the configured watched folder
- **THEN** the application logs the arrival, raises a notification, and attempts the upload exactly as before

#### Scenario: Keep-uploaded-file option is honoured

- **WHEN** an activity uploads successfully and the keep-uploaded-file option is disabled
- **THEN** the source file is deleted; and when the option is enabled, the file is retained

#### Scenario: Application still runs as a WPF desktop application

- **WHEN** the migrated solution is launched on Windows
- **THEN** the shell window, navigation menu, main page, and settings page render and operate as before
