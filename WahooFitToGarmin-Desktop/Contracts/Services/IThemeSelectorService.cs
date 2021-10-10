using System;

using WahooFitToGarmin_Desktop.Models;

namespace WahooFitToGarmin_Desktop.Contracts.Services
{
    public interface IThemeSelectorService
    {
        void InitializeTheme();

        void SetTheme(AppTheme theme);

        AppTheme GetCurrentTheme();
    }
}
