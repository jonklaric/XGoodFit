using DatabaseManager;
using System;
using System.Collections.Generic;

namespace FrameworkUI.Demo
{
    /// <summary>
    /// Provides safe SQLite load-entry helpers for element collections.
    /// </summary>
    /// <remarks>
    /// <para>
    ///     Authors:
    ///     Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil
    /// </para>
    /// <para>
    /// Collection tables can become stale independently from the elements they index. These
    /// helpers implement a consistent first-row-wins repair policy while preserving true
    /// distinct element-name conflicts for the normal validation layer.
    /// </para>
    /// </remarks>
    internal static class CollectionPersistenceHelper
    {
        /// <summary>
        /// Builds load entries from a single-table collection using first duplicate name wins.
        /// </summary>
        /// <param name="dtView">The collection table view.</param>
        /// <param name="needsRewrite">Receives <c>true</c> when blank or duplicate rows were skipped.</param>
        /// <returns>The ordered loadable element names.</returns>
        public static List<string> BuildSingleTableLoadEntries(DataTableView dtView, out bool needsRewrite)
        {
            needsRewrite = false;
            var entries = new List<string>();
            var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < dtView.NumberOfRows; i++)
            {
                string elementName = ReadName(dtView, i);
                if (string.IsNullOrWhiteSpace(elementName))
                {
                    needsRewrite = true;
                    continue;
                }

                if (seenNames.Add(elementName) == false)
                {
                    needsRewrite = true;
                    continue;
                }

                entries.Add(elementName);
            }

            return entries;
        }

        /// <summary>
        /// Determines whether a table contains a row with the specified element name.
        /// </summary>
        /// <param name="sqlite">The SQLite project connection.</param>
        /// <param name="tableName">The table name to search.</param>
        /// <param name="elementName">The element name to find.</param>
        /// <returns><c>true</c> when a matching row exists; otherwise, <c>false</c>.</returns>
        public static bool NamedRowExists(SQLiteManager sqlite, string tableName, string elementName)
        {
            if (string.IsNullOrWhiteSpace(elementName) ||
                sqlite.TableNames.Contains(tableName) == false)
            {
                return false;
            }

            DataTableView dtView = sqlite.GetTableManager(tableName);
            if (dtView.ColumnNames.Contains("Name") == false)
            {
                return false;
            }

            return dtView.SearchColumn(0, dtView.NumberOfRows - 1, "Name", elementName, true, true) != -1;
        }

        /// <summary>
        /// Reads an element name from a single-table collection row.
        /// </summary>
        /// <param name="dtView">The table view to read.</param>
        /// <param name="rowIndex">The row index to read.</param>
        /// <returns>The row name, or an empty string when absent.</returns>
        private static string ReadName(DataTableView dtView, int rowIndex)
        {
            if (dtView.ColumnNames.Contains("Name"))
            {
                return dtView.GetCell("Name", rowIndex)?.ToString() ?? string.Empty;
            }

            object[] row = dtView.GetRow(rowIndex);
            return row.Length != 0 ? row[0]?.ToString() ?? string.Empty : string.Empty;
        }
    }
}
