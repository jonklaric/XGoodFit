using Avalonia.Controls;
using FrameworkInterfaces;
using FrameworkUI;

namespace RMC_BestFit
{
    /// <summary>
    /// XGoodFit port: placeholder. A stand-in for upstream's MainProjectNode (GUI/MainProjectNode.cs), the
    /// project explorer's root node and the shell's controller, until it's ported: the project's
    /// elements show in the explorer, but there are no menus, documents or properties.
    /// </summary>
    public class MainProjectNode : FrameworkUIController
    {
        /// <summary>Creates the project node.</summary>
        /// <param name="project">The BestFit project.</param>
        public MainProjectNode(IProject project) : base(project)
        {
        }

        /// <inheritdoc/>
        public override bool CanMultiSelect => false;

        /// <inheritdoc/>
        protected override void DefineProjectExplorerMenuItems()
        {
        }

        /// <inheritdoc/>
        public override Control GetDocumentControl(IElement element) => null;

        /// <inheritdoc/>
        public override void DocumentClosed(UIElement documentControl)
        {
        }

        /// <inheritdoc/>
        public override void PropertiesClosed(UIElement propertiesControl)
        {
        }

        /// <inheritdoc/>
        public override Control GetPropertiesControl(UIElement documentControl) => null;

        /// <inheritdoc/>
        public override Control GetPropertiesControl(IElement element) => null;

        /// <inheritdoc/>
        public override IElement GetControlElement(UIElement control) => null;
    }
}
