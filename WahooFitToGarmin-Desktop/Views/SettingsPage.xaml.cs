using System.Windows.Controls;

using WahooFitToGarmin_Desktop.ViewModels;

namespace WahooFitToGarmin_Desktop.Views
{
    public partial class SettingsPage : Page
    {
        public SettingsPage(SettingsViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
