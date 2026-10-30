// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PlotViewBase.Events.cs" company="OxyPlot">
//   Copyright (c) 2020 OxyPlot contributors
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf
{
    using System;
    using Avalonia.Input;
    using Avalonia.Threading;

    /// <summary>
    /// Base class for WPF PlotView implementations.
    /// </summary>
    public abstract partial class PlotViewBase
    {
        /// <summary>
        /// Called before the <c>KeyDown</c> event occurs.
        /// </summary>
        /// <param name="e">The data for the event.</param>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Handled)
            {
                return;
            }

            var args = new OxyKeyEventArgs { ModifierKeys = Keyboard.GetModifierKeys(e.KeyModifiers), Key = e.Key.Convert() };
            e.Handled = this.ActualController.HandleKeyDown(this, args);
        }

        // XGoodFit port: WPF's OnManipulationStarted, OnManipulationDelta and OnManipulationCompleted
        // aren't ported: Avalonia has no manipulation events, and reports touch as pointer events,
        // which reach the controller through the mouse handlers below.

        /// <summary>
        /// Pending wheel delta accumulated since the last coalesced flush. See
        /// <see cref="FlushAccumulatedWheel"/>.
        /// </summary>
        private int pendingWheelDelta;

        /// <summary>
        /// Cached position + modifier-keys snapshot from the most recent wheel event in the
        /// accumulation window. Used as the position for the synthesised coalesced wheel
        /// event — taking the latest position keeps zoom-at-cursor accurate.
        /// </summary>
        private OxyMouseWheelEventArgs latestWheelArgs;

        /// <summary>
        /// True when a coalesced-wheel flush is pending in the dispatcher queue. Subsequent
        /// wheel events accumulate into <see cref="pendingWheelDelta"/> instead of dispatching
        /// their own work.
        /// </summary>
        private bool wheelFlushScheduled;

        /// <summary>
        /// Called before the <c>MouseWheel</c> event occurs to provide handling for the event in a derived class without attaching a delegate.
        /// </summary>
        /// <param name="e">A <see cref="PointerWheelEventArgs" /> that contains the event data.</param>
        /// <remarks>
        /// Wheel events are coalesced via <see cref="FlushAccumulatedWheel"/>: rapid wheel
        /// scrolls (high-Hz mice, trackpad inertia) produce N independent events, each of which
        /// would otherwise trigger a synchronous <c>Model.Update</c> + render-queue dispatch.
        /// On a multi-LineSeries large-data plot these stack up to multi-frame latency before
        /// the first paint catches up. The coalescer accumulates deltas and dispatches one
        /// combined wheel event per dispatcher tick, summed at the latest pointer position.
        /// XGoodFit port: Avalonia's wheel event, whose delta is converted to WPF's units (120 per notch).
        /// </remarks>
        protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
        {
#if DEBUG
            // Open a new wheel-event scope at the very top of the WPF event handler. Every traced
            // method below this point logs against this wheel sequence number and timestamp,
            // producing a single coherent block per wheel tick.
            OxyPlot.PlotDiagnostics.BeginWheel();
            try
            {
                using (OxyPlot.PlotDiagnostics.Trace("PlotViewBase.OnMouseWheel",
                    $"delta={e.Delta}"))
                {
#endif
                    base.OnPointerWheelChanged(e);
                    if (e.Handled || !this.IsMouseWheelEnabled)
                    {
                        return;
                    }

                    // Mark handled now to prevent the ScrollViewer (or any ancestor) from
                    // also processing this event. The actual zoom dispatch is deferred to
                    // the coalesced flush below.
                    e.Handled = true;

                    // Accumulate delta and capture the latest pointer position + modifiers.
                    // OxyMouseWheelEventArgs is small and intentionally allocated each event
                    // so the snapshot reflects the wheel-tick state exactly.
                    this.latestWheelArgs = e.ToMouseWheelEventArgs(this);
                    this.pendingWheelDelta += this.latestWheelArgs.Delta;

                    if (!this.wheelFlushScheduled)
                    {
                        this.wheelFlushScheduled = true;
                        // Render priority matches the deferred-render dispatcher and lands
                        // before the next composition tick — so the user sees the combined
                        // zoom on the very next frame.
                        Dispatcher.UIThread.Post(
                            this.FlushAccumulatedWheel,
                            DispatcherPriority.Render);
                    }
#if DEBUG
                }
            }
            finally
            {
                // Close the wheel scope so subsequent unrelated invalidations (mouse-move, pan,
                // render) don't emit trace lines attributed to this wheel sequence number.
                OxyPlot.PlotDiagnostics.EndWheel();
            }
#endif
        }

        /// <summary>
        /// Dispatches the accumulated wheel delta as a single
        /// <see cref="OxyMouseWheelEventArgs"/> through the controller. Resets the pending
        /// state so the next wheel event starts a fresh accumulation window.
        /// </summary>
        private void FlushAccumulatedWheel()
        {
            int delta = this.pendingWheelDelta;
            var args = this.latestWheelArgs;
            this.pendingWheelDelta = 0;
            this.latestWheelArgs = null;
            this.wheelFlushScheduled = false;

            if (delta == 0 || args == null)
            {
                return;
            }

            // Synthesise a combined wheel event at the latest pointer position. Position +
            // ModifierKeys are taken from the most recent event; Delta is the sum across
            // the window so cumulative zoom matches what the user saw.
            args.Delta = delta;
            this.ActualController.HandleMouseWheel(this, args);
        }

        /// <summary>
        /// Invoked when an unhandled MouseDown attached event reaches an element in its route that is derived from this class. Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="e">The <see cref="PointerPressedEventArgs" /> that contains the event data. This event data reports details about the mouse button that was pressed and the handled state.</param>
        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);
            if (e.Handled)
            {
                return;
            }

            this.Focus();
            e.Pointer.Capture(this);

            // store the mouse down point, check it when mouse button is released to determine if the context menu should be shown
            this.mouseDownPoint = e.GetPosition(this).ToScreenPoint();

            e.Handled = this.ActualController.HandleMouseDown(this, e.ToMouseDownEventArgs(this));
        }

        /// <summary>
        /// Invoked when an unhandled MouseMove attached event reaches an element in its route that is derived from this class. Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="e">The <see cref="PointerEventArgs" /> that contains the event data.</param>
        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);
            if (e.Handled)
            {
                return;
            }

            e.Handled = this.ActualController.HandleMouseMove(this, e.ToMouseEventArgs(this));
        }

        /// <summary>
        /// Invoked when an unhandled MouseUp routed event reaches an element in its route that is derived from this class. Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="e">The <see cref="PointerReleasedEventArgs" /> that contains the event data. The event data reports that the mouse button was released.</param>
        /// <remarks>XGoodFit port: Avalonia's context menu opens with <c>Open</c> and closes with
        /// <c>Close</c>, and shows with <c>IsVisible</c>, where WPF's had IsOpen and Visibility.</remarks>
        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);
            if (e.Handled)
            {
                return;
            }

            e.Pointer.Capture(null);

            e.Handled = this.ActualController.HandleMouseUp(this, e.ToMouseReleasedEventArgs(this));

            // Open the context menu
            var p = e.GetPosition(this).ToScreenPoint();
            var d = p.DistanceTo(this.mouseDownPoint);

            if (this.ContextMenu != null)
            {
                if (Math.Abs(d) < 1e-8 && e.InitialPressMouseButton == MouseButton.Right)
                {
                    // TODO: why is the data context not passed to the context menu??
                    this.ContextMenu.DataContext = this.DataContext;
                    this.ContextMenu.PlacementTarget = this;
                    this.ContextMenu.IsVisible = true;
                    this.ContextMenu.Open(this);
                }
                else
                {
                    this.ContextMenu.IsVisible = false;
                    this.ContextMenu.Close();
                }
            }
        }

        /// <summary>
        /// Invoked when an unhandled <c>MouseEnter</c> attached event is raised on this element. Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="e">The <see cref="PointerEventArgs" /> that contains the event data.</param>
        protected override void OnPointerEntered(PointerEventArgs e)
        {
            base.OnPointerEntered(e);
            if (e.Handled)
            {
                return;
            }

            e.Handled = this.ActualController.HandleMouseEnter(this, e.ToMouseEventArgs(this));
        }

        /// <summary>
        /// Invoked when an unhandled <c>MouseLeave</c> attached event is raised on this element. Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="e">The <see cref="PointerEventArgs" /> that contains the event data.</param>
        protected override void OnPointerExited(PointerEventArgs e)
        {
            base.OnPointerExited(e);
            if (e.Handled)
            {
                return;
            }

            e.Handled = this.ActualController.HandleMouseLeave(this, e.ToMouseEventArgs(this));
        }
    }
}
