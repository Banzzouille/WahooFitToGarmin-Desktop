using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Windows.Controls;
using WahooFitToGarmin_Desktop.Helpers;
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
