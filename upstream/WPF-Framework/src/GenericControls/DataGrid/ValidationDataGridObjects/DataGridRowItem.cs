using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GenericControls
{
    /// <summary>
    /// Abstract base class for rows in a ValidationDataGrid, providing validation rule management and property change notification.
    /// </summary>
    /// <remarks>
    /// <para>
    ///     <b> Authors: </b>
    /// <list type="bullet">
    /// <item><description>
    ///     Woodrow Fields, USACE Risk Management Center, woodrow.l.fields@usace.army.mil
    /// </description></item>
    /// <item><description>
    ///     Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil
    /// </description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public abstract class DataGridRowItem : INotifyPropertyChanged
    {

        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="DataGridRowItem"/> class.
        /// </summary>
        /// <param name="list">The observable collection of all data grid row items.</param>
        /// <param name="parentDataGrid">Optional. The parent validation data grid. Default = null.</param>
        public DataGridRowItem(System.Collections.ObjectModel.ObservableCollection<object> list, ValidationDataGrid parentDataGrid = null)
        {
            _parentList = list;
            _parentDataGrid = parentDataGrid;
            AddValidationRules();
            // this is required to ensure that each property has a rule so that the binding will not throw a key not found error.  this could be fixed by adding an attribute [validates] to each property, and only binding error state display to those columns.
            System.Reflection.PropertyInfo[] pinfo = GetType().GetProperties();
            foreach (System.Reflection.PropertyInfo p in pinfo)
            {
                if (p.Name == nameof(RuleMap))
                    continue;
                if (_ruleMap.ContainsKey(p.Name) == false)
                    AddRule(p.Name, () => false, "");
            }
        }

        #endregion

        #region Members

        /// <summary>
        /// Dictionary mapping property names to their validation rules.
        /// </summary>
        protected Dictionary<string, PropertyRule> _ruleMap = new Dictionary<string, PropertyRule>();

        /// <summary>
        /// The parent collection containing all row items.
        /// </summary>
        protected System.Collections.ObjectModel.ObservableCollection<object> _parentList;

        /// <summary>
        /// Dictionary mapping properties to their associated dependent properties for validation.
        /// </summary>
        protected Dictionary<string, HashSet<string>> _associatedProperties = new Dictionary<string, HashSet<string>>();

        private ValidationDataGrid _parentDataGrid;
        private bool _recurse = true;

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets or sets the list that this row item belongs to.
        /// This is used for complex validation rules that require knowledge of neighbors or all other items in the list.
        /// </summary>
        public System.Collections.ObjectModel.ObservableCollection<object> ParentList
        {
            set
            {
                _parentList = value;
            }
        }

        /// <summary>
        /// Gets the map of all property validation rules for this row item.
        /// </summary>
        public Dictionary<string, PropertyRule> RuleMap
        {
            get
            {
                return _ruleMap;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// When overridden in a derived class, defines validation rules for properties.
        /// Use the <see cref="AddRule"/> method to add specific rules.
        /// </summary>
        public abstract void AddValidationRules();

        /// <summary>
        /// When overridden in a derived class, provides a display-friendly name for a property.
        /// This name appears as the column header and in other UI contexts.
        /// </summary>
        /// <param name="propertyName">The property name that needs to be transformed into a display name.</param>
        /// <returns>The display-friendly name for the property.</returns>
        public abstract string PropertyDisplayName(string propertyName);

        /// <summary>
        /// When overridden in a derived class, determines whether a property should be displayed in the data grid.
        /// </summary>
        /// <param name="propertyName">The property name to check.</param>
        /// <returns>True if the property should be displayed; otherwise, false.</returns>
        public abstract bool IsGridDisplayable(string propertyName);

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event and validates the property.
        /// </summary>
        /// <param name="propertyName">Optional. Name of the property that changed.</param>
        protected void NotifyPropertyChanged([CallerMemberName] string propertyName = null)
        {
            ValidateProperty(propertyName);
            // this simplifies the number of calls in the setter.. and sets up the default behavior.
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Forces validation of all properties in this row item.
        /// This is useful when the grid is first displayed or when rows are added.
        /// </summary>
        public void ForceValidation()
        {
            foreach (string propertyName in _ruleMap.Keys)
                ValidateProperty(propertyName);
        }

        /// <summary>
        /// Adds a validation rule for a specific property.
        /// When the error condition is met, the cell will be highlighted and the error message will show as a tooltip.
        /// </summary>
        /// <param name="propertyName">The property name that the rule will be applied to.</param>
        /// <param name="errorCondition">A function that returns true when an error condition is detected.</param>
        /// <param name="errorMessage">The error message to display in the tooltip when the error condition is true.</param>
        /// <param name="associatedProperties">Optional. Properties associated with the target property that trigger revalidation when changed.</param>
        protected void AddRule(string propertyName, Func<bool> errorCondition, string errorMessage, string[] associatedProperties = null)
        {
            if (_ruleMap.ContainsKey(propertyName))
            {
                _ruleMap[propertyName].AddRule(errorCondition, errorMessage);
            }
            else
            {
                _ruleMap.Add(propertyName, new PropertyRule(errorCondition, errorMessage));
            }
            if (associatedProperties == null)
                return;
            // Update associated properties. The dictionary key is the associated property so that when validate gets called on an associated property it will update the target property.
            if (_associatedProperties.ContainsKey(propertyName) == false)
                _associatedProperties.Add(propertyName, new HashSet<string>());
            foreach (string assProp in associatedProperties)
            {
                if (_associatedProperties.ContainsKey(assProp) == false)
                    _associatedProperties.Add(assProp, new HashSet<string>());
                _associatedProperties[assProp].Add(propertyName);
            }
        }

        /// <summary>
        /// Validates a specific property and any associated properties.
        /// This is typically called automatically via property change notification.
        /// </summary>
        /// <param name="propertyName">The name of the property to validate.</param>
        public void ValidateProperty(string propertyName)
        {
            if (_parentDataGrid is not null && _parentDataGrid.SuppressValidation == true)
                return;
            if (_ruleMap.ContainsKey(propertyName))
                _ruleMap[propertyName].ExecuteRules();
            // 
            if (_associatedProperties.ContainsKey(propertyName))
            {
                foreach (string _property in _associatedProperties[propertyName])
                    _ruleMap[_property].ExecuteRules(); // the property is guaranteed to be in the rule map since it was added in the addrule sub.
            }
        }

        /// <summary>
        /// Checks if a property value is unique within the parent list of DataGridRowItems.
        /// If a duplicate is found, sets error messages on the duplicate rows accordingly.
        /// </summary>
        /// <param name="propertyName">The name of the property to check for uniqueness.</param>
        /// <param name="errorMessage">The error message to use if a duplicate is found.</param>
        /// <returns>True if the current row has a duplicate value; otherwise, false.</returns>
        protected bool UniqueRule(string propertyName, string errorMessage)
        {
            if (_parentDataGrid is not null && _parentDataGrid.SuppressValidation == true)
                return default;
            if (_parentDataGrid is not null && _parentDataGrid.PerformingBulkValidation == true)
                return default;
            bool @bool = false;
            if (_parentList.Count >= 1)
            {
                var prop = GetType().GetProperty(propertyName);
                if (prop == null) return false;
                for (int i = 0, loopTo = _parentList.Count - 1; i <= loopTo; i++)
                {
                    DataGridRowItem iRowItem = (DataGridRowItem)_parentList[i];
                    string iValue = prop.GetValue(iRowItem, null)?.ToString() ?? string.Empty;
                    bool hasDuplicate = false;
                    //
                    // Check for duplicates
                    for (int j = 0, loopTo1 = _parentList.Count - 1; j <= loopTo1; j++)
                    {
                        DataGridRowItem jRowItem = (DataGridRowItem)_parentList[j];
                        string jValue = prop.GetValue(jRowItem, null)?.ToString() ?? string.Empty;
                        if (jRowItem.Equals(iRowItem))
                            continue;
                        // 
                        if ((iValue ?? "") == (jValue ?? ""))
                        {
                            hasDuplicate = true;
                        }
                    }
                    // 
                    if (iRowItem.Equals(this))
                    {
                        @bool = hasDuplicate;
                        continue;
                    }
                    // 
                    // If not ME, then update row item error boolean and error message.
                    bool hasError = iRowItem.RuleMap[propertyName].HasError;
                    var hasErrorMessage = iRowItem.RuleMap[propertyName].ErrorMessage.Split(Environment.NewLine.ToCharArray()).ToList();
                    for (int j = hasErrorMessage.Count - 1; j >= 0; j -= 1)
                    {
                        if ((hasErrorMessage[j] ?? "") == (Environment.NewLine ?? "") || string.IsNullOrEmpty(hasErrorMessage[j]) || (hasErrorMessage[j] ?? "") == (errorMessage ?? ""))
                            hasErrorMessage.RemoveAt(j);
                    }
                    if (hasDuplicate == true)
                    {
                        if (hasError == false)
                            iRowItem.RuleMap[propertyName].HasError = hasDuplicate;
                        hasErrorMessage.Add(errorMessage);
                    }
                    // Need to check if there was previously just 1 error, and if that error was this one.
                    else if (hasError == true && hasErrorMessage.Count == 0)
                    {
                        iRowItem.RuleMap[propertyName].HasError = hasDuplicate;
                    }
                    iRowItem.RuleMap[propertyName].ErrorMessage = "";
                    for (int j = 0, loopTo2 = hasErrorMessage.Count - 1; j <= loopTo2; j++)
                        iRowItem.RuleMap[propertyName].ErrorMessage += j == 0 ? hasErrorMessage[j] : Environment.NewLine + hasErrorMessage[j];
                }
            }

            return @bool;
        }

        /// <summary>
        /// Validates that the current row's value (from a callback function) follows the correct order
        /// relative to its neighbors in the parent list (ascending or descending).
        /// </summary>
        /// <typeparam name="T">Comparable value type (unused but reserved for future use).</typeparam>
        /// <typeparam name="DT">Row type derived from DataGridRowItem.</typeparam>
        /// <param name="callBack">A function that retrieves the double value to compare.</param>
        /// <param name="propertyName">The property name used to revalidate neighboring rows if needed.</param>
        /// <param name="ascending">Whether the order should be ascending (true) or descending (false).</param>
        /// <param name="canBeEqual">Whether equal values are allowed (true) or not (false).</param>
        /// <returns>True if the ordering rule is violated; otherwise, false.</returns>
        protected bool OrderRule<T, DT>(Func<DT, double> callBack, string propertyName, bool @ascending = true, bool canBeEqual = true)
                where T : IComparable
                where DT : DataGridRowItem
        {
            if (_parentDataGrid is not null && _parentDataGrid.SuppressValidation == true)
                return default;
            if (_parentList == null)
                return false;
            int currentIndex = _parentList.IndexOf(this);
            if (currentIndex == -1)
                return false;
            double currentValue = callBack((DT)this); // _parentList(currentIndex).GetType().GetProperty(propertyName).GetValue(_parentList(currentIndex))
            int previousIndex = currentIndex - 1;
            int nextIndex = currentIndex + 1;
            // 
            // value has changed so the next and previous row needs to check to make sure it is still ordered.
            if (_recurse == true)
            {
                _recurse = false;
                if (previousIndex >= 0)
                {
                    ((DataGridRowItem)_parentList[previousIndex])._recurse = false;
                    ((DataGridRowItem)_parentList[previousIndex]).RuleMap[propertyName].ExecuteRules();
                    ((DataGridRowItem)_parentList[previousIndex])._recurse = true;
                }
                if (nextIndex < _parentList.Count)
                {
                    ((DataGridRowItem)_parentList[nextIndex])._recurse = false;
                    ((DataGridRowItem)_parentList[nextIndex]).RuleMap[propertyName].ExecuteRules();
                    ((DataGridRowItem)_parentList[nextIndex])._recurse = true;
                }
                _recurse = true;
            }
            // Verify that the changed value is not smaller than the previous row value.
            if (previousIndex >= 0)
            {
                double previousValue = callBack((DT)_parentList[previousIndex]);
                // 
                if (currentValue == previousValue)
                {
                    if (canBeEqual == false)
                        return true;
                }
                else if (ascending == true)
                {
                    if (currentValue < previousValue)
                        return true;
                }
                else if (currentValue > previousValue)
                    return true;
            }
            // 
            return false;
        }

        #endregion
    }
}