using System.Collections.Generic;
using OxyPlot;
using Xunit;

namespace OxyPlotControls.Tests.Controller;

/// <summary>
/// Tests for the Phase 1 behavior where <see cref="ControllerBase.HandleMouseMove"/> skips
/// MouseHoverManipulators (most importantly the tracker) while any MouseDownManipulator is
/// active. This eliminates the per-mouse-move tracker scan that would otherwise saturate
/// the UI thread during pan / zoom-rectangle drag on large-data plots.
/// </summary>
public class SuppressHoverDuringDragTests
{
    /// <summary>
    /// Hover manipulators must fire on MouseMove when NO mouse-down manipulator is active
    /// (the normal hover-to-track case). Default <c>EnableHoverDuringDrag=false</c> does
    /// not affect the normal hover path.
    /// </summary>
    [Fact]
    public void HoverManipulator_Fires_WhenNoDragActive()
    {
        var ctrl = new TestableController();
        var view = new StubView { ActualController = ctrl };

        var hover = new CountingMouseManipulator(view);
        ctrl.MouseHoverManipulatorsAccessor.Add(hover);

        ctrl.HandleMouseMove(view, NewMouseArgs(10, 10));
        ctrl.HandleMouseMove(view, NewMouseArgs(11, 10));

        Assert.Equal(2, hover.DeltaCount);
    }

    /// <summary>
    /// When a MouseDownManipulator is active (pan or zoom-rectangle drag in progress), the
    /// hover manipulator's <c>Delta</c> must NOT be invoked on subsequent MouseMove events.
    /// This is the behavior change of item 1.5 — tracker work is eliminated mid-drag.
    /// </summary>
    [Fact]
    public void HoverManipulator_DoesNotFire_WhileDragIsActive()
    {
        var ctrl = new TestableController();
        var view = new StubView { ActualController = ctrl };

        var hover = new CountingMouseManipulator(view);
        var drag = new CountingMouseManipulator(view);
        ctrl.MouseHoverManipulatorsAccessor.Add(hover);
        ctrl.MouseDownManipulatorsAccessor.Add(drag); // simulates active pan

        ctrl.HandleMouseMove(view, NewMouseArgs(10, 10));
        ctrl.HandleMouseMove(view, NewMouseArgs(11, 10));

        Assert.Equal(2, drag.DeltaCount);  // drag manipulator keeps firing
        Assert.Equal(0, hover.DeltaCount); // hover is suppressed
    }

    /// <summary>
    /// Setting <c>EnableHoverDuringDrag=true</c> restores the legacy behavior where hover
    /// manipulators keep firing even while a MouseDownManipulator is active. This is the
    /// opt-in escape valve for consumers who rely on mid-drag hover.
    /// </summary>
    [Fact]
    public void HoverManipulator_StillFires_WhenEnableHoverDuringDragIsTrue()
    {
        var ctrl = new TestableController { EnableHoverDuringDrag = true };
        var view = new StubView { ActualController = ctrl };

        var hover = new CountingMouseManipulator(view);
        var drag = new CountingMouseManipulator(view);
        ctrl.MouseHoverManipulatorsAccessor.Add(hover);
        ctrl.MouseDownManipulatorsAccessor.Add(drag);

        ctrl.HandleMouseMove(view, NewMouseArgs(10, 10));

        Assert.Equal(1, drag.DeltaCount);
        Assert.Equal(1, hover.DeltaCount);
    }

    /// <summary>
    /// Builds a fresh <see cref="OxyMouseEventArgs"/> at the given screen point.
    /// </summary>
    private static OxyMouseEventArgs NewMouseArgs(double x, double y) =>
        new OxyMouseEventArgs { Position = new ScreenPoint(x, y) };

    /// <summary>
    /// Test-only <see cref="ManipulatorBase{T}"/> that counts Delta invocations.
    /// </summary>
    private sealed class CountingMouseManipulator : ManipulatorBase<OxyMouseEventArgs>
    {
        public CountingMouseManipulator(IView view) : base(view) { }

        public int DeltaCount { get; private set; }

        public override void Delta(OxyMouseEventArgs e)
        {
            this.DeltaCount++;
            base.Delta(e);
        }
    }

    /// <summary>
    /// Subclass of <see cref="ControllerBase"/> that exposes the protected manipulator lists
    /// so tests can inject the test-only manipulators directly (bypassing the binding system).
    /// </summary>
    private sealed class TestableController : ControllerBase
    {
        public IList<ManipulatorBase<OxyMouseEventArgs>> MouseDownManipulatorsAccessor => this.MouseDownManipulators;

        public IList<ManipulatorBase<OxyMouseEventArgs>> MouseHoverManipulatorsAccessor => this.MouseHoverManipulators;
    }

    /// <summary>
    /// Minimal <see cref="IView"/> stub for ControllerBase.HandleMouseMove to run against.
    /// </summary>
    private sealed class StubView : IView
    {
        public IController? ActualController { get; set; }
        public Model? ActualModel => null;
        public OxyRect ClientArea => new OxyRect(0, 0, 100, 100);
        public void HideTracker() { }
        public void HideZoomRectangle() { }
        public void SetClipboardText(string text) { }
        public void SetCursorType(CursorType cursorType) { }
        public void ShowTracker(TrackerHitResult trackerHitResult) { }
        public void ShowZoomRectangle(OxyRect rectangle) { }
    }
}
