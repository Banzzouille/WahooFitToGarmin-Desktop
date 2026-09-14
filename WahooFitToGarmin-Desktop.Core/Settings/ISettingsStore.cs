namespace WahooFitToGarmin_Desktop.Core.Settings
{
    /// <summary>
    /// The single source of truth for user settings.
    /// </summary>
    /// <remarks>
    /// Settings are persisted as soon as they change rather than at shutdown, so
    /// a value survives an abrupt termination. Components that care about a
    /// setting subscribe to <see cref="Changed"/> instead of reading once at
    /// construction — which is why the application no longer needs restarting
    /// for a setting to take effect.
    /// </remarks>
    public interface ISettingsStore
    {
        UserSettings Current { get; }

        /// <summary>
        /// Raised after <see cref="Current"/> has been replaced.
        /// </summary>
        event EventHandler<UserSettings>? Changed;

        /// <summary>
        /// Applies a change and persists it.
        /// </summary>
        void Update(Func<UserSettings, UserSettings> change);
    }
}
