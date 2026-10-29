// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PlotDiagnostics.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Debug-only diagnostic helpers for tracing the full wheel-zoom stack.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot
{
    /// <summary>
    /// Debug-only diagnostic helpers for tracing the full wheel-zoom stack across the OxyPlot
    /// core, the WPF view, the manipulator, the axes, the model update, and the per-series render.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Mirrored from <c>OxyPlot.Wpf.Plot.InvalidatePlotPhaseDiagnosticsEnabled</c>. Consumers that
    /// already toggle that flag automatically get the full stack trace.
    /// </para>
    /// <para>
    /// All output is via <see cref="System.Diagnostics.Debug.WriteLine(string)"/> and gated on
    /// <c>#if DEBUG</c>; zero cost in Release builds.
    /// </para>
    /// <para>
    /// Each wheel event opens a numbered scope (W#NNNN). Every traced method logs entry and exit
    /// with elapsed-since-wheel-start time and method-local elapsed time, so one log can show:
    /// </para>
    /// <list type="bullet">
    /// <item><description>The total wheel-event-to-pixels wall-clock time.</description></item>
    /// <item><description>Where in the stack the time is consumed (manipulator vs. axis vs. model update vs. render).</description></item>
    /// <item><description>Whether render-priority work runs before or after the next wheel event arrives.</description></item>
    /// </list>
    /// </remarks>
    public static class PlotDiagnostics
    {
#if DEBUG
        /// <summary>
        /// Master toggle for the full wheel-zoom stack trace. When true, every traced method on
        /// the wheel event path emits enter/exit lines to the debug console.
        /// </summary>
        /// <remarks>
        /// <c>OxyPlot.Wpf.Plot.InvalidatePlotPhaseDiagnosticsEnabled</c> mirrors to this flag; setting
        /// it on the WPF side enables this on the core side too.
        /// </remarks>
        public static volatile bool WheelTraceEnabled;

        private static long _globalWheelSeq;

        [System.ThreadStatic]
        private static int _wheelSeq;

        [System.ThreadStatic]
        private static long _wheelStartTicks;

        /// <summary>
        /// Opens a new wheel-event scope: increments the global sequence number and resets the
        /// per-event start clock. Call this exactly once at the WPF <c>OnMouseWheel</c> entry.
        /// </summary>
        /// <returns>The new wheel sequence number, or 0 if tracing is disabled.</returns>
        public static int BeginWheel()
        {
            if (!WheelTraceEnabled) return 0;
            _wheelSeq = (int)System.Threading.Interlocked.Increment(ref _globalWheelSeq);
            _wheelStartTicks = System.Diagnostics.Stopwatch.GetTimestamp();
            return _wheelSeq;
        }

        /// <summary>
        /// Closes the current wheel-event scope on this thread: clears the per-thread sequence
        /// number and start clock so subsequent unrelated calls (mouse-move, pan, render) don't
        /// emit trace output attributed to the last wheel event. Call this exactly once at the
        /// end of the WPF <c>OnMouseWheel</c> handler, paired with <see cref="BeginWheel"/>.
        /// </summary>
        public static void EndWheel()
        {
            _wheelSeq = 0;
            _wheelStartTicks = 0;
        }

        /// <summary>
        /// Returns the current wheel sequence number for this thread, or 0 if no wheel scope is open.
        /// </summary>
        /// <remarks>
        /// Backed by a <c>[ThreadStatic]</c> field. Reads from any thread other than the one that
        /// called <see cref="BeginWheel"/> (e.g. a background diagnostic logger or the WPF render
        /// thread) always return 0. Intended for use only from the UI thread that processes the
        /// wheel event.
        /// </remarks>
        public static int CurrentWheelSeq => _wheelSeq;

        /// <summary>
        /// Returns true if a wheel scope has been opened on the current thread and tracing is on.
        /// Cheap; intended to gate the scope helpers below without the cost of building log strings.
        /// </summary>
        public static bool IsActive => WheelTraceEnabled && _wheelSeq != 0;

        /// <summary>
        /// Milliseconds since <see cref="BeginWheel"/> was called on the current thread. Returns
        /// -1 if no scope is open.
        /// </summary>
        public static double SinceWheelStartMs()
        {
            if (_wheelStartTicks == 0) return -1;
            long delta = System.Diagnostics.Stopwatch.GetTimestamp() - _wheelStartTicks;
            return delta * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
        }

        /// <summary>
        /// Emits a single trace line in the form
        /// <c>[W#NNNN +TTTT.TTms] message</c>. Cheap when <see cref="WheelTraceEnabled"/> is false.
        /// </summary>
        /// <param name="message">The message to emit.</param>
        public static void Log(string message)
        {
            if (!WheelTraceEnabled || _wheelSeq == 0) return;
            System.Diagnostics.Debug.WriteLine(
                $"[W#{_wheelSeq,4} +{SinceWheelStartMs(),8:F2}ms] {message}");
        }

        /// <summary>
        /// Emits an "ENTER" trace line and returns a disposable that emits a matching
        /// "EXIT methodLocalMs=X.XX" line on disposal. Use with <c>using</c>:
        /// <code>using (PlotDiagnostics.Trace("Foo.Method")) { ... }</code>
        /// </summary>
        /// <param name="methodTag">A short tag identifying the method, e.g. <c>"Axis.ZoomAt"</c>.</param>
        /// <param name="extra">Optional extra context appended to the ENTER line.</param>
        public static System.IDisposable Trace(string methodTag, string extra = null)
        {
            if (!WheelTraceEnabled || _wheelSeq == 0) return _noopScope;
            return new TraceScope(methodTag, extra);
        }

        private static readonly System.IDisposable _noopScope = new NoopScope();

        private sealed class NoopScope : System.IDisposable
        {
            public void Dispose() { }
        }

        private sealed class TraceScope : System.IDisposable
        {
            private readonly string _tag;
            private readonly long _t0;

            public TraceScope(string tag, string extra)
            {
                _tag = tag;
                _t0 = System.Diagnostics.Stopwatch.GetTimestamp();
                System.Diagnostics.Debug.WriteLine(
                    string.IsNullOrEmpty(extra)
                        ? $"[W#{_wheelSeq,4} +{SinceWheelStartMs(),8:F2}ms] {_tag} ENTER"
                        : $"[W#{_wheelSeq,4} +{SinceWheelStartMs(),8:F2}ms] {_tag} ENTER {extra}");
            }

            public void Dispose()
            {
                long ticks = System.Diagnostics.Stopwatch.GetTimestamp() - _t0;
                double ms = ticks * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
                System.Diagnostics.Debug.WriteLine(
                    $"[W#{_wheelSeq,4} +{SinceWheelStartMs(),8:F2}ms] {_tag} EXIT {ms,8:F2}ms");
            }
        }
#else
        /// <summary>
        /// No-op in Release builds.
        /// </summary>
        public static int BeginWheel() => 0;

        /// <summary>
        /// No-op in Release builds.
        /// </summary>
        public static void EndWheel() { }

        /// <summary>
        /// No-op in Release builds.
        /// </summary>
        public static bool IsActive => false;

        /// <summary>
        /// No-op in Release builds.
        /// </summary>
        public static void Log(string message) { }

        /// <summary>
        /// No-op in Release builds.
        /// </summary>
        public static System.IDisposable Trace(string methodTag, string extra = null) => null;
#endif
    }
}
