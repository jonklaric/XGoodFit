using System.ComponentModel;
using System.Windows.Controls;

namespace GenericControls
{
    /// <summary>
    /// A user control for displaying property attributes such as display name and description.
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
    public partial class PropertyAttributes:UserControl
    {

        #region Construction

        /// <summary>
        /// Initialize an empty control.
        /// </summary>
        public PropertyAttributes()
        {
            // This call is required by the designer.
            this.InitializeComponent();
            // Add any initialization after the InitializeComponent() call.
        }

        #endregion

        #region Methods

        /// <summary>
        /// Sets the default attribute description to display.
        /// </summary>
        /// <param name="displayName">The property display name.</param>
        /// <param name="description">The property description.</param>
        public void SetDefaultAttributes(string displayName, string description)
        {
            this.NameTextBlock.Text = displayName;
            this.Description.Text = description;
        }

        /// <summary>
        /// Gets the attributes for the specified class object.
        /// </summary>
        /// <param name="classObject">The class object.</param>
        public void GetClassAttributes(object classObject)
        {
            // Get the attributes for the class object.
            var attributes = TypeDescriptor.GetAttributes(classObject.GetType());
            // Update the name and description text boxes.
            var dnAttr = attributes[typeof(DisplayNameAttribute)] as DisplayNameAttribute;
            var descAttr = attributes[typeof(DescriptionAttribute)] as DescriptionAttribute;
            if (dnAttr != null) this.NameTextBlock.Text = dnAttr.DisplayName;
            if (descAttr != null) this.Description.Text = descAttr.Description;
        }

        /// <summary>
        /// Gets the attributes for the property from a specified class object.
        /// </summary>
        /// <param name="propertyName">The property name.</param>
        /// <param name="classObject">The class object.</param>
        public void GetPropertyAttributes(string propertyName, object classObject)
        {
            // Get property descriptors for the class.
            var propertyDescriptors = TypeDescriptor.GetProperties(classObject.GetType());
            // Get the attributes for property.
            var attributes = propertyDescriptors[propertyName].Attributes;
            // Update the name and description text boxes.
            var dnAttr = attributes[typeof(DisplayNameAttribute)] as DisplayNameAttribute;
            var descAttr = attributes[typeof(DescriptionAttribute)] as DescriptionAttribute;
            if (dnAttr != null) this.NameTextBlock.Text = dnAttr.DisplayName;
            if (descAttr != null) this.Description.Text = descAttr.Description;
        }

        #endregion

    }
}