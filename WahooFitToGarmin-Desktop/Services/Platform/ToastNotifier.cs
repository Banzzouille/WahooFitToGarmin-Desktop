using Microsoft.Extensions.Logging;

using WahooFitToGarmin_Desktop.Contracts.Services;
using WahooFitToGarmin_Desktop.Core.Platform;

namespace WahooFitToGarmin_Desktop.Services.Platform
{
    /// <summary>
    /// Notifications through the existing Windows toast service.
    /// </summary>
    /// <remarks>
    /// Delivery failure is caught and logged rather than propagated: a platform
    /// that refuses to show a notification must not interrupt an upload.
    /// </remarks>
    public sealed class ToastNotifier : INotifier
    {
        private readonly IToastNotificationsService _toasts;
        private readonly ILogger<ToastNotifier> _logger;

        public ToastNotifier(IToastNotificationsService toasts, ILogger<ToastNotifier> logger)
        {
            _toasts = toasts;
            _logger = logger;
        }

        public void Notify(string title, string body)
        {
            try
            {
                _toasts.ShowSimpleToastNotification(title, body);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not raise a notification; processing continues");
            }
        }
    }
}
