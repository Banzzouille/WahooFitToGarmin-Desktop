using System.Windows.Controls;

namespace WahooFitToGarmin_Desktop.Contracts.Views
{
    public interface IShellWindow
    {
        Frame GetNavigationFrame();

        void ShowWindow();

        void CloseWindow();
    }
}
