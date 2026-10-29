using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows.Media;
using FrameworkInterfaces;
using FrameworkUI.ProjectExplorer;
using Xunit;

namespace FrameworkUI.Tests.ProjectExplorer;

/// <summary>Checks compatibility and recovery when loading saved project-explorer layouts.</summary>
public class ProjectNodeLayoutCompatibilityTests
{
    /// <summary>Verifies that a legacy grouped layout preserves group order and node expansion state.</summary>
    [StaFact]
    public void Load_WithLegacyGroupedXml_PreservesGroupsOrderAndExpansion()
    {
        var project = CreateProject();
        project.ProjectExplorerLayout =
            """
            <Node NodeType="ProjectNode" Name="Legacy" IsExpanded="true">
              <Node NodeType="ElementNodeCollection" Name="Input Data" IsExpanded="false">
                <Node NodeType="ElementNode" Name="Root Item" IsExpanded="true" />
                <Node NodeType="ElementNodeGroup" Name="Final Set" IsExpanded="true">
                  <Node NodeType="ElementNode" Name="Grouped A" IsExpanded="true" />
                  <Node NodeType="ElementNode" Name="Grouped B" IsExpanded="false" />
                </Node>
              </Node>
              <Node NodeType="ElementNodeCollection" Name="Univariate Distribution Analysis" IsExpanded="true">
                <Node NodeType="ElementNode" Name="Analysis A" IsExpanded="true" />
              </Node>
            </Node>
            """;

        var node = new TestProjectNode(project);

        var input = Assert.IsType<ElementNodeCollection>(node.ChildNodes[0]);
        Assert.False(input.IsExpanded);
        Assert.Equal("Root Item", Assert.IsType<ElementNode>(input.ChildNodes[0]).Element.Name);

        var group = Assert.IsType<ElementNodeGroup>(input.ChildNodes[1]);
        Assert.Equal("Final Set", group.NodeHeader.HeaderText);
        Assert.True(group.IsExpanded);
        Assert.Equal("Grouped A", Assert.IsType<ElementNode>(group.ChildNodes[0]).Element.Name);
        Assert.False(group.ChildNodes[1].IsExpanded);

        var analysis = Assert.IsType<ElementNodeCollection>(node.ChildNodes[1]);
        Assert.Equal("Univariate Distribution Analysis", analysis.ElementCollection?.Name);
    }

    /// <summary>Verifies that the current layout loads when no previous layout is available.</summary>
    [StaFact]
    public void Load_WithNoPreviousLayout_LoadsCurrentLayoutOnly()
    {
        var project = CreateProject();
        project.ProjectExplorerLayout =
            """
            <Node NodeType="ProjectNode" Name="Legacy" IsExpanded="true">
              <Node NodeType="ElementNodeCollection" Name="Input Data" IsExpanded="true">
                <Node NodeType="ElementNodeGroup" Name="Only Current" IsExpanded="true">
                  <Node NodeType="ElementNode" Name="Grouped A" IsExpanded="true" />
                </Node>
              </Node>
            </Node>
            """;
        project.SetPreviousProjectExplorerLayout(string.Empty);

        var node = new TestProjectNode(project);

        var input = Assert.IsType<ElementNodeCollection>(node.ChildNodes[0]);
        Assert.Contains(input.ChildNodes, child => child is ElementNodeGroup group && group.NodeHeader.HeaderText == "Only Current");
    }

    /// <summary>Verifies that stale elements are omitted while groups and unlisted current elements are retained.</summary>
    [StaFact]
    public void Load_WithStaleElementInsideGroup_KeepsGroupAndBackfillsCurrentElements()
    {
        var project = CreateProject();
        project.ProjectExplorerLayout =
            """
            <Node NodeType="ProjectNode" Name="Legacy" IsExpanded="true">
              <Node NodeType="ElementNodeCollection" Name="Input Data" IsExpanded="true">
                <Node NodeType="ElementNodeGroup" Name="Final Set" IsExpanded="true">
                  <Node NodeType="ElementNode" Name="Grouped A" IsExpanded="true" />
                  <Node NodeType="ElementNode" Name="Renamed Or Deleted" IsExpanded="true" />
                </Node>
              </Node>
            </Node>
            """;

        var node = new TestProjectNode(project);

        var input = Assert.IsType<ElementNodeCollection>(node.ChildNodes[0]);
        var group = Assert.IsType<ElementNodeGroup>(input.ChildNodes[0]);
        Assert.Single(group.ChildNodes);
        Assert.Equal("Grouped A", Assert.IsType<ElementNode>(group.ChildNodes[0]).Element.Name);
        Assert.Contains(input.ChildNodes, child => child is ElementNode element && element.Element.Name == "Grouped B");
        Assert.Contains(input.ChildNodes, child => child is ElementNode element && element.Element.Name == "Root Item");
    }

    /// <summary>Verifies that a newly added collection is appended without removing saved groups.</summary>
    [StaFact]
    public void Load_WithCollectionMissingFromXml_AppendsCollectionWithoutLosingGroups()
    {
        var project = CreateProject();
        project.AddCollection("Rating Curve Analysis", "Rating A");
        project.ProjectExplorerLayout =
            """
            <Node NodeType="ProjectNode" Name="Legacy" IsExpanded="true">
              <Node NodeType="ElementNodeCollection" Name="Input Data" IsExpanded="true">
                <Node NodeType="ElementNodeGroup" Name="Final Set" IsExpanded="true">
                  <Node NodeType="ElementNode" Name="Grouped A" IsExpanded="true" />
                </Node>
              </Node>
            </Node>
            """;

        var node = new TestProjectNode(project);

        var input = Assert.IsType<ElementNodeCollection>(node.ChildNodes[0]);
        Assert.Contains(input.ChildNodes, child => child is ElementNodeGroup group && group.NodeHeader.HeaderText == "Final Set");
        Assert.Contains(node.ChildNodes, child => child is ElementNodeCollection collection && collection.ElementCollection?.Name == "Rating Curve Analysis");
    }

    /// <summary>Verifies that previous groups are recovered when the current layout contains only flat elements.</summary>
    [StaFact]
    public void Load_WithCurrentFlatLayoutAndPreviousGroupedLayout_RecoversPreviousGroups()
    {
        var project = CreateProject();
        project.ProjectExplorerLayout =
            """
            <Node NodeType="ProjectNode" Name="Flat" IsExpanded="true">
              <Node NodeType="ElementNodeCollection" Name="Input Data" IsExpanded="true">
                <Node NodeType="ElementNode" Name="Root Item" IsExpanded="true" />
                <Node NodeType="ElementNode" Name="Grouped A" IsExpanded="true" />
                <Node NodeType="ElementNode" Name="Grouped B" IsExpanded="true" />
              </Node>
              <Node NodeType="ElementNodeCollection" Name="Univariate Distribution Analysis" IsExpanded="true">
                <Node NodeType="ElementNode" Name="Analysis A" IsExpanded="true" />
              </Node>
            </Node>
            """;
        project.SetPreviousProjectExplorerLayout(
            """
            <Node NodeType="ProjectNode" Name="Grouped" IsExpanded="true">
              <Node NodeType="ElementNodeCollection" Name="Input Data" IsExpanded="true">
                <Node NodeType="ElementNodeGroup" Name="Recovered" IsExpanded="true">
                  <Node NodeType="ElementNode" Name="Grouped A" IsExpanded="true" />
                </Node>
              </Node>
              <Node NodeType="ElementNodeCollection" Name="Univariate Distribution Analysis" IsExpanded="true">
                <Node NodeType="ElementNode" Name="Analysis A" IsExpanded="true" />
              </Node>
            </Node>
            """);

        var node = new TestProjectNode(project);

        var input = Assert.IsType<ElementNodeCollection>(node.ChildNodes[0]);
        Assert.Contains(input.ChildNodes, child => child is ElementNodeGroup group && group.NodeHeader.HeaderText == "Recovered");
    }

    /// <summary>Verifies that malformed current layout XML falls back to the previous grouped layout.</summary>
    [StaFact]
    public void Load_WithMalformedCurrentLayout_FallsBackSafely()
    {
        var project = CreateProject();
        project.ProjectExplorerLayout = "<Node><Node";
        project.SetPreviousProjectExplorerLayout(
            """
            <Node NodeType="ProjectNode" Name="Grouped" IsExpanded="true">
              <Node NodeType="ElementNodeCollection" Name="Input Data" IsExpanded="true">
                <Node NodeType="ElementNodeGroup" Name="Previous" IsExpanded="true">
                  <Node NodeType="ElementNode" Name="Grouped A" IsExpanded="true" />
                </Node>
              </Node>
            </Node>
            """);

        var node = new TestProjectNode(project);

        var input = Assert.IsType<ElementNodeCollection>(node.ChildNodes[0]);
        Assert.Contains(input.ChildNodes, child => child is ElementNodeGroup group && group.NodeHeader.HeaderText == "Previous");
    }

    private static FakeProject CreateProject()
    {
        return new FakeProject(
            ("Input Data", new[] { "Root Item", "Grouped A", "Grouped B" }),
            ("Univariate Distribution Analysis", new[] { "Analysis A" }));
    }

    private sealed class TestProjectNode : ProjectNode
    {
        public TestProjectNode(IProject project)
            : base(project)
        {
            Load();
        }

        public override bool CanMultiSelect => false;

        protected override void DefineProjectExplorerMenuItems()
        {
        }
    }

    private sealed class FakeProject : ProjectBase
    {
        private readonly List<IElementCollection> _collections = new();

        public FakeProject(params (string Name, string[] ElementNames)[] collections)
        {
            _name = "Fake Project";
            _description = string.Empty;
            _creationDate = DateTime.UtcNow;
            _lastModified = _creationDate;

            foreach (var collection in collections)
            {
                AddCollection(collection.Name, collection.ElementNames);
            }
        }

        public override string Name
        {
            get => _name;
            set => _name = value;
        }

        public override string Description
        {
            get => _description;
            set => _description = value;
        }

        public override string SoftwareVersion => "2.4";

        public override ImageSource ProjectImage { get; } = new DrawingImage();

        public void AddCollection(string name, params string[] elementNames)
        {
            _collections.Add(new FakeElementCollection(this, name, elementNames));
            _readOnlyElementCollections = new ReadOnlyCollection<IElementCollection>(_collections);
        }

        public void SetPreviousProjectExplorerLayout(string layout)
        {
            _projectExplorerLayoutPrevious = layout;
        }

        public override bool IsValid()
        {
            return true;
        }

        public override void CreateNew(string newFullFileName)
        {
            FullFileName = newFullFileName;
        }

        public override void Open()
        {
        }

        public override void Save()
        {
        }

        public override void Close()
        {
        }

        public override void Compact()
        {
        }

        public override void Optimize()
        {
        }
    }

    private sealed class FakeElementCollection : ElementCollectionBase
    {
        private readonly string _name;

        public FakeElementCollection(IProject parentProject, string name, IEnumerable<string> elementNames)
            : base(parentProject)
        {
            _name = name;

            foreach (var elementName in elementNames)
            {
                Add(new FakeElement(elementName, this));
            }
        }

        public override string Name => _name;

        public override void Open()
        {
        }

        public override void Add(IElement item)
        {
            ElementList.Add(item);
            RaiseElementAddedEvent(item);
        }

        public override void Insert(int index, IElement item)
        {
            ElementList.Insert(index, item);
            RaiseElementAddedEvent(item);
        }

        public override void InsertFromExternalProject(int index, string elementName, string elementType, string fullFileName)
        {
            Insert(index, new FakeElement(elementName, this));
        }

        public override void Delete()
        {
        }
    }

    private sealed class FakeElement : ElementBase
    {
        public FakeElement(string name, IElementCollection parentCollection)
            : base(name, parentCollection)
        {
            _description = string.Empty;
            _creationDate = DateTime.UtcNow;
            _lastModified = _creationDate;
            _isValid = true;
        }

        public override string Name
        {
            get => NameField;
            set => NameField = value;
        }

        public override string Description
        {
            get => _description;
            set => _description = value;
        }

        public override DateTime CreationDate => _creationDate;

        public override DateTime LastModified => _lastModified;

        public override bool IsValid => _isValid;

        public override string NameOnDisk => Name;

        public override ImageSource ElementImage { get; } = new DrawingImage();

        public override bool CanCopyFromExternal => false;

        public override void Open()
        {
        }

        public override void Save()
        {
        }

        public override IElement Copy(string? newName = null)
        {
            return new FakeElement(newName ?? Name, ParentCollection);
        }

        public override IElement CopyFromExternal(string itemName, string fullFileName)
        {
            return new FakeElement(itemName, ParentCollection);
        }

        public override void Delete()
        {
        }
    }
}
