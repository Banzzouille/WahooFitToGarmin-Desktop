using System;
using System.Windows.Controls;

namespace WahooFitToGarmin_Desktop.Contracts.Services
{
    public interface IPageService
    {
        Type GetPageType(string key);

        Page GetPage(string key);
    }
}
