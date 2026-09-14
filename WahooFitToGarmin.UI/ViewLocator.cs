using Avalonia.Controls;
using Avalonia.Controls.Templates;

using WahooFitToGarmin.UI.ViewModels;

namespace WahooFitToGarmin.UI;

/// <summary>
/// Resolves a view from a view model by naming convention.
/// </summary>
/// <remarks>
/// Replaces the page service and the frame-based navigation service: a view
/// model named <c>MainViewModel</c> renders as <c>MainView</c>, with no registry
/// to keep in step.
/// </remarks>
public sealed class ViewLocator : IDataTemplate
{
    public Control Build(object? param)
    {
        if (param is null)
        {
            return new TextBlock { Text = "Nothing to show" };
        }

        var viewName = param.GetType().FullName!
            .Replace("ViewModels", "Views", StringComparison.Ordinal)
            .Replace("ViewModel", "View", StringComparison.Ordinal);

        var viewType = Type.GetType(viewName);

        return viewType is null
            ? new TextBlock { Text = $"No view for {viewName}" }
            : (Control)Activator.CreateInstance(viewType)!;
    }

    public bool Match(object? data) => data is ViewModelBase;
}
