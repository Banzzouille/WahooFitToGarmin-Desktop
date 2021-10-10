using System;
using System.Diagnostics;
using System.Reflection;

using WahooFitToGarmin_Desktop.Contracts.Services;

namespace WahooFitToGarmin_Desktop.Services
{
    public class ApplicationInfoService : IApplicationInfoService
    {
        public ApplicationInfoService()
        {
        }

        public Version GetVersion()
        {
            // Set the app version in WahooFitToGarmin-Desktop > Properties > Package > PackageVersion
            string assemblyLocation = Assembly.GetExecutingAssembly().Location;
            var version = FileVersionInfo.GetVersionInfo(assemblyLocation).FileVersion;
            return new Version(version);
        }
    }
}
