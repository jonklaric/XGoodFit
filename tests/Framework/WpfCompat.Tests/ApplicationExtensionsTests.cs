using Avalonia;
using Avalonia.Controls;

namespace WpfCompat.Tests;

/// <summary>
/// WpfCompat's <c>Application.MainWindow</c>: without a desktop lifetime (the headless session has none), the
/// window set last, as WPF's property holds it, so code that disables the main window while it works
/// (BestFit's Estimate buttons) can run in tests.
/// </summary>
[TestClass]
public sealed class ApplicationExtensionsTests
{
    [TestMethod]
    public void MainWindow_WithoutADesktopLifetime_IsTheWindowSetLast()
    {
        HeadlessSession.Run(() =>
        {
            Application application = Application.Current!;
            Assert.IsNull(application.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime);
            var window = new Window();
            try
            {
                application.MainWindow = window;
                Assert.AreSame(window, application.MainWindow);
                application.MainWindow = null;
                Assert.IsNull(application.MainWindow);
            }
            finally
            {
                application.MainWindow = null;
            }
        });
    }
}
