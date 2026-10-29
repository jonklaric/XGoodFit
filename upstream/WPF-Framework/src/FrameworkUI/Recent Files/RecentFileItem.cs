using System.Windows.Controls;

namespace FrameworkUI
{
    /// <summary>
    /// A class for recent file items.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Authors:
    ///     Woody Fields, USACE Risk Management Center, woodrow.l.fields@usace.army.mil
    ///     Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil
    /// </para>
    /// </remarks>
    public class RecentFileItem
    {

        /// <summary>
        /// Gets the file name without extension.
        /// </summary>
        public string Name
        {
            get { return System.IO.Path.GetFileNameWithoutExtension(FilePath); }
        }

        /// <summary>
        /// Gets the shortened display location of the file.
        /// </summary>
        public string DisplayLocation
        {
            get { return UtilityFunctions.ShortenPathname(System.IO.Path.GetDirectoryName(FilePath) ?? string.Empty, 50); }
        }

        /// <summary>
        /// Gets or sets the recent file path.
        /// </summary>
        public string FilePath { get; set; } = "";

        /// <summary>
        /// Gets or sets the file menu item.
        /// </summary>
        public MenuItem? MenuItem { get; set; }

        /// <summary>
        /// Gets the recent file display path.
        /// </summary>
        public string DisplayPath
        {
            get { return System.IO.Path.Combine(System.IO.Path.GetDirectoryName(FilePath) ?? string.Empty, System.IO.Path.GetFileNameWithoutExtension(FilePath)); }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RecentFileItem"/> class.
        /// </summary>
        /// <param name="filePath">The full file path.</param>
        public RecentFileItem(string filePath)
        {
            FilePath = filePath;
        }

    }
}
