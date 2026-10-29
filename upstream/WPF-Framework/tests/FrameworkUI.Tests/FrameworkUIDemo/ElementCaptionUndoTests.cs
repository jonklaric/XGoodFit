using System.Windows.Data;
using System.Windows.Media;
using FrameworkInterfaces;
using FrameworkUI.Demo;
using Xceed.Wpf.AvalonDock.Layout;
using Xunit;

namespace FrameworkUI.Tests.FrameworkUIDemo;

/// <summary>Checks element rename replay through the actual docking title binding.</summary>
public class ElementCaptionUndoTests
{
    /// <summary>Undo and redo update the title without changing dirty state or replacing its binding.</summary>
    [Fact]
    public void RenameUndoRedo_UpdatesAvalonDockTitleAndPreservesBinding()
    {
        DispatcherTestHost.Run(() =>
        {
            var element = new HazardElement("Original", new HazardElementCollection(DemoProject.GetInstance()));
            var document = new LayoutDocument();
            try
            {
                BindingOperations.SetBinding(document, LayoutContent.TitleProperty,
                    new Binding(nameof(element.DisplayName)) { Source = element });
                var expression = BindingOperations.GetBindingExpression(document, LayoutContent.TitleProperty);
                Assert.Equal("Original", document.Title);
                element.Name = "Renamed";
                Assert.Equal("Renamed*", document.Title);
                var notifications = new List<string?>();
                element.PropertyChanged += (_, args) => notifications.Add(args.PropertyName);
                element.UndoManager.Undo();
                Assert.Equal("Original", element.Name);
                Assert.True(element.IsDirty);
                Assert.Equal("Original*", element.DisplayName);
                Assert.Equal("Original*", document.Title);
                Assert.True(notifications.IndexOf(nameof(element.Name)) < notifications.IndexOf(nameof(element.DisplayName)));
                Assert.DoesNotContain(nameof(element.IsDirty), notifications);
                Assert.Same(expression, BindingOperations.GetBindingExpression(document, LayoutContent.TitleProperty));
                element.UndoManager.Redo();
                Assert.Equal("Renamed", element.Name);
                Assert.True(element.IsDirty);
                Assert.Equal("Renamed*", element.DisplayName);
                Assert.Equal("Renamed*", document.Title);
                Assert.Same(expression, BindingOperations.GetBindingExpression(document, LayoutContent.TitleProperty));
            }
            finally
            {
                BindingOperations.ClearBinding(document, LayoutContent.TitleProperty);
                element.UndoManager.Clear();
            }
        });
    }

    /// <summary>Docking layout-item creation retains the document's source binding.</summary>
    [Fact]
    public void DockingManagerLayoutItem_RetainsTitleBindingAcrossRenames()
    {
        DispatcherTestHost.Run(() =>
        {
            var element = new HazardElement("DockOriginal", new HazardElementCollection(DemoProject.GetInstance()));
            var document = new LayoutDocument { Content = new System.Windows.Controls.TextBlock() };
            var pane = new LayoutDocumentPane(document);
            try
            {
                BindingOperations.SetBinding(document, LayoutContent.TitleProperty,
                    new Binding(nameof(element.DisplayName)) { Source = element });
                var expression = BindingOperations.GetBindingExpression(document, LayoutContent.TitleProperty);
                var manager = new Xceed.Wpf.AvalonDock.DockingManager
                {
                    Layout = new LayoutRoot { RootPanel = new LayoutPanel(pane) }
                };
                Assert.NotNull(manager.GetLayoutItemFromModel(document));
                Assert.Same(expression, BindingOperations.GetBindingExpression(document, LayoutContent.TitleProperty));
                element.Name = "DockFirst";
                Assert.Equal("DockFirst*", document.Title);
                element.Name = "DockSecond";
                Assert.Equal("DockSecond*", document.Title);
                Assert.Same(expression, BindingOperations.GetBindingExpression(document, LayoutContent.TitleProperty));
            }
            finally
            {
                pane.Children.Remove(document);
                BindingOperations.ClearBinding(document, LayoutContent.TitleProperty);
                element.UndoManager.Clear();
            }
        });
    }

    /// <summary>Notify-only and undo-disabled renames refresh captions without changing dirty state.</summary>
    /// <param name="dirty">The existing dirty state.</param>
    /// <param name="undoEnabled">Whether undo recording remains enabled.</param>
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void RenameWithoutDirtyPromotion_PreservesDirtyStateAndNotificationOrder(bool dirty, bool undoEnabled)
    {
        var element = new CaptionElement();
        element.SetDirty(dirty);
        element.IsUndoEnabled = undoEnabled;
        element.PromoteDirty = !undoEnabled;
        var notifications = new List<string?>();
        element.PropertyChanged += (_, args) => notifications.Add(args.PropertyName);
        try
        {
            element.Name = "Changed";
            Assert.Equal("Changed", element.Name);
            Assert.Equal(dirty ? "Changed*" : "Changed", element.DisplayName);
            Assert.Equal(dirty, element.IsDirty);
            Assert.Equal(new[] { nameof(element.Name), nameof(element.DisplayName) }, notifications);
            notifications.Clear();
            element.Name = "Changed";
            Assert.Empty(notifications);
        }
        finally
        {
            element.UndoManager.Clear();
        }
    }

    /// <summary>Provides a minimal name setter using the production base notification path.</summary>
    private sealed class CaptionElement : ElementBase
    {
        /// <summary>Initializes an unattached clean element.</summary>
        public CaptionElement() : base("Original", null!) { }
        /// <summary>Controls dirty promotion by the name setter.</summary>
        public bool PromoteDirty { get; set; } = true;
        /// <inheritdoc/>
        public override string Name
        {
            get => NameField;
            set
            {
                if (NameField == value) return;
                var old = NameField;
                NameField = value;
                RecordPropertyChange(nameof(Name), old, value, PromoteDirty);
            }
        }
        /// <summary>Sets the fixture's initial dirty state.</summary>
        /// <param name="dirty">The initial state.</param>
        public void SetDirty(bool dirty) => SetIsDirty(dirty);
        /// <inheritdoc/>
        public override string Description { get; set; } = string.Empty;
        /// <inheritdoc/>
        public override DateTime CreationDate => DateTime.MinValue;
        /// <inheritdoc/>
        public override DateTime LastModified => DateTime.MinValue;
        /// <inheritdoc/>
        public override bool IsValid => true;
        /// <inheritdoc/>
        public override string NameOnDisk => Name;
        /// <inheritdoc/>
        public override ImageSource ElementImage => null!;
        /// <inheritdoc/>
        public override bool CanCopyFromExternal => false;
        /// <inheritdoc/>
        public override void Open() => throw new NotSupportedException();
        /// <inheritdoc/>
        public override void Save() => throw new NotSupportedException();
        /// <inheritdoc/>
        public override IElement Copy(string? newName = null) => throw new NotSupportedException();
        /// <inheritdoc/>
        public override IElement CopyFromExternal(string itemName, string fullFileName) => throw new NotSupportedException();
        /// <inheritdoc/>
        public override void Delete() => throw new NotSupportedException();
    }
}
