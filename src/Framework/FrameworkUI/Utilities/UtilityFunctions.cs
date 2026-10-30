using System.IO;
using System.Text;


namespace FrameworkUI
{
    /// <summary>
    /// Utility functions for common operations in the framework.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b> Authors: </b>
    /// <list type="bullet">
    ///     <item> Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil </item>
    /// </list>
    /// </para>
    /// </remarks>
    public static class UtilityFunctions
    {

        /// <summary>
        /// Converts a UTF-8 string to a byte array.
        /// </summary>
        /// <param name="str">The string to convert.</param>
        /// <returns>A byte array representing the UTF-8 encoded string.</returns>
        public static byte[] UTF8StringToBytes(string str)
        {
            return Encoding.UTF8.GetBytes(str);
        }

        /// <summary>
        /// Converts a UTF-8 byte array to a string.
        /// </summary>
        /// <param name="bytes">The byte array to convert.</param>
        /// <returns>A string decoded from the UTF-8 byte array.</returns>
        public static string UTF8BytesToString(byte[] bytes)
        {
            return Encoding.UTF8.GetString(bytes);
        }

        /// <summary>
        /// Function used to convert an integer to a color.
        /// </summary>
        /// <param name="ARGB">Alpha, red, green, blue as integer.</param>
        /// <returns>A Color object created from the ARGB integer value.</returns>
        public static Avalonia.Media.Color IntegerToColor(ref int ARGB)
        {
            var Bytes = BitConverter.GetBytes(ARGB);
            byte Alpha = Bytes[3];
            byte Red = Bytes[2];
            byte Green = Bytes[1];
            byte Blue = Bytes[0];
            return Avalonia.Media.Color.FromArgb(Alpha, Red, Green, Blue);
        }

        /// <summary>
        /// Function used to convert a color to an integer.
        /// </summary>
        /// <param name="color">The color to convert.</param>
        /// <returns>An ARGB integer value representing the color.</returns>
        public static int ColorToInteger(Avalonia.Media.Color color)
        {
            return (color.A << 24) | (color.R << 16) | (color.G << 8) | color.B;
        }

        /// <summary>
        /// Gets the free space available on the drive.
        /// </summary>
        /// <param name="driveName">Drive name.</param>
        /// <returns>The available free space on the drive in bytes.</returns>
        public static long GetAvailableDriveSpace(string driveName)
        {
            var allDrives = DriveInfo.GetDrives();
            var freeSpace = default(long);
            foreach (var dInfo in allDrives)
            {
                if (dInfo.Name == driveName)
                {
                    freeSpace = dInfo.AvailableFreeSpace;
                    break;
                }
            }
            return freeSpace;
        }

        /// <summary>
        /// Shortens a pathname for display purposes.
        /// </summary>
        /// <param name="pathname">The pathname to shorten.</param>
        /// <param name="maxLength">The maximum number of characters to be displayed.</param>
        /// <remarks>
        /// Shortens a pathname by either removing consecutive components of a path
        /// and/or by removing characters from the end of the filename and replacing
        /// then with three ellipses (...)
        /// <para>
        /// In all cases, the root of the passed path will be preserved in it's entirety.
        /// </para>
        /// <para>
        /// If a UNC path is used or the pathname and maxLength are particularly short,
        /// the resulting path may be longer than maxLength.
        /// </para>
        /// <para>
        /// This method expects fully resolved pathnames to be passed to it.
        /// (Use Path.GetFullPath() to obtain this.)
        /// </para>
        /// <para>
        /// This method is taken from Joe Woodbury's article at: http://www.codeproject.com/KB/cs/mrutoolstripmenu.aspx
        /// </para>
        /// </remarks>
        /// <returns>A shortened pathname string.</returns>
        public static string ShortenPathname(string pathname, int maxLength)
        {
            if (pathname.Length <= maxLength)
            {
                return pathname;
            }

            string? root = Path.GetPathRoot(pathname);
            if (root == null || root.Length == 0) return pathname;
            if (root.Length > 3)
            {
                root += Path.DirectorySeparatorChar.ToString();
            }

            var elements = pathname.Substring(root.Length).Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            int filenameIndex = elements.GetLength(0) - 1;
            if (elements.GetLength(0) == 1)
            {
                // pathname is just a root and filename
                if (elements[0].Length > 5)
                {
                    // long enough to shorten
                    // if path is a UNC path, root may be rather long
                    if (root.Length + 6 >= maxLength)
                    {
                        return root + elements[0].Substring(0, 3) + "...";
                    }
                    else
                    {
                        return pathname.Substring(0, maxLength - 3) + "...";
                    }
                }
            }
            else if (root.Length + 4 + elements[filenameIndex].Length > maxLength)
            {
                // pathname is just a root and filename
                root += @"...\";
                int len = elements[filenameIndex].Length;
                if (len < 6)
                {
                    return root + elements[filenameIndex];
                }

                if (root.Length + 6 >= maxLength)
                {
                    len = 3;
                }
                else
                {
                    len = maxLength - root.Length - 3;
                }

                return root + elements[filenameIndex].Substring(0, len) + "...";
            }
            else if (elements.GetLength(0) == 2)
            {
                return root + @"...\" + elements[1];
            }
            else
            {
                int len = 0;
                int begin = 0;
                for (int i = 0; i < filenameIndex; i++)
                {
                    if (elements[i].Length > len)
                    {
                        begin = i;
                        len = elements[i].Length;
                    }
                }

                int totalLength = pathname.Length - len + 3;
                int end = begin + 1;
                while (totalLength > maxLength)
                {
                    if (begin > 0)
                    {
                        totalLength -= elements[System.Threading.Interlocked.Decrement(ref begin)].Length - 1;
                    }

                    if (totalLength <= maxLength)
                    {
                        break;
                    }

                    if (end < filenameIndex)
                    {
                        totalLength -= elements[System.Threading.Interlocked.Increment(ref end)].Length - 1;
                    }

                    if (begin == 0 && end == filenameIndex)
                    {
                        break;
                    }
                }

                // assemble final string

                for (int i = 0; i < begin; i++)
                    root += elements[i] + '\\';
                root += @"...\";
                for (int i = end; i < filenameIndex; i++)
                    root += elements[i] + '\\';
                return root + elements[filenameIndex];
            }

            return pathname;
        }

        // XGoodFit port: upstream's MenuItem Clone, CopyClick and GetRoutedEventHandlers aren't ported.
        // Nothing uses them, and they copied event handlers out of WPF's internal handler store.
    }
}
