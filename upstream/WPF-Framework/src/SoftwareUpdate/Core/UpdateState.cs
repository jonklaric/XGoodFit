namespace SoftwareUpdate
{
    /// <summary>
    /// Represents the current state of the update service.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b> Authors: </b>
    /// <list type="bullet">
    ///     <item> Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil </item>
    /// </list>
    /// </para>
    /// </remarks>
    public enum UpdateState
    {
        /// <summary>
        /// No update check has been performed yet.
        /// </summary>
        Idle,

        /// <summary>
        /// Currently checking for updates.
        /// </summary>
        Checking,

        /// <summary>
        /// An update is available for download.
        /// </summary>
        UpdateAvailable,

        /// <summary>
        /// Currently downloading an update.
        /// </summary>
        Downloading,

        /// <summary>
        /// Update has been downloaded and is ready to install.
        /// </summary>
        ReadyToInstall,

        /// <summary>
        /// Installing the update.
        /// </summary>
        Installing,

        /// <summary>
        /// An error occurred during the update process.
        /// </summary>
        Error,

        /// <summary>
        /// The application is up to date.
        /// </summary>
        UpToDate
    }
}
