using System.Collections.ObjectModel;
using GenericControls;

namespace ThemeGallery;

/// <summary>The rows the gallery's data grids show.</summary>
internal static class DataGridSamples
{
    /// <summary>The copy-paste grid's rows: upstream's GenericControls.Demo sample data.</summary>
    public static ObservableCollection<SampleDataItem> CreateSampleDataItems() =>
    [
        new SampleDataItem { Name = "Item 1", Value = 100, Category = "Alpha" },
        new SampleDataItem { Name = "Item 2", Value = 250, Category = "Beta" },
        new SampleDataItem { Name = "Item 3", Value = 175, Category = "Alpha" },
        new SampleDataItem { Name = "Item 4", Value = 320, Category = "Gamma" },
        new SampleDataItem { Name = "Item 5", Value = 95, Category = "Beta" },
    ];

    /// <summary>The validation grid's rows, two of which break a rule.</summary>
    public static ObservableCollection<object> CreateRuleRows(ValidationDataGrid grid)
    {
        var rows = new ObservableCollection<object>();
        foreach ((string name, double value) in new[] { ("Low", 10d), ("Mid", 25d), ("Mid", 20d), ("High", 40d) })
        {
            rows.Add(new SampleRuleRow(rows, grid) { Name = name, Value = value });
        }
        return rows;
    }
}

/// <summary>A row of the copy-paste grid (upstream's GenericControls.Demo SampleDataItem).</summary>
public sealed class SampleDataItem
{
    /// <summary>The item's name.</summary>
    public string Name { get; set; } = "";

    /// <summary>The item's value.</summary>
    public double Value { get; set; }

    /// <summary>The item's category.</summary>
    public string Category { get; set; } = "";
}

/// <summary>A row of the validation grid: unique names, and positive values that increase.</summary>
public sealed class SampleRuleRow : DataGridRowItem
{
    private string _name = "";
    private double _value;

    /// <summary>Creates a row for the grid's rows.</summary>
    public SampleRuleRow(ObservableCollection<object> list, ValidationDataGrid grid) : base(list, grid)
    {
    }

    /// <summary>Creates a row the grid adds; the grid gives it its list.</summary>
    public SampleRuleRow() : base(null)
    {
    }

    /// <summary>The row's name.</summary>
    public string Name
    {
        get => _name;
        set
        {
            _name = value;
            NotifyPropertyChanged();
        }
    }

    /// <summary>The row's value.</summary>
    public double Value
    {
        get => _value;
        set
        {
            _value = value;
            NotifyPropertyChanged();
        }
    }

    /// <inheritdoc/>
    public override void AddValidationRules()
    {
        AddRule(nameof(Name), () => UniqueRule(nameof(Name), "Names must be unique."), "Names must be unique.");
        AddRule(nameof(Value), () => Value <= 0, "Values must be positive.");
        AddRule(nameof(Value), () => OrderRule<double, SampleRuleRow>(row => row.Value, nameof(Value)), "Values must increase down the table.");
    }

    /// <inheritdoc/>
    public override string PropertyDisplayName(string propertyName) => propertyName;

    /// <inheritdoc/>
    public override bool IsGridDisplayable(string propertyName) => propertyName != nameof(RuleMap);
}
