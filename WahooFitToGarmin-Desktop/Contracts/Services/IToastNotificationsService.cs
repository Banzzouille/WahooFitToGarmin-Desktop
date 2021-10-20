using Windows.UI.Notifications;

namespace WahooFitToGarmin_Desktop.Contracts.Services
{
    public interface IToastNotificationsService
    {
        public void ShowToastNotification(ToastNotification toastNotification);
        public void ShowSimpleToastNotification(string title, string body);

    }
}
