using System.Reflection;

using WahooFitToGarmin_Desktop.Contracts.Services;

namespace WahooFitToGarmin_Desktop.Services
{
    public class ApplicationInfoService : IApplicationInfoService
    {
        /// <summary>
        /// Reports the application version from assembly metadata.
        /// </summary>
        /// <remarks>
        /// This used to read the version out of the executing assembly's file on
        /// disk. <c>Assembly.Location</c> returns an empty string under
        /// single-file publishing, which the packaging change will use, so that
        /// approach would have failed there with an error pointing nowhere near
        /// the cause.
        /// </remarks>
        public Version GetVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();

            var informational = assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion;

            // Informational versions carry build metadata after a '+', which is
            // not part of a Version.
            if (!string.IsNullOrWhiteSpace(informational))
            {
                var trimmed = informational.Split('+')[0];
                if (Version.TryParse(trimmed, out var parsed))
                {
                    return parsed;
                }
            }

            return assembly.GetName().Version ?? new Version(0, 0, 0, 0);
        }
    }
}
