using Microsoft.Toolkit.Uwp.Notifications;

using WahooFitToGarmin_Desktop.Contracts.Services;

using Windows.UI.Notifications;

namespace WahooFitToGarmin_Desktop.Services
{
    public partial class ToastNotificationsService : IToastNotificationsService
    {
        public ToastNotificationsService()
        {
        }

        public void ShowToastNotification(ToastNotification toastNotification)
        {
            ToastNotificationManagerCompat.CreateToastNotifier().Show(toastNotification);
        }
    }
}
