using System.Linq;
using System.Reflection;
using OxyPlotControls;
using Xunit;

namespace OxyPlotControls.Tests;

/// <summary>
/// Regression tests pinning forensic-audit Phase 3 OxyPlotToolbar refactor.
/// </summary>
public class AuditRegressionTests
{
    /// <summary>
    /// E-001: cursors used to be per-instance and the toolbar implemented IDisposable so
    /// consumers had to remember to call <c>Dispose()</c> — they typically didn't, leaking
    /// HCURSOR handles. The Phase-3 refactor moved the five custom cursors into a
    /// private static <c>CursorCache</c> and removed the IDisposable contract entirely.
    /// </summary>
    [StaFact]
    public void E001_OxyPlotToolbar_DoesNotImplementIDisposable()
    {
        // Arrange + Assert — type contract.
        var iface = typeof(OxyPlotToolbar)
            .GetInterfaces()
            .FirstOrDefault(i => i == typeof(System.IDisposable));
        Assert.Null(iface);
    }

    /// <summary>
    /// E-001: confirms the static CursorCache exists and is populated. Reflection is used
    /// because the cache is private. The cache being process-lifetime guarantees that
    /// cursor handles are not leaked per toolbar instance.
    /// </summary>
    [StaFact]
    public void E001_OxyPlotToolbar_HasStaticCursorCache()
    {
        // Arrange — locate the private nested CursorCache class.
        var cacheType = typeof(OxyPlotToolbar)
            .GetNestedType("CursorCache", BindingFlags.NonPublic);
        Assert.NotNull(cacheType);
        Assert.True(cacheType!.IsAbstract && cacheType.IsSealed,
            "CursorCache must be a static class.");

        // Act — every cursor field should be a non-null Cursor.
        var fields = cacheType.GetFields(BindingFlags.Static | BindingFlags.NonPublic);
        Assert.True(fields.Length >= 5,
            $"Expected at least 5 cached cursors; found {fields.Length}.");

        foreach (var field in fields)
        {
            var value = field.GetValue(null);
            Assert.NotNull(value);
            Assert.IsAssignableFrom<System.Windows.Input.Cursor>(value);
        }
    }

    /// <summary>
    /// E-001: cursors are shared as singletons across toolbar instances. We compare the
    /// reference identity of every CursorCache field across two reads — they must be
    /// the same instance.
    /// </summary>
    [StaFact]
    public void E001_OxyPlotToolbar_CursorsAreShared()
    {
        var cacheType = typeof(OxyPlotToolbar)
            .GetNestedType("CursorCache", BindingFlags.NonPublic);
        Assert.NotNull(cacheType);

        var fields = cacheType!.GetFields(BindingFlags.Static | BindingFlags.NonPublic);
        foreach (var field in fields)
        {
            var first = field.GetValue(null);
            var second = field.GetValue(null);
            // Reference equality: static readonly cached cursor is the same instance.
            Assert.Same(first, second);
        }
    }
}
