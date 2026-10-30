// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DrawingVisual.cs" company="XGoodFit">
//   Copyright (c) 2026 XGoodFit contributors
// </copyright>
// <summary>
//   The retained drawing of a DrawingVisualHost, standing in for WPF's DrawingVisual.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf
{
    using System;
    using System.Collections.Generic;
    using Avalonia;
    using Avalonia.Media;

    /// <summary>
    /// The retained drawing of a <see cref="DrawingVisualHost"/>: what <see cref="DrawingVisualRenderContext"/>
    /// drew last.
    /// </summary>
    /// <remarks>
    /// <para>
    /// XGoodFit addition, standing in for WPF's <c>DrawingVisual</c>. In WPF, a plot draws into its visual
    /// through <c>DrawingVisual.RenderOpen</c> when it renders, and WPF keeps that drawing until the next
    /// one. Avalonia controls draw only when Avalonia's renderer asks them to, and Avalonia's own recorded
    /// drawing (<c>DrawingGroup.Open</c>) can't hold bitmaps, which heat maps and image annotations draw.
    /// </para>
    /// <para>
    /// So <see cref="RenderOpen"/> returns a <see cref="DrawingRecorder"/> with the WPF drawing context's
    /// methods, which records the drawing; closing it keeps the recording and asks the host to render,
    /// and the host replays the recording into Avalonia's drawing context (<see cref="Render"/>). The plot
    /// still draws when it renders, as in WPF, so its axes and hit-testing data are current then.
    /// </para>
    /// </remarks>
    public sealed class DrawingVisual
    {
        /// <summary>
        /// The recorded drawing commands.
        /// </summary>
        private IReadOnlyList<Action<DrawingContext, Stack<DrawingContext.PushedState>>> commands =
            Array.Empty<Action<DrawingContext, Stack<DrawingContext.PushedState>>>();

        /// <summary>
        /// Occurs when a new drawing has been recorded.
        /// </summary>
        public event EventHandler Changed;

        /// <summary>
        /// Starts recording a new drawing, which replaces the current one when the recorder is closed.
        /// </summary>
        /// <returns>The recorder.</returns>
        public DrawingRecorder RenderOpen()
        {
            return new DrawingRecorder(this);
        }

        /// <summary>
        /// Draws the recorded drawing.
        /// </summary>
        /// <param name="context">The drawing context to draw into.</param>
        public void Render(DrawingContext context)
        {
            var pushed = new Stack<DrawingContext.PushedState>();
            foreach (var command in this.commands)
            {
                command(context, pushed);
            }

            while (pushed.Count > 0)
            {
                pushed.Pop().Dispose();
            }
        }

        /// <summary>
        /// Keeps a finished recording.
        /// </summary>
        /// <param name="recording">The recorded commands.</param>
        internal void Commit(List<Action<DrawingContext, Stack<DrawingContext.PushedState>>> recording)
        {
            this.commands = recording;
            this.Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Records drawing into a <see cref="DrawingVisual"/>, with the methods of WPF's drawing context.
    /// </summary>
    /// <remarks>
    /// XGoodFit addition; see <see cref="DrawingVisual"/>. As in WPF, every <c>Push</c> is matched by a
    /// <see cref="Pop"/>, and nothing shows until <see cref="Close"/>.
    /// </remarks>
    public sealed class DrawingRecorder : IDisposable
    {
        /// <summary>
        /// The visual the recording is for.
        /// </summary>
        private readonly DrawingVisual visual;

        /// <summary>
        /// The commands recorded so far.
        /// </summary>
        private readonly List<Action<DrawingContext, Stack<DrawingContext.PushedState>>> commands =
            new List<Action<DrawingContext, Stack<DrawingContext.PushedState>>>();

        /// <summary>
        /// Whether the recording has been closed.
        /// </summary>
        private bool closed;

        /// <summary>
        /// Initializes a new instance of the <see cref="DrawingRecorder"/> class.
        /// </summary>
        /// <param name="visual">The visual the recording is for.</param>
        internal DrawingRecorder(DrawingVisual visual)
        {
            this.visual = visual;
        }

        /// <summary>
        /// Draws a geometry.
        /// </summary>
        /// <param name="brush">The fill, or null.</param>
        /// <param name="pen">The stroke, or null.</param>
        /// <param name="geometry">The geometry.</param>
        public void DrawGeometry(IBrush brush, IPen pen, Geometry geometry)
        {
            this.Add((dc, _) => dc.DrawGeometry(brush, pen, geometry));
        }

        /// <summary>
        /// Draws a rectangle.
        /// </summary>
        /// <param name="brush">The fill, or null.</param>
        /// <param name="pen">The stroke, or null.</param>
        /// <param name="rectangle">The rectangle.</param>
        public void DrawRectangle(IBrush brush, IPen pen, Rect rectangle)
        {
            this.Add((dc, _) => dc.DrawRectangle(brush, pen, rectangle));
        }

        /// <summary>
        /// Draws an ellipse.
        /// </summary>
        /// <param name="brush">The fill, or null.</param>
        /// <param name="pen">The stroke, or null.</param>
        /// <param name="center">The centre.</param>
        /// <param name="radiusX">The horizontal radius.</param>
        /// <param name="radiusY">The vertical radius.</param>
        public void DrawEllipse(IBrush brush, IPen pen, Point center, double radiusX, double radiusY)
        {
            this.Add((dc, _) => dc.DrawEllipse(brush, pen, center, radiusX, radiusY));
        }

        /// <summary>
        /// Draws text.
        /// </summary>
        /// <param name="formattedText">The text.</param>
        /// <param name="origin">The top-left corner of the text.</param>
        public void DrawText(FormattedText formattedText, Point origin)
        {
            this.Add((dc, _) => dc.DrawText(formattedText, origin));
        }

        /// <summary>
        /// Draws an image.
        /// </summary>
        /// <param name="imageSource">The image.</param>
        /// <param name="rectangle">Where to draw it.</param>
        public void DrawImage(IImage imageSource, Rect rectangle)
        {
            this.Add((dc, _) => dc.DrawImage(imageSource, rectangle));
        }

        /// <summary>
        /// Draws part of an image.
        /// </summary>
        /// <param name="imageSource">The image.</param>
        /// <param name="sourceRectangle">The part of the image, in its own units.</param>
        /// <param name="destinationRectangle">Where to draw it.</param>
        /// <remarks>WPF draws part of an image through a cropped bitmap.</remarks>
        public void DrawImage(IImage imageSource, Rect sourceRectangle, Rect destinationRectangle)
        {
            this.Add((dc, _) => dc.DrawImage(imageSource, sourceRectangle, destinationRectangle));
        }

        /// <summary>
        /// Clips what follows to a geometry, until the matching <see cref="Pop"/>.
        /// </summary>
        /// <param name="clipGeometry">The clip.</param>
        public void PushClip(Geometry clipGeometry)
        {
            this.Add((dc, pushed) => pushed.Push(dc.PushGeometryClip(clipGeometry)));
        }

        /// <summary>
        /// Transforms what follows, until the matching <see cref="Pop"/>.
        /// </summary>
        /// <param name="transform">The transform. Its current value is recorded.</param>
        public void PushTransform(Transform transform)
        {
            var matrix = transform.Value;
            this.Add((dc, pushed) => pushed.Push(dc.PushTransform(matrix)));
        }

        /// <summary>
        /// Makes what follows partly transparent, until the matching <see cref="Pop"/>.
        /// </summary>
        /// <param name="opacity">The opacity.</param>
        public void PushOpacity(double opacity)
        {
            this.Add((dc, pushed) => pushed.Push(dc.PushOpacity(opacity)));
        }

        /// <summary>
        /// Ends the latest <c>Push</c>.
        /// </summary>
        public void Pop()
        {
            this.Add((dc, pushed) =>
            {
                if (pushed.Count > 0)
                {
                    pushed.Pop().Dispose();
                }
            });
        }

        /// <summary>
        /// Finishes the recording, which then replaces the visual's drawing.
        /// </summary>
        public void Close()
        {
            if (this.closed)
            {
                return;
            }

            this.closed = true;
            this.visual.Commit(this.commands);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            this.Close();
        }

        /// <summary>
        /// Records a command.
        /// </summary>
        /// <param name="command">The command.</param>
        private void Add(Action<DrawingContext, Stack<DrawingContext.PushedState>> command)
        {
            if (this.closed)
            {
                throw new InvalidOperationException("The drawing has been closed.");
            }

            this.commands.Add(command);
        }
    }
}
