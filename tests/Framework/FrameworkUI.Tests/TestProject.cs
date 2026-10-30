using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Avalonia.Media;
using FrameworkInterfaces;

namespace FrameworkUI.Tests;

/// <summary>
/// An in-memory project for the shell's tests: element collections of <see cref="TestElement"/>s, with
/// the layout strings and dirty flags a project file keeps, and a log of the calls the shell makes. Its
/// "file" is the elements of each collection when it was created or last saved: <see cref="Close"/>
/// empties the collections and <see cref="Open"/> fills them from the file, as a project file does.
/// </summary>
public sealed class TestProject : IProject
{
    private string _name = "Test Project";
    private string _fullFileName;
    private string _avalonDockLayout = string.Empty;
    private string _projectExplorerLayout = string.Empty;
    private readonly Dictionary<TestElementCollection, IElement[]> _file = [];

    /// <summary>Creates a project with collections of the given names, each with its elements.</summary>
    public TestProject(string fullFileName, params (string Collection, string[] Elements)[] collections)
    {
        _fullFileName = fullFileName;
        var list = new List<IElementCollection>();
        foreach ((string name, string[] elements) in collections)
        {
            var collection = new TestElementCollection(name, this);
            foreach (string element in elements)
            {
                collection.Add(new TestElement(element, collection));
            }
            list.Add(collection);
            _file[collection] = [.. collection];
        }
        ElementCollections = new ReadOnlyCollection<IElementCollection>(list);
    }

    /// <summary>The calls the shell has made, in order: Open, Save, Close, CreateNew, SaveAs.</summary>
    public List<string> Calls { get; } = [];

    /// <inheritdoc/>
    public string Name { get => _name; set { _name = value; Changed(nameof(Name)); } }

    /// <inheritdoc/>
    public string Description { get; set; } = "A project for the tests.";

    /// <inheritdoc/>
    public DateTime CreationDate { get; } = new(2026, 10, 30, 9, 0, 0);

    /// <inheritdoc/>
    public DateTime LastModified { get; } = new(2026, 10, 30, 10, 0, 0);

    /// <inheritdoc/>
    public bool IsDirty { get; set; }

    /// <inheritdoc/>
    public string NameOnDisk => _name;

    /// <inheritdoc/>
    public string FullFileName { get => _fullFileName; set { _fullFileName = value; Changed(nameof(FullFileName)); } }

    /// <inheritdoc/>
    public ReadOnlyCollection<IElementCollection>? ElementCollections { get; }

    /// <inheritdoc/>
    public string SoftwareVersion => "1.0.0";

    /// <inheritdoc/>
    public string? FileDirectory => Path.GetDirectoryName(FullFileName);

    /// <inheritdoc/>
    public string AvalonDockLayout { get => _avalonDockLayout; set { if (_avalonDockLayout != value) { _avalonDockLayout = value; LayoutDirty = true; } } }

    /// <inheritdoc/>
    public string ProjectExplorerLayout { get => _projectExplorerLayout; set { if (_projectExplorerLayout != value) { _projectExplorerLayout = value; LayoutDirty = true; } } }

    /// <inheritdoc/>
    public bool LayoutDirty { get; set; }

    /// <inheritdoc/>
    public string AvalonDockLayoutPrevious { get; set; } = string.Empty;

    /// <inheritdoc/>
    public string ProjectExplorerLayoutPrevious { get; set; } = string.Empty;

    /// <inheritdoc/>
    public IImage ProjectImage => null!;

    /// <inheritdoc/>
    public event PreviewObjectSavedEventHandler? PreviewObjectSaved;

    /// <inheritdoc/>
    public event ObjectSavedEventHandler? ObjectSaved;

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>The collection with <paramref name="name"/>.</summary>
    public TestElementCollection Collection(string name) => (TestElementCollection)ElementCollections!.First(c => c.Name == name);

    /// <inheritdoc/>
    public void Open()
    {
        Calls.Add(nameof(Open));
        foreach ((TestElementCollection collection, IElement[] elements) in _file)
        {
            collection.Load(elements);
        }
        IsDirty = false;
    }

    /// <inheritdoc/>
    public void Save()
    {
        Calls.Add(nameof(Save));
        bool cancel = false;
        PreviewObjectSaved?.Invoke(this, ref cancel);
        foreach (TestElementCollection collection in _file.Keys)
        {
            _file[collection] = [.. collection];
        }
        IsDirty = false;
        LayoutDirty = false;
        ObjectSaved?.Invoke(this);
    }

    /// <inheritdoc/>
    public void CreateNew(string newFullFileName) => Calls.Add($"{nameof(CreateNew)} {newFullFileName}");

    /// <inheritdoc/>
    public void Close()
    {
        Calls.Add(nameof(Close));
        foreach (TestElementCollection collection in _file.Keys)
        {
            collection.Clear();
        }
    }

    /// <inheritdoc/>
    public void SaveAs(string newFullFileName) => Calls.Add($"{nameof(SaveAs)} {newFullFileName}");

    /// <inheritdoc/>
    public void ZipProject(string zipFileName) => Calls.Add($"{nameof(ZipProject)} {zipFileName}");

    /// <inheritdoc/>
    public bool IsValid() => true;

    /// <inheritdoc/>
    public void Compact() => Calls.Add(nameof(Compact));

    /// <inheritdoc/>
    public void Optimize() => Calls.Add(nameof(Optimize));

    private void Changed(string property) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
}

/// <summary>An in-memory element collection.</summary>
public sealed class TestElementCollection(string name, IProject project) : IElementCollection
{
    private readonly List<IElement> _elements = [];

    /// <inheritdoc/>
    public string Name { get; } = name;

    /// <inheritdoc/>
    public IProject ParentProject { get; } = project;

    /// <inheritdoc/>
    public ReadOnlyCollection<IElementCollection> ElementCollections { get; set; } = new([]);

    /// <inheritdoc/>
    public bool IsDirty => _elements.Any(e => e.IsDirty);

    /// <inheritdoc/>
    public string NameOnDisk => Name;

    /// <inheritdoc/>
    public int Count => _elements.Count;

    /// <inheritdoc/>
    public bool IsReadOnly => false;

    /// <inheritdoc/>
    public IElement this[int index] { get => _elements[index]; set => _elements[index] = value; }

    /// <inheritdoc/>
    public event ElementAddedEventHandler? ElementAdded;

    /// <summary>How many handlers listen to <see cref="ElementAdded"/>.</summary>
    public int ElementAddedListeners => ElementAdded?.GetInvocationList().Length ?? 0;

    /// <inheritdoc/>
    public event ElementRemovedEventHandler? ElementRemoved;

    /// <inheritdoc/>
    public event EventHandler? ElementIsDirtyChanged;

    /// <inheritdoc/>
    public event PreviewObjectSavedEventHandler? PreviewObjectSaved;

    /// <inheritdoc/>
    public event ObjectSavedEventHandler? ObjectSaved;

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <inheritdoc/>
    public void Add(IElement item)
    {
        _elements.Add(item);
        ElementAdded?.Invoke(item);
    }

    /// <inheritdoc/>
    public void Insert(int index, IElement item)
    {
        _elements.Insert(index, item);
        ElementAdded?.Invoke(item);
    }

    /// <inheritdoc/>
    public bool Remove(IElement item)
    {
        bool removed = _elements.Remove(item);
        if (removed) ElementRemoved?.Invoke(item);
        return removed;
    }

    /// <inheritdoc/>
    public void RemoveAt(int index) => Remove(_elements[index]);

    /// <inheritdoc/>
    public void Clear() => _elements.Clear();

    /// <summary>Replaces the elements, as opening a project file does, without raising events.</summary>
    public void Load(IEnumerable<IElement> elements)
    {
        _elements.Clear();
        _elements.AddRange(elements);
    }

    /// <inheritdoc/>
    public bool Contains(IElement item) => _elements.Contains(item);

    /// <inheritdoc/>
    public void CopyTo(IElement[] array, int arrayIndex) => _elements.CopyTo(array, arrayIndex);

    /// <inheritdoc/>
    public int IndexOf(IElement item) => _elements.IndexOf(item);

    /// <inheritdoc/>
    public IEnumerator<IElement> GetEnumerator() => _elements.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <inheritdoc/>
    public void Sort(IComparer<IElement> sortAction) => _elements.Sort(sortAction);

    /// <inheritdoc/>
    public void MoveElement(IElement element, int startIndex, int endIndex)
    {
        _elements.RemoveAt(startIndex);
        _elements.Insert(endIndex, element);
    }

    /// <inheritdoc/>
    public void Delete() => _elements.Clear();

    /// <inheritdoc/>
    public void InsertFromExternalProject(int index, string elementName, string elementType, string fullFileName) =>
        Insert(index, new TestElement(elementName, this));

    /// <inheritdoc/>
    public void Open()
    {
    }

    /// <inheritdoc/>
    public void Save()
    {
        bool cancel = false;
        PreviewObjectSaved?.Invoke(this, ref cancel);
        ObjectSaved?.Invoke(this);
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsDirty)));
        ElementIsDirtyChanged?.Invoke(this, EventArgs.Empty);
    }
}

/// <summary>An in-memory element, which counts its saves and opens.</summary>
public sealed class TestElement(string name, IElementCollection collection) : IElement
{
    private string _name = name;
    private bool _isDirty;

    /// <inheritdoc/>
    public string Name { get => _name; set { _name = value; Changed(nameof(Name)); Changed(nameof(DisplayName)); } }

    /// <inheritdoc/>
    public string Description { get; set; } = $"The element {name}.";

    /// <inheritdoc/>
    public DateTime CreationDate { get; } = new(2026, 10, 30, 9, 0, 0);

    /// <inheritdoc/>
    public DateTime LastModified { get; } = new(2026, 10, 30, 10, 0, 0);

    /// <inheritdoc/>
    public string DisplayName => Name;

    /// <inheritdoc/>
    public IElementCollection ParentCollection { get; } = collection;

    /// <inheritdoc/>
    public IImage ElementImage => null!;

    /// <inheritdoc/>
    public string? ElementImageResourceKey => "ProjectPropertiesImage";

    /// <inheritdoc/>
    public bool CanCopyFromExternal => true;

    /// <inheritdoc/>
    public bool IsValid => true;

    /// <inheritdoc/>
    public bool IsDirty { get => _isDirty; set { _isDirty = value; Changed(nameof(IsDirty)); } }

    /// <inheritdoc/>
    public string NameOnDisk => Name;

    /// <summary>How many times the element was saved.</summary>
    public int Saves { get; private set; }

    /// <summary>How many times the element was opened (reverted).</summary>
    public int Opens { get; private set; }

    /// <summary>Whether the element was deleted.</summary>
    public bool Deleted { get; private set; }

    /// <inheritdoc/>
    public event PreviewDeletedEventHandler? PreviewDeleted;

    /// <inheritdoc/>
    event DeletedEventHandler? IElement.Deleted { add => _deleted += value; remove => _deleted -= value; }

    private DeletedEventHandler? _deleted;

    /// <inheritdoc/>
    public event PreviewObjectSavedEventHandler? PreviewObjectSaved;

    /// <inheritdoc/>
    public event ObjectSavedEventHandler? ObjectSaved;

    /// <inheritdoc/>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <inheritdoc/>
    public IElement Copy(string? newName = null) => new TestElement(newName ?? Name, ParentCollection);

    /// <inheritdoc/>
    public IElement CopyFromExternal(string itemName, string fullFileName) => new TestElement(itemName, ParentCollection);

    /// <inheritdoc/>
    public void Delete()
    {
        bool cancel = false;
        PreviewDeleted?.Invoke(this, ref cancel);
        if (cancel) return;
        Deleted = true;
        ParentCollection.Remove(this);
        _deleted?.Invoke(this);
    }

    /// <inheritdoc/>
    public void Open()
    {
        Opens++;
        IsDirty = false;
    }

    /// <inheritdoc/>
    public void Save()
    {
        bool cancel = false;
        PreviewObjectSaved?.Invoke(this, ref cancel);
        Saves++;
        IsDirty = false;
        ObjectSaved?.Invoke(this);
    }

    private void Changed(string property) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
}
