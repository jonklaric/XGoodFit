// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PlotViewBase.Events.cs" company="OxyPlot">
//   Copyright (c) 2020 OxyPlot contributors
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf
{
    using System;
    using System.Windows.Input;
    using System.Windows.Threading;

    /// <summary>
    /// Base class for WPF PlotView implementations.
    /// </summary>
    public abstract partial class PlotViewBase
    {
        /// <summary>
        /// Called before the <see cref="E:System.Windows.UIElement.KeyDown" /> event occurs.
        /// </summary>
        /// <param name="e">The data for the event.</param>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Handled)
            {
                return;
            }

            var args = new OxyKeyEventArgs { ModifierKeys = Keyboard.GetModifierKeys(), Key = e.Key.Convert() };
            e.Handled = this.ActualController.HandleKeyDown(this, args);
        }

        /// <summary>
        /// Called when the <see cref="E:System.Windows.UIElement.ManipulationStarted" /> event occurs.
        /// </summary>
        /// <param name="e">The data for the event.</param>
        protected override void OnManipulationStarted(ManipulationStartedEventArgs e)
        {
            base.OnManipulationStarted(e);
            if (e.Handled)
            {
                return;
            }

            e.Handled = this.ActualController.HandleTouchStarted(this, e.ToTouchEventArgs(this));
        }

        /// <summary>
        /// Called when the <see cref="E:System.Windows.UIElement.ManipulationDelta" /> event occurs.
        /// </summary>
        /// <param name="e">The data for the event.</param>
        protected override void OnManipulationDelta(ManipulationDeltaEventArgs e)
        {
            base.OnManipulationDelta(e);
            if (e.Handled)
            {
                return;
            }

            e.Handled = this.ActualController.HandleTouchDelta(this, e.ToTouchEventArgs(this));
        }

        /// <summary>
        /// Called when the <see cref="E:System.Windows.UIElement.ManipulationCompleted" /> event occurs.
        /// </summary>
        /// <param name="e">The data for the event.</param>
        protected override void OnManipulationCompleted(ManipulationCompletedEventArgs e)
        {
            base.OnManipulationCompleted(e);
            if (e.Handled)
            {
                return;
            }

            e.Handled = this.ActualController.HandleTouchCompleted(this, e.ToTouchEventArgs(this));
        }

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
        /// Called before the <see cref="E:System.Windows.UIElement.MouseWheel" /> event occurs to provide handling for the event in a derived class without attaching a delegate.
        /// </summary>
        /// <param name="e">A <see cref="T:System.Windows.Input.MouseWheelEventArgs" /> that contains the event data.</param>
        /// <remarks>
        /// Wheel events are coalesced via <see cref="FlushAccumulatedWheel"/>: rapid wheel
        /// scrolls (high-Hz mice, trackpad inertia) produce N independent events, each of which
        /// would otherwise trigger a synchronous <c>Model.Update</c> + render-queue dispatch.
        /// On a multi-LineSeries large-data plot these stack up to multi-frame latency before
        /// the first paint catches up. The coalescer accumulates deltas and dispatches one
        /// combined wheel event per dispatcher tick, summed at the latest pointer position.
        /// </remarks>
        protected override void OnMouseWheel(MouseWheelEventArgs e)
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
                    base.OnMouseWheel(e);
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
                    this.pendingWheelDelta += e.Delta;
                    this.latestWheelArgs = e.ToMouseWheelEventArgs(this);

                    if (!this.wheelFlushScheduled)
                    {
                        this.wheelFlushScheduled = true;
                        // Render priority matches the deferred-render dispatcher and lands
                        // before the next composition tick — so the user sees the combined
                        // zoom on the very next frame.
                        this.Dispatcher.BeginInvoke(
                            DispatcherPriority.Render,
                            new Action(this.FlushAccumulatedWheel));
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
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseButtonEventArgs" /> that contains the event data. This event data reports details about the mouse button that was pressed and the handled state.</param>
        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Handled)
            {
                return;
            }

            this.Focus();
            this.CaptureMouse();

            // store the mouse down point, check it when mouse button is released to determine if the context menu should be shown
            this.mouseDownPoint = e.GetPosition(this).ToScreenPoint();

            e.Handled = this.ActualController.HandleMouseDown(this, e.ToMouseDownEventArgs(this));
        }

        /// <summary>
        /// Invoked when an unhandled MouseMove attached event reaches an element in its route that is derived from this class. Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseEventArgs" /> that contains the event data.</param>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (e.Handled)
            {
                return;
            }

            e.Handled = this.ActualController.HandleMouseMove(this, e.ToMouseEventArgs(this));
        }

        /// <summary>
        /// Invoked when an unhandled MouseUp routed event reaches an element in its route that is derived from this class. Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseButtonEventArgs" /> that contains the event data. The event data reports that the mouse button was released.</param>
        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Handled)
            {
                return;
            }

            this.ReleaseMouseCapture();

            e.Handled = this.ActualController.HandleMouseUp(this, e.ToMouseReleasedEventArgs(this));

            // Open the context menu
            var p = e.GetPosition(this).ToScreenPoint();
            var d = p.DistanceTo(this.mouseDownPoint);

            if (this.ContextMenu != null)
            {
                if (Math.Abs(d) < 1e-8 && e.ChangedButton == MouseButton.Right)
                {
                    // TODO: why is the data context not passed to the context menu??
                    this.ContextMenu.DataContext = this.DataContext;
                    this.ContextMenu.PlacementTarget = this;
                    this.ContextMenu.Visibility = System.Windows.Visibility.Visible;
                    this.ContextMenu.IsOpen = true;
                }
                else
                {
                    this.ContextMenu.Visibility = System.Windows.Visibility.Collapsed;
                    this.ContextMenu.IsOpen = false;
                }
            }
        }

        /// <summary>
        /// Invoked when an unhandled <see cref="E:System.Windows.Input.Mouse.MouseEnter" /> attached event is raised on this element. Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseEventArgs" /> that contains the event data.</param>
        protected override void OnMouseEnter(MouseEventArgs e)
        {
            base.OnMouseEnter(e);
            if (e.Handled)
            {
                return;
            }

            e.Handled = this.ActualController.HandleMouseEnter(this, e.ToMouseEventArgs(this));
        }

        /// <summary>
        /// Invoked when an unhandled <see cref="E:System.Windows.Input.Mouse.MouseLeave" /> attached event is raised on this element. Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseEventArgs" /> that contains the event data.</param>
        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseLeave(e);
            if (e.Handled)
            {
                return;
            }

            e.Handled = this.ActualController.HandleMouseLeave(this, e.ToMouseEventArgs(this));
        }
    }
}
