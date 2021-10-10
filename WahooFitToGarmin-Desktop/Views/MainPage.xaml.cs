using System.Windows.Controls;

using WahooFitToGarmin_Desktop.ViewModels;

namespace WahooFitToGarmin_Desktop.Views
{
    public partial class MainPage : Page
    {
        public MainPage(MainViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
