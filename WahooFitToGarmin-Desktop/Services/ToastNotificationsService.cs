using Windows.Data.Xml.Dom;
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

        public void ShowToastNotification(string title, string body)
        {
            var content = new ToastContent()
            {
                Visual = new ToastVisual()
                {
                    BindingGeneric = new ToastBindingGeneric()
                    {
                        Children =
                        {
                            new AdaptiveText()
                            {
                                Text = title
                            },

                            new AdaptiveText()
                            {
                                Text = body
                            }
                        }
                    }
                }
            };
            var doc = new XmlDocument();
            doc.LoadXml(content.GetContent());
            var toast = new ToastNotification(doc)
            {
                // TODO WTS: Set a unique identifier for this notification within the notification group. (optional)
                // More details at https://docs.microsoft.com/uwp/api/windows.ui.notifications.toastnotification.tag
                Tag = "ToastTag"
            };
            ShowToastNotification(toast);
        }
    }
}
