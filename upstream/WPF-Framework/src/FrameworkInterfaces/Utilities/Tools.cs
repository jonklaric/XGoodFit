using System;

namespace FrameworkInterfaces.Utilities
{
    /// <summary>
    /// Provides utility tools for date/time conversion and formatting.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b> Authors: </b>
    /// <list type="bullet">
    ///     <item> Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil </item>
    /// </list>
    /// </para>
    /// </remarks>
    public static class Tools
    {
        /// <summary>
        /// Converts a date string to a DateTime object in local time.
        /// </summary>
        /// <param name="dateString">The date string to parse.</param>
        /// <returns>The parsed DateTime in local time, or null if parsing fails.</returns>
        public static DateTime? DateFromString(string dateString)
        {
            if (DateTime.TryParse(dateString, out DateTime result))
            {
                // Only convert to local time if the kind is Utc or Unspecified.
                // If it is already Local, calling ToLocalTime() again would double-shift the value.
                if (result.Kind == DateTimeKind.Local)
                    return result;
                return result.ToLocalTime();
            }
            return null;
        }

        /// <summary>
        /// Converts a DateTime object to a universal time string.
        /// </summary>
        /// <param name="dateTime">The DateTime to convert.</param>
        /// <returns>A string representation of the DateTime in universal time.</returns>
        public static string DateToUniversalString(DateTime dateTime)
        {
            return dateTime.ToUniversalTime().ToString();
        }

    }
}
