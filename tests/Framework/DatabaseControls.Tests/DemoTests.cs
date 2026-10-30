using Avalonia.Controls;
using DatabaseManager;
using Themes;

namespace DatabaseControls.Tests;

/// <summary>
/// What upstream's DatabaseControls.Demo exercises, through its own ported window: opening SQLite, CSV and
/// dBASE files, choosing a table, the status bar, and switching themes.
/// </summary>
[TestClass]
public sealed class DemoTests
{
    private static string NewFolder()
    {
        string folder = Path.Combine(Path.GetTempPath(), "xgf-dbc-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        return folder;
    }

    private static DatabaseControls.Demo.MainWindow ShowDemo()
    {
        var demo = new DatabaseControls.Demo.MainWindow();
        demo.Show();
        TestSupport.Settle();
        return demo;
    }

    [TestMethod]
    public void SQLiteFile_ListsItsTables_AndShowsTheChosenOne()
    {
        HeadlessSession.Run(() =>
        {
            string path = Path.Combine(NewFolder(), "two-tables.sqlite");
            SQLiteManager.CreateSqLiteFile(path);
            var db = new SQLiteManager(path);
            db.SaveDataTable(TestSupport.SampleTable(20, "Sample"));
            db.SaveDataTable(TestSupport.SampleTable(7, "Other"));
            db.Close();

            var demo = ShowDemo();
            demo.LoadSqliteFile(path);
            TestSupport.Settle();

            Assert.IsTrue(demo.TableComboBox.IsEnabled);
            CollectionAssert.AreEquivalent(new[] { "Sample", "Other" }, demo.TableComboBox.Items.Cast<string>().ToArray());
            string first = (string)demo.TableComboBox.Items[0]!;
            Assert.AreEqual(first, demo.TestViewer.DataView.TableName, "the first table opens");
            Assert.AreEqual($"Loaded: {first}", demo.StatusText.Text);

            demo.TableComboBox.SelectedItem = "Other";
            TestSupport.Settle();
            Assert.AreEqual("Other", demo.TestViewer.DataView.TableName);
            Assert.AreEqual("Rows: 7 | Columns: 3", demo.RowCountText.Text);
            Assert.AreEqual(7, demo.TestViewer.VisibleRowCount());

            var view = demo.TestViewer.DataView;
            demo.Close();
            Assert.IsFalse(view.ParentDatabase.DataBaseOpen, "closing the demo closes the database");
        });
    }

    [TestMethod]
    public void CsvAndDbfFiles_OpenAsTheirOnlyTable()
    {
        HeadlessSession.Run(() =>
        {
            var demo = ShowDemo();
            demo.LoadCsvFile(Path.Combine(AppContext.BaseDirectory, "TestData", "synthetic-gallery-sample.csv"));
            TestSupport.Settle();
            Assert.IsFalse(demo.TableComboBox.IsEnabled);
            Assert.AreEqual("synthetic-gallery-sample", demo.TestViewer.DataView.TableName);
            Assert.AreEqual("Rows: 12 | Columns: 5", demo.RowCountText.Text);
            CollectionAssert.AreEqual(new[] { "Sample ID", "Category", "Value", "Units", "Source" }, demo.TestViewer.HeaderTexts());
            Assert.AreEqual("DEMO-003", demo.TestViewer.CellText(2, 0));

            // A dBASE file, written by DatabaseManager from the CSV's table.
            string dbf = Path.Combine(NewFolder(), "sample.dbf");
            demo.TestViewer.DataView.ExportToDbf(dbf);
            demo.LoadDbfFile(dbf);
            TestSupport.Settle();
            Assert.AreEqual("sample", demo.TableComboBox.Items[0]);
            Assert.AreEqual(12, demo.TestViewer.DataView.NumberOfRows);
            Assert.AreEqual("DEMO-003", demo.TestViewer.CellText(2, 0));
            demo.Close();
        });
    }

    [TestMethod]
    public void ThemeButtons_SwitchTheTheme_AndTheViewerFollows()
    {
        HeadlessSession.Run(() =>
        {
            var demo = ShowDemo();
            demo.LoadCsvFile(Path.Combine(AppContext.BaseDirectory, "TestData", "synthetic-gallery-sample.csv"));
            TestSupport.Settle();

            demo.DarkThemeRadio.IsChecked = true;
            TestSupport.Settle();
            Assert.AreEqual(Theme.Dark, ThemeService.Instance.CurrentTheme);
            var rowBackground = demo.TestViewer.RowColorGrid.Children.OfType<Avalonia.Controls.Shapes.Rectangle>().First().Fill;
            Assert.AreEqual(demo.FindResource("DataGrid.Row.Background"), rowBackground, "the rows take the dark theme's colours");

            demo.LightThemeRadio.IsChecked = true;
            TestSupport.Settle();
            Assert.AreEqual(Theme.Light, ThemeService.Instance.CurrentTheme);
            demo.Close();
        });
    }
}
