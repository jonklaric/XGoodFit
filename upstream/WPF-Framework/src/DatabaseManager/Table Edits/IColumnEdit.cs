using System;

namespace DatabaseManager
{
    /// <summary>
    /// Represents metadata about a column edit, including index, name, data type,
    /// and whether the column is being added.
    /// </summary>
    public interface IColumnEdit
    {
        /// <summary>
        /// Gets the index of the column being edited.
        /// </summary>
        int ColumnIndex { get; }

        /// <summary>
        /// Gets the name of the column being edited.
        /// </summary>
        string ColumnName { get; }

        /// <summary>
        /// Gets the data type of the column being edited.
        /// </summary>
        Type ColumnDataType { get; }

        /// <summary>
        /// Gets a value indicating whether this edit is a column addition.
        /// </summary>
        bool IsColumnAdd { get; }
    }
}