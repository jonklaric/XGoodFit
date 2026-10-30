#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using FrameworkInterfaces;
using OxyPlot.Wpf;
using OxyPlot.Wpf.Serialization;
using RMC.BestFit.UI;

namespace BestFitFileCompat;

/// <summary>
/// A snapshot of an open project's in-memory state: the project's and every element's public
/// properties, with plots as their saved XML and models as their <c>ToXElement()</c> XML.
/// </summary>
/// <remarks>
/// XGoodFit addition (docs/migration-plan.md, Phase 6), shared with tools/BestFitFileCompat.Wpf. It serves
/// two checks: the state after opening an example against the state after saving it and opening the saved
/// file again (<see cref="Differences"/>), and the port's state after opening an example against upstream's
/// (<see cref="GroupHashes"/>, one hash per element).
/// <para>
/// Values: simple values as invariant text (doubles round-trip; local dates as UTC, so the time zone
/// doesn't show), plots as a hash of <see cref="PlotSerializer.ToXElement"/>, objects with a public
/// <c>ToXElement()</c> as a hash of that XML and, for an element's own models, their properties one level
/// down too (an analysis's MCMC results and fit statistics aren't in its XML), results that save
/// themselves with a static <c>ToByteArray</c> as a hash of those bytes, other elements by name, and
/// lists of simple values in full. Other objects of RMC-BestFit's UI layer (such as an analysis's
/// <c>BayesianPlots</c>) are walked one level down; anything else is recorded by type. Images (the element
/// icons) are skipped: they come from the running application. Text naming the folder the project was
/// opened from shows it as <c>{dir}</c>, with '/' separators.
/// </para>
/// </remarks>
public sealed class StateSnapshot
{
    private const int MaxDepth = 2;
    private const int MaxListItems = 64;

    private readonly string? _openedFrom;

    private StateSnapshot(string? openedFrom) => _openedFrom = openedFrom;

    /// <summary>Every value, by property path ("collection/element/property/...").</summary>
    public SortedDictionary<string, string> Values { get; } = new(StringComparer.Ordinal);

    /// <summary>The group each value belongs to: "Project", a collection, or an element of one.</summary>
    public Dictionary<string, string> Groups { get; } = new(StringComparer.Ordinal);

    /// <summary>Takes a snapshot of a project and its elements, opened from a file in <paramref name="openedFrom"/>.</summary>
    public static StateSnapshot Take(BestFitProject project, string? openedFrom)
    {
        var snapshot = new StateSnapshot(openedFrom);
        snapshot.AddProperties("Project", "Project", project, 0);
        int collectionIndex = 0;
        foreach (IElementCollection collection in project.ElementCollections ?? Enumerable.Empty<IElementCollection>())
            snapshot.AddCollection($"{collectionIndex++:D2} {collection.Name}", collection);
        return snapshot;
    }

    /// <summary>The paths whose values differ between two snapshots, or that only one has.</summary>
    public List<string> Differences(StateSnapshot other) =>
        Values.Keys.Union(other.Values.Keys, StringComparer.Ordinal)
            .Where(key => !Values.TryGetValue(key, out string? a) || !other.Values.TryGetValue(key, out string? b) || a != b)
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToList();

    /// <summary>
    /// A hash of each group's values, and a second one with every number left out: two runs whose
    /// computed statistics differ only in the last bits, as different maths libraries round them, have
    /// the same second hash.
    /// </summary>
    public SortedDictionary<string, (string Full, string WithoutNumbers)> GroupHashes()
    {
        var hashes = new SortedDictionary<string, (string, string)>(StringComparer.Ordinal);
        foreach (IGrouping<string, KeyValuePair<string, string>> group in Values.GroupBy(entry => Groups[entry.Key]))
        {
            var full = new StringBuilder();
            var withoutNumbers = new StringBuilder();
            foreach (KeyValuePair<string, string> entry in group)
            {
                full.Append(entry.Key).Append('\t').Append(entry.Value).Append('\n');
                bool isNumber = double.TryParse(entry.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out _);
                withoutNumbers.Append(entry.Key).Append('\t').Append(isNumber ? "#" : entry.Value).Append('\n');
            }
            hashes[group.Key] = (FileDescription.Sha(full.ToString()), FileDescription.Sha(withoutNumbers.ToString()));
        }
        return hashes;
    }

    private void AddCollection(string path, IElementCollection collection)
    {
        Add(path, path + "/Count", collection.Count.ToString(CultureInfo.InvariantCulture));
        for (int i = 0; i < collection.Count; i++)
        {
            IElement element = collection[i];
            string elementPath = $"{path}/{i:D2} {element.GetType().Name} '{element.Name}'";
            AddProperties(elementPath, elementPath, element, 0);
        }
        int childIndex = 0;
        foreach (IElementCollection child in collection.ElementCollections ?? Enumerable.Empty<IElementCollection>())
            AddCollection($"{path}/{childIndex++:D2} {child.Name}", child);
    }

    private void Add(string group, string path, string value)
    {
        Values[path] = value;
        Groups[path] = group;
    }

    private void AddProperties(string group, string path, object owner, int depth)
    {
        foreach (PropertyInfo property in owner.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0 && !IsSkipped(p))
            .OrderBy(p => p.Name, StringComparer.Ordinal))
        {
            string propertyPath = path + "/" + property.Name;
            object? value;
            try
            {
                value = property.GetValue(owner);
            }
            catch (TargetInvocationException ex)
            {
                Add(group, propertyPath, "throws " + (ex.InnerException ?? ex).GetType().Name);
                continue;
            }
            if (value != null && depth < MaxDepth && IsWalked(value.GetType()))
            {
                AddProperties(group, propertyPath, value, depth + 1);
                continue;
            }
            Add(group, propertyPath, Format(value));

            // A model saved as XML also holds state its XML leaves out (an analysis's MCMC results and
            // fit statistics), so its own properties are recorded too, one level down.
            if (value != null && depth == 0 && FindToXElement(value.GetType()) != null && value is not Plot)
                AddProperties(group, propertyPath, value, MaxDepth);
        }
    }

    // Images come from the running application; parents lead back up the tree.
    private static bool IsSkipped(PropertyInfo property) =>
        property.Name is "ParentCollection" or "ParentProject" or "ElementCollections" or "ProjectImage" or "ElementImage" ||
        property.PropertyType.Name is "ImageSource" or "IImage";

    // Objects of the UI layer that hold more state, such as an analysis's BayesianController.
    private static bool IsWalked(Type type) =>
        type.Assembly == typeof(BestFitProject).Assembly && !typeof(IElement).IsAssignableFrom(type) &&
        !typeof(IElementCollection).IsAssignableFrom(type) && !type.IsEnum && FindToXElement(type) == null;

    private string Format(object? value)
    {
        switch (value)
        {
            case null:
                return "null";
            case string text:
                if (!string.IsNullOrEmpty(_openedFrom) && text.Contains(_openedFrom, StringComparison.Ordinal))
                    text = text.Replace(_openedFrom, "{dir}", StringComparison.Ordinal).Replace('\\', '/');
                return "\"" + FileDescription.Escape(text) + "\"";
            case double number:
                return number.ToString("R", CultureInfo.InvariantCulture);
            case float number:
                return number.ToString("R", CultureInfo.InvariantCulture);
            case DateTime date:
                // Upstream reads its UTC dates into local time; as UTC again they don't depend on the time zone.
                return (date.Kind == DateTimeKind.Local ? date.ToUniversalTime() : date).ToString("o", CultureInfo.InvariantCulture) + " " + date.Kind;
            case Plot plot:
                return "plot " + FileDescription.Sha(PlotSerializer.ToXElement(plot).ToString(SaveOptions.DisableFormatting));
            case IElement element:
                return "element " + element.GetType().Name + " '" + element.Name + "'";
            case IElementCollection collection:
                return "collection '" + collection.Name + "'";
            case XElement xml:
                return "xml " + FileDescription.Sha(xml.ToString(SaveOptions.DisableFormatting));
            case IFormattable formattable when value.GetType().IsPrimitive || value.GetType().IsEnum || value is decimal or TimeSpan or Guid:
                return formattable.ToString(null, CultureInfo.InvariantCulture);
            case bool flag:
                return flag ? "True" : "False";
            case char character:
                return "'" + FileDescription.Escape(character.ToString()) + "'";
        }

        // Numerics' results classes (MCMC and uncertainty-analysis results) save themselves as bytes.
        MethodInfo? toByteArray = value.GetType().GetMethod("ToByteArray", BindingFlags.Public | BindingFlags.Static, null, new[] { value.GetType() }, null);
        if (toByteArray != null && toByteArray.ReturnType == typeof(byte[]))
        {
            try
            {
                var bytes = (byte[]?)toByteArray.Invoke(null, new[] { value });
                return "bytes " + (bytes == null ? "null" : FileDescription.Sha(bytes));
            }
            catch (TargetInvocationException ex)
            {
                return "ToByteArray throws " + (ex.InnerException ?? ex).GetType().Name;
            }
        }

        MethodInfo? toXElement = FindToXElement(value.GetType());
        if (toXElement != null)
        {
            try
            {
                var xml = (XElement?)toXElement.Invoke(value, null);
                return "xml " + (xml == null ? "null" : FileDescription.Sha(xml.ToString(SaveOptions.DisableFormatting)));
            }
            catch (TargetInvocationException ex)
            {
                return "ToXElement throws " + (ex.InnerException ?? ex).GetType().Name;
            }
        }

        if (value is IEnumerable items)
        {
            var list = items.Cast<object?>().Take(MaxListItems + 1).ToList();
            if (list.All(item => item == null || item is string || item.GetType().IsPrimitive || item.GetType().IsEnum))
                return "[" + string.Join(", ", list.Take(MaxListItems).Select(Format)) + (list.Count > MaxListItems ? ", ..." : "") + "]";
            return value.GetType().Name + " of " + items.Cast<object?>().Count().ToString(CultureInfo.InvariantCulture);
        }
        return value.GetType().FullName ?? value.GetType().Name;
    }

    private static MethodInfo? FindToXElement(Type type)
    {
        MethodInfo? method = type.GetMethod("ToXElement", BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null);
        return method != null && method.ReturnType == typeof(XElement) ? method : null;
    }
}
