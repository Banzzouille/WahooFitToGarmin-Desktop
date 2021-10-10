using System.Windows.Controls;

using MahApps.Metro.Controls;

using WahooFitToGarmin_Desktop.Contracts.Views;
using WahooFitToGarmin_Desktop.ViewModels;

namespace WahooFitToGarmin_Desktop.Views
{
    public partial class ShellWindow : MetroWindow, IShellWindow
    {
        public ShellWindow(ShellViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        public Frame GetNavigationFrame()
            => shellFrame;

        public void ShowWindow()
            => Show();

        public void CloseWindow()
            => Close();
    }
}
