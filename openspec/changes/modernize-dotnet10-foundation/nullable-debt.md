# Nullable warning debt in the desktop project

Recorded for task 11.11. Nullable reference types are enabled across the whole
solution, and the warnings they produce are not treated as errors — see
`design.md`, D8.

**Core is clean: zero warnings.** It is annotated properly because it survives
every downstream change.

**The desktop project carries 104 warnings**, listed below by file. They are not
fixed here, deliberately: almost every file in this list is rewritten or deleted
by a later change, and annotating code that is about to disappear is effort
spent twice.

| File | Warnings | Fate |
|---|---:|---|
| `ViewModels/SettingsViewModel.cs` | 24 | Rewritten by `extract-platform-agnostic-core`, then again by `avalonia-ui-port` |
| `ViewModels/ShellViewModel.cs` | 22 | Rewritten by `avalonia-ui-port` |
| `Services/NavigationService.cs` | 12 | Replaced by `avalonia-ui-port` |
| `App.xaml.cs` | 12 | Rewritten by `avalonia-ui-port` as the Avalonia composition root |
| `Services/ApplicationHostService.cs` | 8 | Rewritten by `avalonia-ui-port` |
| `Services/PageService.cs` | 6 | Deleted by `avalonia-ui-port` |
| `TemplateSelectors/MenuItemTemplateSelector.cs` | 4 | Deleted by `avalonia-ui-port` |
| `Helpers/PropertyChangedBase.cs` | 4 | Deleted by `avalonia-ui-port` |
| `Converters/EnumToBooleanConverter.cs` | 4 | Deleted by `avalonia-ui-port` |
| `ViewModels/MainViewModel.cs` | 2 | Reduced by `extract-platform-agnostic-core` |
| `Services/ThemeSelectorService.cs` | 2 | Replaced by `avalonia-ui-port` |
| `Helpers/FrameExtensions.cs` | 2 | Deleted by `avalonia-ui-port` |
| `Contracts/Services/INavigationService.cs` | 2 | Replaced by `avalonia-ui-port` |

Every file in this table is touched by a change that is already specified. The
expectation is that this count reaches zero as a side effect of that work rather
than through a dedicated cleanup pass.

If a file here is still generating warnings once its owning change has shipped,
that is worth treating as a defect in that change rather than as leftover debt
from this one.
