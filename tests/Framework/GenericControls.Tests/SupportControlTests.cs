using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace GenericControls.Tests;

/// <summary>
/// The supporting controls: PropertyAttributes, SeparatorWithHeader, the font selector, the insertion
/// adorner and the combo box item template selector.
/// </summary>
[TestClass]
public sealed class SupportControlTests
{
    [DisplayName("Gauge station"), System.ComponentModel.Description("A stream gauge.")]
    private sealed class Station
    {
        [DisplayName("Drainage area"), System.ComponentModel.Description("Square miles upstream of the gauge.")]
        public double Area { get; set; }
    }

    private static string?[] Texts(Visual visual) =>
        visual.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text).ToArray();

    [TestMethod]
    public void PropertyAttributes_ShowsDisplayNamesAndDescriptions()
    {
        HeadlessSession.Run(() =>
        {
            var attributes = new PropertyAttributes();
            Window window = TestInput.Show(attributes, out _);

            attributes.SetDefaultAttributes("Analysis", "No selection.");
            Assert.AreEqual(("Analysis", "No selection."), (attributes.NameTextBlock.Text, attributes.Description.Text));
            attributes.GetClassAttributes(new Station());
            Assert.AreEqual(("Gauge station", "A stream gauge."), (attributes.NameTextBlock.Text, attributes.Description.Text));
            attributes.GetPropertyAttributes(nameof(Station.Area), new Station());
            Assert.AreEqual(("Drainage area", "Square miles upstream of the gauge."), (attributes.NameTextBlock.Text, attributes.Description.Text));
            window.Close();
        });
    }

    [TestMethod]
    public void SeparatorWithHeader_ShowsItsHeader()
    {
        HeadlessSession.Run(() =>
        {
            var separator = new SeparatorWithHeader { Header = "Numeric Properties" };
            Window window = TestInput.Show(separator, out _);
            CollectionAssert.Contains(Texts(separator), "Numeric Properties");
            window.Close();
        });
    }

    [TestMethod]
    public void FontSelector_ListsTheSystemFontsByName_AndSelectsByName()
    {
        HeadlessSession.Run(() =>
        {
            var selector = new FontSelectorControl { Title = "Font" };
            Window window = TestInput.Show(selector, out _, 500);
            ComboBox combo = selector.GetVisualDescendants().OfType<ComboBox>().Single();
            string[] names = combo.Items.Cast<FontFamily>().Select(f => f.Name).ToArray();

            Assert.AreNotEqual(0, names.Length);
            CollectionAssert.AreEqual(names.OrderBy(n => n, StringComparer.CurrentCulture).ToArray(), names, "sorted by name");
            combo.SelectedIndex = names.Length - 1;
            Assert.AreEqual(names[^1], selector.FontFamilyString);
            window.Close();
        });
    }

    [TestMethod]
    public void FontConverters_GiveWpfsFamilyName_WithoutTheFontCollectionsKey()
    {
        // A family Avalonia caches after the platform substituted a font for it (fontconfig on Linux
        // does this for "Segoe UI") carries the system collection's key. Its WPF text, which the font
        // selector stores and plots save, is the plain name.
        var keyed = new FontFamily("fonts:SystemFonts#Segoe UI");
        Assert.AreEqual("fonts:SystemFonts#Segoe UI", keyed.ToString(), "what ToString() would have given");
        Assert.AreEqual("Segoe UI", new FontToFontFamilyConverter().ConvertBack(keyed, typeof(string), null!, System.Globalization.CultureInfo.InvariantCulture));
        Assert.AreEqual("Segoe UI", new FontFamilyToFontStringConverter().Convert(keyed, typeof(string), null!, System.Globalization.CultureInfo.InvariantCulture));
        Assert.AreEqual("Arial, Verdana", new FontFamilyToFontStringConverter().Convert(new FontFamily("Arial, Verdana"), typeof(string), null!, System.Globalization.CultureInfo.InvariantCulture),
            "a list of families is written as it was made, as in WPF");
    }

    [TestMethod]
    public void InsertionAdorner_DrawsALineAcrossTheAdornedElement_OnTheHalfItIsIn()
    {
        HeadlessSession.Run(() =>
        {
            var target = new Border { Width = 120, Height = 30 };
            Window window = TestInput.Show(target, out _);
            AdornerLayer layer = AdornerLayer.GetAdornerLayer(target)!;

            var adorner = new InsertionAdorner(isSeparatorHorizontal: true, isInFirstHalf: false, target, layer);
            Dispatcher.UIThread.RunJobs();
            Assert.IsTrue(layer.Children.Contains(adorner));
            Assert.AreSame(target, AdornerLayer.GetAdornedElement(adorner));
            Assert.IsFalse(adorner.IsHitTestVisible);

            var drawing = new DrawingGroup();
            using (DrawingContext context = drawing.Open())
            {
                adorner.Render(context);
            }
            Rect line = drawing.Children.OfType<GeometryDrawing>().First().Geometry!.Bounds;
            Assert.AreEqual(new Rect(0, 30, 120, 0), line, "along the bottom: the second half");
            Assert.AreEqual(2, drawing.Children.OfType<DrawingGroup>().Count(), "with a triangle at each end");

            adorner.Detach();
            Assert.IsFalse(layer.Children.Contains(adorner));
            window.Close();
        });
    }

    // A data template showing its item after a prefix.
    private static DataTemplate Template(string prefix) => new()
    {
        Content = (Func<IServiceProvider?, object?>)(_ =>
        {
            var text = new TextBlock();
            text.Bind(TextBlock.TextProperty, new Binding { StringFormat = prefix + "{0}" });
            return new TemplateResult<Control>(text, new NameScope());
        }),
    };

    [TestMethod]
    public void ComboBoxItemTemplateSelector_ShowsTheDropDownTemplateInTheList_AndTheSelectedOneInTheBox()
    {
        HeadlessSession.Run(() =>
        {
            var selector = new ComboBoxItemTemplateSelector { DropDownTemplate = Template("in list: "), SelectedTemplate = Template("chosen: ") };
            var combo = new ComboBox { ItemsSource = new[] { "a", "b" }, SelectedIndex = 0, ItemTemplate = selector, Width = 200 };
            Window window = TestInput.Show(combo, out _);
            CollectionAssert.Contains(Texts(combo), "chosen: a");

            combo.IsDropDownOpen = true;
            Dispatcher.UIThread.RunJobs();
            string?[] listed = combo.GetRealizedContainers().SelectMany(c => Texts(c)).ToArray();
            CollectionAssert.AreEqual(new[] { "in list: a", "in list: b" }, listed);
            combo.IsDropDownOpen = false;
            window.Close();
        });
    }
}
