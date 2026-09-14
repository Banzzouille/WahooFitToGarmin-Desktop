using System.Windows;

using WahooFitToGarmin_Desktop.Core.Platform;

namespace WahooFitToGarmin_Desktop.Services.Platform
{
    /// <summary>Dispatches onto the WPF user interface thread.</summary>
    public sealed class WpfUiDispatcher : IUiDispatcher
    {
        public bool IsOnUiThread => Application.Current?.Dispatcher.CheckAccess() ?? true;

        public void Post(Action action)
        {
            var dispatcher = Application.Current?.Dispatcher;

            if (dispatcher is null || dispatcher.CheckAccess())
            {
                action();
                return;
            }

            dispatcher.BeginInvoke(action);
        }
    }
}
