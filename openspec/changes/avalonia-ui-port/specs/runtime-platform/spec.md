## ADDED Requirements

### Requirement: Platform-neutral solution

No project in the solution SHALL declare an operating-system-specific target framework, and no project SHALL reference a Windows-only user interface or platform API. Platform differences SHALL be handled at runtime through platform-specific implementations of shared abstractions, not through platform-specific target frameworks.

#### Scenario: No project declares an operating-system target framework

- **WHEN** every project file in the solution is inspected
- **THEN** no target framework carries an operating-system suffix such as `-windows`

#### Scenario: No Windows-only user interface dependency remains

- **WHEN** the solution's package and assembly references are inspected
- **THEN** `PresentationFramework`, `System.Windows.Forms`, `MahApps.Metro`, `Hardcodet.NotifyIcon.Wpf`, `Microsoft.Toolkit.Uwp.Notifications`, and `Microsoft.Xaml.Behaviors.Wpf` are all absent

#### Scenario: No Windows Runtime projection is used

- **WHEN** the source is inspected
- **THEN** no file imports `Windows.UI.Notifications`, `Windows.Data.Xml.Dom`, or any other Windows Runtime projection

#### Scenario: Solution builds on a non-Windows host

- **WHEN** `dotnet build` is run on macOS
- **THEN** every project in the solution compiles

## REMOVED Requirements

### Requirement: Desktop project targets the Windows platform explicitly

**Reason**: The desktop project no longer uses WPF or Windows Runtime notification APIs, so the Windows-specific target framework moniker and the Windows SDK version floor it carried are obsolete. The requirement is superseded by "Platform-neutral solution", which forbids operating-system-specific target frameworks anywhere in the solution.

**Migration**: The desktop project is replaced by an Avalonia user interface project targeting `net10.0` with no operating-system suffix. `UseWPF`, `UseWindowsForms`, and the application manifest are removed. Windows Runtime notification calls are replaced by the cross-platform notifier defined in the `desktop-notifications` capability, and the Windows Forms folder dialog is replaced by the native folder picker defined in the `cross-platform-desktop-shell` capability.
