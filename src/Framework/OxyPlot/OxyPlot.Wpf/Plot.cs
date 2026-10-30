// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Plot.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Represents a WPF control that displays a plot with support for declarative series, axes, and annotations.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

#nullable enable annotations
#nullable disable warnings

namespace OxyPlot.Wpf
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Linq;
    using Avalonia;
    using Avalonia.Controls;
    using Avalonia.Controls.Metadata;
    using Avalonia.Input;
    using Avalonia.LogicalTree;
    using Avalonia.Media;
    using Avalonia.Metadata;
    using Avalonia.Threading;
    using OxyPlot.Legends;

    /// <summary>
    /// Represents a WPF control that displays a plot with support for declarative series, axes, and annotations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This control extends <see cref="PlotView"/> to add ObservableCollections for Series, Axes, and Annotations,
    /// enabling declarative XAML-based plot construction with data binding support.
    /// </para>
    /// <para>
    /// XGoodFit port:
    /// <list type="bullet">
    /// <item>Its dependency properties are styled properties with the same names, types and defaults, and
    /// the same change callbacks; colours are Avalonia's, brushes <see cref="IBrush"/>, font weights
    /// Avalonia's enum and the legend item alignment Avalonia's. As in WPF, its theme is PlotViewBase's.</item>
    /// <item><see cref="Series"/> is the XAML content property (Avalonia's <see cref="ContentAttribute"/> on
    /// the collection, where WPF names it on the class).</item>
    /// <item>Series, axes and annotations join Avalonia's logical children, which Avalonia keeps as a list
    /// rather than WPF's enumerated LogicalChildren.</item>
    /// <item><see cref="PropertyChanged"/> hides Avalonia's own property-changed event of the same name,
    /// and is this class's INotifyPropertyChanged event, as in WPF.</item>
    /// </list>
    /// </para>
    /// </remarks>
    [TemplatePart(PartGrid, typeof(Grid))]
    public partial class Plot : PlotView, INotifyPropertyChanged
    {
        /// <summary>
        /// Occurs when a property value changes on this Plot or on any item in its
        /// Annotations, Series, or Axes collections.
        /// </summary>
        /// <remarks>
        /// <para>
        /// For Plot-level properties, the property name is the DP name (e.g., "Title", "SubtitleColor").
        /// For collection structure changes, the property name is the collection name ("Annotations", "Series", "Axes").
        /// For item-level property changes, the property name is prefixed (e.g., "Annotation.Color", "Series.Title").
        /// </para>
        /// <para>
        /// This event is suppressed when <see cref="SuppressPropertyChanged"/> is true.
        /// </para>
        /// </remarks>
        public new event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Backing field for <see cref="SuppressPropertyChanged"/>. Marked <c>volatile</c> so
        /// reads on the dispatcher thread reliably observe writes from any thread without a
        /// memory barrier at the call site. The <see cref="InvalidatePlot"/> gate reads this
        /// flag once per call, so a stale-read race could miss a suppressed bulk-update window
        /// or render mid-suppression on consumers that prepare data on a worker thread.
        /// </summary>
        private volatile bool suppressPropertyChanged;

        /// <summary>
        /// Gets or sets whether <see cref="PropertyChanged"/> events are suppressed.
        /// When true, no PropertyChanged events fire from this Plot instance,
        /// including relayed item and collection change events.
        /// </summary>
        /// <remarks>
        /// Use this to suppress events during bulk operations such as deserialization,
        /// theme application, or programmatic series population where the undo system
        /// should not record individual changes. The backing field is <c>volatile</c> so
        /// cross-thread writes are visible without explicit synchronization at the call site,
        /// but the broader <see cref="InvalidatePlot"/> pipeline still expects dispatch-thread
        /// access for visual-tree mutations.
        /// </remarks>
        public bool SuppressPropertyChanged
        {
            get => this.suppressPropertyChanged;
            set => this.suppressPropertyChanged = value;
        }

        /// <summary>
        /// Occurs when an axis is replaced via <see cref="ReplaceAxis"/>,
        /// typically due to an axis type change (e.g., Linear to Logarithmic).
        /// The first parameter is the old axis, the second is the new axis.
        /// </summary>
        public event Action<Axis, Axis>? AxisReplaced;

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event if not suppressed.
        /// </summary>
        /// <param name="propertyName">The name of the property that changed.</param>
        protected void OnPropertyChanged(string propertyName)
        {
            if (SuppressPropertyChanged) return;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Identifies the <see cref="Title"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<string> TitleProperty = AvaloniaProperty.Register<Plot, string>(
            nameof(Title),
            null);

        /// <summary>
        /// Identifies the <see cref="Subtitle"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<string> SubtitleProperty = AvaloniaProperty.Register<Plot, string>(
            nameof(Subtitle),
            null);

        /// <summary>
        /// Identifies the <see cref="TitleColor"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<Color> TitleColorProperty = AvaloniaProperty.Register<Plot, Color>(
            nameof(TitleColor),
            MoreColors.Automatic);

        /// <summary>
        /// Identifies the <see cref="SubtitleColor"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<Color> SubtitleColorProperty = AvaloniaProperty.Register<Plot, Color>(
            nameof(SubtitleColor),
            MoreColors.Automatic);

        /// <summary>
        /// Identifies the <see cref="PlotAreaBackground"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<IBrush> PlotAreaBackgroundProperty = AvaloniaProperty.Register<Plot, IBrush>(
            nameof(PlotAreaBackground),
            null);

        /// <summary>
        /// Identifies the <see cref="PlotAreaBorderColor"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<Color> PlotAreaBorderColorProperty = AvaloniaProperty.Register<Plot, Color>(
            nameof(PlotAreaBorderColor),
            Colors.Black);

        /// <summary>
        /// Identifies the <see cref="PlotAreaBorderThickness"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<Thickness> PlotAreaBorderThicknessProperty = AvaloniaProperty.Register<Plot, Thickness>(
            nameof(PlotAreaBorderThickness),
            new Thickness(1));

        /// <summary>
        /// Identifies the <see cref="IsLegendVisible"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<bool> IsLegendVisibleProperty = AvaloniaProperty.Register<Plot, bool>(
            nameof(IsLegendVisible),
            true);

        /// <summary>
        /// Identifies the <see cref="TextColor"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<Color> TextColorProperty = AvaloniaProperty.Register<Plot, Color>(
            nameof(TextColor),
            Colors.Black);

        /// <summary>
        /// Identifies the <see cref="TitleFont"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<string> TitleFontProperty = AvaloniaProperty.Register<Plot, string>(
            nameof(TitleFont),
            null);

        /// <summary>
        /// Identifies the <see cref="TitleFontSize"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> TitleFontSizeProperty = AvaloniaProperty.Register<Plot, double>(
            nameof(TitleFontSize),
            18.0);

        /// <summary>
        /// Identifies the <see cref="TitleFontWeight"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<FontWeight> TitleFontWeightProperty = AvaloniaProperty.Register<Plot, FontWeight>(
            nameof(TitleFontWeight),
            FontWeight.Bold);

        /// <summary>
        /// Identifies the <see cref="TitlePadding"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> TitlePaddingProperty = AvaloniaProperty.Register<Plot, double>(
            nameof(TitlePadding),
            6.0);

        /// <summary>
        /// Identifies the <see cref="SubtitleFont"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<string> SubtitleFontProperty = AvaloniaProperty.Register<Plot, string>(
            nameof(SubtitleFont),
            null);

        /// <summary>
        /// Identifies the <see cref="SubtitleFontSize"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> SubtitleFontSizeProperty = AvaloniaProperty.Register<Plot, double>(
            nameof(SubtitleFontSize),
            14.0);

        /// <summary>
        /// Identifies the <see cref="SubtitleFontWeight"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<FontWeight> SubtitleFontWeightProperty = AvaloniaProperty.Register<Plot, FontWeight>(
            nameof(SubtitleFontWeight),
            FontWeight.Normal);

        /// <summary>
        /// Identifies the <see cref="DefaultPlotCursor"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<Cursor> DefaultPlotCursorProperty = AvaloniaProperty.Register<Plot, Cursor>(
            nameof(DefaultPlotCursor),
            null);

        /// <summary>
        /// Identifies the <see cref="LegendBackground"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<Color> LegendBackgroundProperty = AvaloniaProperty.Register<Plot, Color>(
            nameof(LegendBackground),
            MoreColors.Undefined);

        /// <summary>
        /// Identifies the <see cref="LegendBorder"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<Color> LegendBorderProperty = AvaloniaProperty.Register<Plot, Color>(
            nameof(LegendBorder),
            MoreColors.Undefined);

        /// <summary>
        /// Identifies the <see cref="LegendBorderThickness"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> LegendBorderThicknessProperty = AvaloniaProperty.Register<Plot, double>(
            nameof(LegendBorderThickness),
            1.0);

        /// <summary>
        /// Identifies the <see cref="LegendItemAlignment"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<Avalonia.Layout.HorizontalAlignment> LegendItemAlignmentProperty = AvaloniaProperty.Register<Plot, Avalonia.Layout.HorizontalAlignment>(
            nameof(LegendItemAlignment),
            Avalonia.Layout.HorizontalAlignment.Left);

        /// <summary>
        /// Identifies the <see cref="LegendItemClickTogglesSeriesVisibility"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<bool> LegendItemClickTogglesSeriesVisibilityProperty = AvaloniaProperty.Register<Plot, bool>(
            nameof(LegendItemClickTogglesSeriesVisibility),
            false);

        /// <summary>
        /// Identifies the <see cref="LegendItemOrder"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<OxyPlot.Legends.LegendItemOrder> LegendItemOrderProperty = AvaloniaProperty.Register<Plot, OxyPlot.Legends.LegendItemOrder>(
            nameof(LegendItemOrder),
            OxyPlot.Legends.LegendItemOrder.Normal);

        /// <summary>
        /// Identifies the <see cref="LegendItemSpacing"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> LegendItemSpacingProperty = AvaloniaProperty.Register<Plot, double>(
            nameof(LegendItemSpacing),
            24.0);

        /// <summary>
        /// Identifies the <see cref="LegendLineSpacing"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> LegendLineSpacingProperty = AvaloniaProperty.Register<Plot, double>(
            nameof(LegendLineSpacing),
            0.0);

        /// <summary>
        /// Identifies the <see cref="LegendMargin"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> LegendMarginProperty = AvaloniaProperty.Register<Plot, double>(
            nameof(LegendMargin),
            8.0);

        /// <summary>
        /// Identifies the <see cref="LegendMaxHeight"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> LegendMaxHeightProperty = AvaloniaProperty.Register<Plot, double>(
            nameof(LegendMaxHeight),
            double.NaN);

        /// <summary>
        /// Identifies the <see cref="LegendMaxWidth"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> LegendMaxWidthProperty = AvaloniaProperty.Register<Plot, double>(
            nameof(LegendMaxWidth),
            double.NaN);

        /// <summary>
        /// Identifies the <see cref="LegendOrientation"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<OxyPlot.Legends.LegendOrientation> LegendOrientationProperty = AvaloniaProperty.Register<Plot, OxyPlot.Legends.LegendOrientation>(
            nameof(LegendOrientation),
            OxyPlot.Legends.LegendOrientation.Vertical);

        /// <summary>
        /// Identifies the <see cref="LegendPadding"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> LegendPaddingProperty = AvaloniaProperty.Register<Plot, double>(
            nameof(LegendPadding),
            8.0);

        /// <summary>
        /// Identifies the <see cref="LegendPlacement"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<OxyPlot.Legends.LegendPlacement> LegendPlacementProperty = AvaloniaProperty.Register<Plot, OxyPlot.Legends.LegendPlacement>(
            nameof(LegendPlacement),
            OxyPlot.Legends.LegendPlacement.Inside);

        /// <summary>
        /// Identifies the <see cref="LegendPosition"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<OxyPlot.Legends.LegendPosition> LegendPositionProperty = AvaloniaProperty.Register<Plot, OxyPlot.Legends.LegendPosition>(
            nameof(LegendPosition),
            OxyPlot.Legends.LegendPosition.RightTop);

        /// <summary>
        /// Identifies the <see cref="LegendSymbolLength"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> LegendSymbolLengthProperty = AvaloniaProperty.Register<Plot, double>(
            nameof(LegendSymbolLength),
            16.0);

        /// <summary>
        /// Identifies the <see cref="LegendSymbolMargin"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> LegendSymbolMarginProperty = AvaloniaProperty.Register<Plot, double>(
            nameof(LegendSymbolMargin),
            4.0);

        /// <summary>
        /// Identifies the <see cref="LegendSymbolPlacement"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<OxyPlot.Legends.LegendSymbolPlacement> LegendSymbolPlacementProperty = AvaloniaProperty.Register<Plot, OxyPlot.Legends.LegendSymbolPlacement>(
            nameof(LegendSymbolPlacement),
            OxyPlot.Legends.LegendSymbolPlacement.Left);

        /// <summary>
        /// Identifies the <see cref="LegendTextColor"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<Color> LegendTextColorProperty = AvaloniaProperty.Register<Plot, Color>(
            nameof(LegendTextColor),
            MoreColors.Automatic);

        /// <summary>
        /// Identifies the <see cref="LegendTitle"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<string> LegendTitleProperty = AvaloniaProperty.Register<Plot, string>(
            nameof(LegendTitle),
            null);

        /// <summary>
        /// Identifies the <see cref="LegendTitleColor"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<Color> LegendTitleColorProperty = AvaloniaProperty.Register<Plot, Color>(
            nameof(LegendTitleColor),
            MoreColors.Automatic);

        /// <summary>
        /// Identifies the <see cref="LegendTitleFont"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<string> LegendTitleFontProperty = AvaloniaProperty.Register<Plot, string>(
            nameof(LegendTitleFont),
            null);

        /// <summary>
        /// Identifies the <see cref="LegendTitleFontSize"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> LegendTitleFontSizeProperty = AvaloniaProperty.Register<Plot, double>(
            nameof(LegendTitleFontSize),
            double.NaN);

        /// <summary>
        /// Identifies the <see cref="LegendTitleFontWeight"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<FontWeight> LegendTitleFontWeightProperty = AvaloniaProperty.Register<Plot, FontWeight>(
            nameof(LegendTitleFontWeight),
            FontWeight.Bold);

        /// <summary>
        /// Identifies the <see cref="LegendColumnSpacing"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> LegendColumnSpacingProperty = AvaloniaProperty.Register<Plot, double>(
            nameof(LegendColumnSpacing),
            8.0);

        /// <summary>
        /// Identifies the <see cref="LegendFont"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<string> LegendFontProperty = AvaloniaProperty.Register<Plot, string>(
            nameof(LegendFont),
            null);

        /// <summary>
        /// Identifies the <see cref="LegendFontSize"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<double> LegendFontSizeProperty = AvaloniaProperty.Register<Plot, double>(
            nameof(LegendFontSize),
            double.NaN);

        /// <summary>
        /// Identifies the <see cref="LegendFontWeight"/> dependency property.
        /// </summary>
        public static readonly StyledProperty<FontWeight> LegendFontWeightProperty = AvaloniaProperty.Register<Plot, FontWeight>(
            nameof(LegendFontWeight),
            FontWeight.Normal);

        /// <summary>
        /// The series collection.
        /// </summary>
        private readonly ObservableCollection<Series> series;

        /// <summary>
        /// The axes collection.
        /// </summary>
        private readonly ObservableCollection<Axis> axes;

        /// <summary>
        /// The annotations collection.
        /// </summary>
        private readonly ObservableCollection<Annotation> annotations;

        /// <summary>
        /// Shadow list tracking Series items subscribed to PropertyChanged.
        /// Required to unsubscribe on Reset (Clear), which does not provide OldItems.
        /// </summary>
        private readonly List<Series> subscribedSeries = new List<Series>();

        /// <summary>
        /// Shadow list tracking Axis items subscribed to PropertyChanged.
        /// </summary>
        private readonly List<Axis> subscribedAxes = new List<Axis>();

        /// <summary>
        /// Shadow list tracking Annotation items subscribed to PropertyChanged.
        /// </summary>
        private readonly List<Annotation> subscribedAnnotations = new List<Annotation>();

        /// <summary>
        /// The internal plot model.
        /// </summary>
        private readonly PlotModel internalModel;

        /// <summary>
        /// When true, the next <see cref="InvalidatePlot"/> call will re-synchronize
        /// all WPF wrapper properties to the internal OxyPlot model. Set by any mutation
        /// (DP changes, collection changes). Cleared after synchronization completes.
        /// This avoids expensive re-synchronization during zoom/pan, where only
        /// internal axis ranges change and the WPF wrappers are untouched.
        /// </summary>
        /// <remarks>
        /// Semantics: this is the "force full sync" flag. Setting it to <c>true</c> guarantees
        /// every Synchronize* method runs on the next non-gated <see cref="InvalidatePlot"/>.
        /// Internal callers that know which collection changed should set the corresponding
        /// per-collection dirty flag (<see cref="_seriesDirty"/>, <see cref="_axesDirty"/>,
        /// <see cref="_annotationsDirty"/>, <see cref="_propertiesDirty"/>) instead — those
        /// trigger only the sync work that actually applies. External callers that don't have
        /// that granularity can keep setting <c>_needsSynchronization = true</c>; that path
        /// is preserved as a conservative "sync everything" fallback for backward compatibility.
        /// </remarks>
        internal bool _needsSynchronization = true;

        /// <summary>
        /// Set to <c>true</c> when an item in <see cref="Series"/> changed (collection mutation
        /// or item-level PropertyChanged). Triggers <see cref="SynchronizeSeries"/> alone on
        /// the next sync pass — avoids re-creating all axes and annotation models for an
        /// unrelated series-only edit.
        /// </summary>
        private bool _seriesDirty;

        /// <summary>
        /// Set to <c>true</c> when an item in <see cref="Axes"/> changed. Triggers
        /// <see cref="SynchronizeAxes"/> alone on the next sync pass.
        /// </summary>
        private bool _axesDirty;

        /// <summary>
        /// Set to <c>true</c> when an item in <see cref="Annotations"/> changed. Triggers
        /// <see cref="SynchronizeAnnotations"/> alone on the next sync pass.
        /// </summary>
        private bool _annotationsDirty;

        /// <summary>
        /// Set to <c>true</c> when a Plot-level DP changed (background, title, legend
        /// appearance, etc.). Triggers <see cref="SynchronizeProperties"/> alone on the
        /// next sync pass.
        /// </summary>
        private bool _propertiesDirty;

        /// <summary>
        /// When true, the next non-gated <see cref="InvalidatePlot"/> call must run with
        /// <c>updateData=true</c> regardless of the caller's argument, because at least one
        /// suppressed call requested a data refresh. Set inside the suppression gate when
        /// <c>updateData=true</c> is dropped; cleared as soon as it is consumed by a non-gated
        /// call. This guarantees a data refresh is never silently lost across a suppression
        /// window, even if the consumer's final flush call passes <c>updateData=false</c>.
        /// </summary>
        /// <remarks>
        /// Intentionally non-volatile: both the writer (suppression-gate branch in
        /// <see cref="InvalidatePlot"/>) and the consumer (just below the gate, same method)
        /// run on the dispatcher thread. The cross-thread visibility we need is on
        /// <see cref="SuppressPropertyChanged"/>, not on this flag — once a gated write
        /// observes <c>SuppressPropertyChanged==true</c>, the corresponding pending-flag
        /// write is already on the dispatcher thread.
        /// </remarks>
        private bool _pendingUpdateData;

        /// <summary>
        /// Initializes static members of the <see cref="Plot"/> class.
        /// </summary>
        static Plot()
        {
            TitleProperty.Changed.AddClassHandler<Plot>(AppearanceChanged);
            SubtitleProperty.Changed.AddClassHandler<Plot>(AppearanceChanged);
            TitleColorProperty.Changed.AddClassHandler<Plot>(AppearanceChanged);
            SubtitleColorProperty.Changed.AddClassHandler<Plot>(AppearanceChanged);
            PlotAreaBackgroundProperty.Changed.AddClassHandler<Plot>(AppearanceChanged);
            PlotAreaBorderColorProperty.Changed.AddClassHandler<Plot>(AppearanceChanged);
            PlotAreaBorderThicknessProperty.Changed.AddClassHandler<Plot>(AppearanceChanged);
            IsLegendVisibleProperty.Changed.AddClassHandler<Plot>(AppearanceChanged);
            TextColorProperty.Changed.AddClassHandler<Plot>(AppearanceChanged);
            TitleFontProperty.Changed.AddClassHandler<Plot>(AppearanceChanged);
            TitleFontSizeProperty.Changed.AddClassHandler<Plot>(AppearanceChanged);
            TitleFontWeightProperty.Changed.AddClassHandler<Plot>(AppearanceChanged);
            TitlePaddingProperty.Changed.AddClassHandler<Plot>(AppearanceChanged);
            SubtitleFontProperty.Changed.AddClassHandler<Plot>(AppearanceChanged);
            SubtitleFontSizeProperty.Changed.AddClassHandler<Plot>(AppearanceChanged);
            SubtitleFontWeightProperty.Changed.AddClassHandler<Plot>(AppearanceChanged);
            LegendBackgroundProperty.Changed.AddClassHandler<Plot>(AppearanceChanged);
            LegendBorderProperty.Changed.AddClassHandler<Plot>(AppearanceChanged);
            LegendBorderThicknessProperty.Changed.AddClassHandler<Plot>(AppearanceChanged);
            LegendItemAlignmentProperty.Changed.AddClassHandler<Plot>(AppearanceChanged);
            LegendItemClickTogglesSeriesVisibilityProperty.Changed.AddClassHandler<Plot>(AppearanceChanged);
            LegendItemOrderProperty.Changed.AddClassHandler<Plot>(AppearanceChanged);
            LegendItemSpacingProperty.Changed.AddClassHandler<Plot>(AppearanceChanged);
            LegendLineSpacingProperty.Changed.AddClassHandler<Plot>(AppearanceChanged);
            LegendMarginProperty.Changed.AddClassHandler<Plot>(AppearanceChanged);
            LegendMaxHeightProperty.Changed.AddClassHandler<Plot>(AppearanceChanged);
            LegendMaxWidthProperty.Changed.AddClassHandler<Plot>(AppearanceChanged);
            LegendOrientationProperty.Changed.AddClassHandler<Plot>(AppearanceChanged);
            LegendPaddingProperty.Changed.AddClassHandler<Plot>(AppearanceChanged);
            LegendPlacementProperty.Changed.AddClassHandler<Plot>(AppearanceChanged);
            LegendPositionProperty.Changed.AddClassHandler<Plot>(AppearanceChanged);
            LegendSymbolLengthProperty.Changed.AddClassHandler<Plot>(AppearanceChanged);
            LegendSymbolMarginProperty.Changed.AddClassHandler<Plot>(AppearanceChanged);
            LegendSymbolPlacementProperty.Changed.AddClassHandler<Plot>(AppearanceChanged);
            LegendTextColorProperty.Changed.AddClassHandler<Plot>(AppearanceChanged);
            LegendTitleProperty.Changed.AddClassHandler<Plot>(AppearanceChanged);
            LegendTitleColorProperty.Changed.AddClassHandler<Plot>(AppearanceChanged);
            LegendTitleFontProperty.Changed.AddClassHandler<Plot>(AppearanceChanged);
            LegendTitleFontSizeProperty.Changed.AddClassHandler<Plot>(AppearanceChanged);
            LegendTitleFontWeightProperty.Changed.AddClassHandler<Plot>(AppearanceChanged);
            LegendColumnSpacingProperty.Changed.AddClassHandler<Plot>(AppearanceChanged);
            LegendFontProperty.Changed.AddClassHandler<Plot>(AppearanceChanged);
            LegendFontSizeProperty.Changed.AddClassHandler<Plot>(AppearanceChanged);
            LegendFontWeightProperty.Changed.AddClassHandler<Plot>(AppearanceChanged);
            // XGoodFit port: the theme key is PlotViewBase.StyleKeyOverride (WPF's DefaultStyleKey).
            PaddingProperty.OverrideDefaultValue<Plot>(new Thickness(8));
            PaddingProperty.Changed.AddClassHandler<Plot>(AppearanceChanged);
            // XGoodFit port: WPF gives every plot generic.xaml's background (White) as soon as it is made,
            // even outside a window, so that is the value a plot saves and draws. Avalonia themes reach
            // only controls in a window, so the theme's White is the default here instead of null.
            BackgroundProperty.OverrideDefaultValue<Plot>(Brushes.White);
            BackgroundProperty.Changed.AddClassHandler<Plot>(AppearanceChanged);
            BorderBrushProperty.OverrideDefaultValue<Plot>(null);
            BorderBrushProperty.Changed.AddClassHandler<Plot>(AppearanceChanged);
            BorderThicknessProperty.OverrideDefaultValue<Plot>(new Thickness(0));
            BorderThicknessProperty.Changed.AddClassHandler<Plot>(AppearanceChanged);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Plot"/> class.
        /// </summary>
        public Plot()
        {
            this.series = new ObservableCollection<Series>();
            this.axes = new ObservableCollection<Axis>();
            this.annotations = new ObservableCollection<Annotation>();

            this.series.CollectionChanged += this.OnSeriesChanged;
            this.axes.CollectionChanged += this.OnAxesChanged;
            this.annotations.CollectionChanged += this.OnAnnotationsChanged;

            this.internalModel = new PlotModel();
            ((IPlotModel)this.internalModel).AttachPlotView(this);
        }

        /// <summary>
        /// Gets the series collection.
        /// </summary>
        [Content]
        public ObservableCollection<Series> Series => this.series;

        /// <summary>
        /// Gets the axes collection.
        /// </summary>
        public ObservableCollection<Axis> Axes => this.axes;

        /// <summary>
        /// Gets the annotations collection.
        /// </summary>
        public ObservableCollection<Annotation> Annotations => this.annotations;

        /// <summary>
        /// Gets the actual model.
        /// </summary>
        public override PlotModel ActualModel => this.internalModel;

        /// <summary>
        /// Overlay canvas for backward compatibility when using the DrawingVisual backend.
        /// Provides a transparent canvas that toolbar and other code can add overlay elements to.
        /// </summary>
        private Canvas _overlayCanvas;

        /// <summary>
        /// Gets the canvas used for rendering.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When using the Canvas backend, returns the actual rendering canvas (identical to previous behavior).
        /// When using the DrawingVisual backend, returns a transparent overlay canvas that is
        /// automatically inserted into the grid. This allows toolbar code to continue adding
        /// overlay elements (leader lines, text editing controls) without modification.
        /// </para>
        /// </remarks>
        public Canvas canvas
        {
            get
            {
                if (this.plotPresenter is Canvas c)
                {
                    return c;
                }

                // DrawingVisual backend: return overlay canvas for toolbar compatibility.
                if (this._overlayCanvas == null)
                {
                    this._overlayCanvas = new Canvas { IsHitTestVisible = false };
                    if (this.grid != null)
                    {
                        var idx = this.grid.Children.IndexOf(this.plotPresenter);
                        this.grid.Children.Insert(idx + 1, this._overlayCanvas);
                    }
                }

                return this._overlayCanvas;
            }
        }

        /// <summary>
        /// Gets the grid used for layout.
        /// </summary>
        /// <remarks>
        /// This property is provided for backward compatibility with code that accesses the grid directly.
        /// </remarks>
        public new Grid grid => base.grid;

        /// <summary>
        /// Gets the render context.
        /// </summary>
        /// <remarks>
        /// This property is provided for backward compatibility with code that accesses the render context directly.
        /// </remarks>
        public IRenderContext RenderContext => this.renderContext;

        /// <summary>
        /// Hit-tests rendered text elements at the given point.
        /// </summary>
        /// <param name="point">The point in plot coordinates.</param>
        /// <returns>
        /// For the Canvas backend: delegates to <c>Canvas.InputHitTest</c> (returns a
        /// <see cref="Avalonia.Controls.TextBlock"/>, <see cref="Avalonia.Controls.Shapes.Path"/>, or null).
        /// For the DrawingVisual backend: queries recorded text positions (returns a
        /// <see cref="DrawingVisualRenderContext.TextHitResult"/> or null).
        /// </returns>
        /// <remarks>
        /// This method provides backend-agnostic text hit-testing for the OxyPlotToolbar.
        /// Callers should check the return type to determine the backend and extract the text content.
        /// </remarks>
        public object HitTestRenderedText(Point point)
        {
            if (this.plotPresenter is Canvas c)
            {
                return c.InputHitTest(point);
            }

            if (this.renderContext is DrawingVisualRenderContext dvrc)
            {
                return dvrc.HitTestText(new ScreenPoint(point.X, point.Y));
            }

            return null;
        }

        /// <summary>
        /// Gets or sets the plot title.
        /// </summary>
        public string Title
        {
            get => (string)this.GetValue(TitleProperty);
            set => this.SetValue(TitleProperty, value);
        }

        /// <summary>
        /// Gets or sets the plot subtitle.
        /// </summary>
        public string Subtitle
        {
            get => (string)this.GetValue(SubtitleProperty);
            set => this.SetValue(SubtitleProperty, value);
        }

        /// <summary>
        /// Gets or sets the title color.
        /// </summary>
        public Color TitleColor
        {
            get => (Color)this.GetValue(TitleColorProperty);
            set => this.SetValue(TitleColorProperty, value);
        }

        /// <summary>
        /// Gets or sets the subtitle color.
        /// </summary>
        public Color SubtitleColor
        {
            get => (Color)this.GetValue(SubtitleColorProperty);
            set => this.SetValue(SubtitleColorProperty, value);
        }

        /// <summary>
        /// Gets or sets the plot area background brush.
        /// </summary>
        public IBrush PlotAreaBackground
        {
            get => (IBrush)this.GetValue(PlotAreaBackgroundProperty);
            set => this.SetValue(PlotAreaBackgroundProperty, value);
        }

        /// <summary>
        /// Gets or sets the plot area border color.
        /// </summary>
        public Color PlotAreaBorderColor
        {
            get => (Color)this.GetValue(PlotAreaBorderColorProperty);
            set => this.SetValue(PlotAreaBorderColorProperty, value);
        }

        /// <summary>
        /// Gets or sets the plot area border thickness.
        /// </summary>
        public Thickness PlotAreaBorderThickness
        {
            get => (Thickness)this.GetValue(PlotAreaBorderThicknessProperty);
            set => this.SetValue(PlotAreaBorderThicknessProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the legend is visible.
        /// </summary>
        public bool IsLegendVisible
        {
            get => (bool)this.GetValue(IsLegendVisibleProperty);
            set => this.SetValue(IsLegendVisibleProperty, value);
        }

        /// <summary>
        /// Gets or sets the default text color.
        /// </summary>
        /// <remarks>
        /// This property round-trips through
        /// <c>OxyPlot.Wpf.Serialization.PlotSerializer.GeneralPropertiesToXElement</c>
        /// and is restored on load — saved plot configurations preserve the chosen
        /// text color rather than reverting to the framework default.
        /// </remarks>
        public Color TextColor
        {
            get => (Color)this.GetValue(TextColorProperty);
            set => this.SetValue(TextColorProperty, value);
        }

        /// <summary>
        /// Gets or sets the title font.
        /// </summary>
        public string TitleFont
        {
            get => (string)this.GetValue(TitleFontProperty);
            set => this.SetValue(TitleFontProperty, value);
        }

        /// <summary>
        /// Gets or sets the title font size.
        /// </summary>
        public double TitleFontSize
        {
            get => (double)this.GetValue(TitleFontSizeProperty);
            set => this.SetValue(TitleFontSizeProperty, value);
        }

        /// <summary>
        /// Gets or sets the title font weight.
        /// </summary>
        public FontWeight TitleFontWeight
        {
            get => (FontWeight)this.GetValue(TitleFontWeightProperty);
            set => this.SetValue(TitleFontWeightProperty, value);
        }

        /// <summary>
        /// Gets or sets the title padding.
        /// </summary>
        public double TitlePadding
        {
            get => (double)this.GetValue(TitlePaddingProperty);
            set => this.SetValue(TitlePaddingProperty, value);
        }

        /// <summary>
        /// Gets or sets the subtitle font.
        /// </summary>
        public string SubtitleFont
        {
            get => (string)this.GetValue(SubtitleFontProperty);
            set => this.SetValue(SubtitleFontProperty, value);
        }

        /// <summary>
        /// Gets or sets the subtitle font size.
        /// </summary>
        public double SubtitleFontSize
        {
            get => (double)this.GetValue(SubtitleFontSizeProperty);
            set => this.SetValue(SubtitleFontSizeProperty, value);
        }

        /// <summary>
        /// Gets or sets the subtitle font weight.
        /// </summary>
        public FontWeight SubtitleFontWeight
        {
            get => (FontWeight)this.GetValue(SubtitleFontWeightProperty);
            set => this.SetValue(SubtitleFontWeightProperty, value);
        }

        /// <summary>
        /// Gets or sets the default plot cursor.
        /// </summary>
        public Cursor DefaultPlotCursor
        {
            get => (Cursor)this.GetValue(DefaultPlotCursorProperty);
            set => this.SetValue(DefaultPlotCursorProperty, value);
        }

        /// <summary>
        /// Gets or sets the legend background color.
        /// </summary>
        public Color LegendBackground
        {
            get => (Color)this.GetValue(LegendBackgroundProperty);
            set => this.SetValue(LegendBackgroundProperty, value);
        }

        /// <summary>
        /// Gets or sets the legend border color.
        /// </summary>
        public Color LegendBorder
        {
            get => (Color)this.GetValue(LegendBorderProperty);
            set => this.SetValue(LegendBorderProperty, value);
        }

        /// <summary>
        /// Gets or sets the legend border thickness.
        /// </summary>
        public double LegendBorderThickness
        {
            get => (double)this.GetValue(LegendBorderThicknessProperty);
            set => this.SetValue(LegendBorderThicknessProperty, value);
        }

        /// <summary>
        /// Gets or sets the legend item alignment.
        /// </summary>
        public Avalonia.Layout.HorizontalAlignment LegendItemAlignment
        {
            get => (Avalonia.Layout.HorizontalAlignment)this.GetValue(LegendItemAlignmentProperty);
            set => this.SetValue(LegendItemAlignmentProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether clicking a legend item toggles the corresponding series visibility.
        /// </summary>
        public bool LegendItemClickTogglesSeriesVisibility
        {
            get => (bool)this.GetValue(LegendItemClickTogglesSeriesVisibilityProperty);
            set => this.SetValue(LegendItemClickTogglesSeriesVisibilityProperty, value);
        }

        /// <summary>
        /// Gets or sets the legend item order.
        /// </summary>
        public OxyPlot.Legends.LegendItemOrder LegendItemOrder
        {
            get => (OxyPlot.Legends.LegendItemOrder)this.GetValue(LegendItemOrderProperty);
            set => this.SetValue(LegendItemOrderProperty, value);
        }

        /// <summary>
        /// Gets or sets the legend item spacing.
        /// </summary>
        public double LegendItemSpacing
        {
            get => (double)this.GetValue(LegendItemSpacingProperty);
            set => this.SetValue(LegendItemSpacingProperty, value);
        }

        /// <summary>
        /// Gets or sets the legend line spacing.
        /// </summary>
        public double LegendLineSpacing
        {
            get => (double)this.GetValue(LegendLineSpacingProperty);
            set => this.SetValue(LegendLineSpacingProperty, value);
        }

        /// <summary>
        /// Gets or sets the legend margin.
        /// </summary>
        public double LegendMargin
        {
            get => (double)this.GetValue(LegendMarginProperty);
            set => this.SetValue(LegendMarginProperty, value);
        }

        /// <summary>
        /// Gets or sets the legend maximum height.
        /// </summary>
        public double LegendMaxHeight
        {
            get => (double)this.GetValue(LegendMaxHeightProperty);
            set => this.SetValue(LegendMaxHeightProperty, value);
        }

        /// <summary>
        /// Gets or sets the legend maximum width.
        /// </summary>
        public double LegendMaxWidth
        {
            get => (double)this.GetValue(LegendMaxWidthProperty);
            set => this.SetValue(LegendMaxWidthProperty, value);
        }

        /// <summary>
        /// Gets or sets the legend orientation.
        /// </summary>
        public OxyPlot.Legends.LegendOrientation LegendOrientation
        {
            get => (OxyPlot.Legends.LegendOrientation)this.GetValue(LegendOrientationProperty);
            set => this.SetValue(LegendOrientationProperty, value);
        }

        /// <summary>
        /// Gets or sets the legend padding.
        /// </summary>
        public double LegendPadding
        {
            get => (double)this.GetValue(LegendPaddingProperty);
            set => this.SetValue(LegendPaddingProperty, value);
        }

        /// <summary>
        /// Gets or sets the legend placement.
        /// </summary>
        public OxyPlot.Legends.LegendPlacement LegendPlacement
        {
            get => (OxyPlot.Legends.LegendPlacement)this.GetValue(LegendPlacementProperty);
            set => this.SetValue(LegendPlacementProperty, value);
        }

        /// <summary>
        /// Gets or sets the legend position.
        /// </summary>
        public OxyPlot.Legends.LegendPosition LegendPosition
        {
            get => (OxyPlot.Legends.LegendPosition)this.GetValue(LegendPositionProperty);
            set => this.SetValue(LegendPositionProperty, value);
        }

        /// <summary>
        /// Gets or sets the legend symbol length.
        /// </summary>
        public double LegendSymbolLength
        {
            get => (double)this.GetValue(LegendSymbolLengthProperty);
            set => this.SetValue(LegendSymbolLengthProperty, value);
        }

        /// <summary>
        /// Gets or sets the legend symbol margin.
        /// </summary>
        public double LegendSymbolMargin
        {
            get => (double)this.GetValue(LegendSymbolMarginProperty);
            set => this.SetValue(LegendSymbolMarginProperty, value);
        }

        /// <summary>
        /// Gets or sets the legend symbol placement.
        /// </summary>
        public OxyPlot.Legends.LegendSymbolPlacement LegendSymbolPlacement
        {
            get => (OxyPlot.Legends.LegendSymbolPlacement)this.GetValue(LegendSymbolPlacementProperty);
            set => this.SetValue(LegendSymbolPlacementProperty, value);
        }

        /// <summary>
        /// Gets or sets the legend text color.
        /// </summary>
        public Color LegendTextColor
        {
            get => (Color)this.GetValue(LegendTextColorProperty);
            set => this.SetValue(LegendTextColorProperty, value);
        }

        /// <summary>
        /// Gets or sets the legend title.
        /// </summary>
        public string LegendTitle
        {
            get => (string)this.GetValue(LegendTitleProperty);
            set => this.SetValue(LegendTitleProperty, value);
        }

        /// <summary>
        /// Gets or sets the legend title color.
        /// </summary>
        public Color LegendTitleColor
        {
            get => (Color)this.GetValue(LegendTitleColorProperty);
            set => this.SetValue(LegendTitleColorProperty, value);
        }

        /// <summary>
        /// Gets or sets the legend title font.
        /// </summary>
        public string LegendTitleFont
        {
            get => (string)this.GetValue(LegendTitleFontProperty);
            set => this.SetValue(LegendTitleFontProperty, value);
        }

        /// <summary>
        /// Gets or sets the legend title font size.
        /// </summary>
        public double LegendTitleFontSize
        {
            get => (double)this.GetValue(LegendTitleFontSizeProperty);
            set => this.SetValue(LegendTitleFontSizeProperty, value);
        }

        /// <summary>
        /// Gets or sets the legend title font weight.
        /// </summary>
        public FontWeight LegendTitleFontWeight
        {
            get => (FontWeight)this.GetValue(LegendTitleFontWeightProperty);
            set => this.SetValue(LegendTitleFontWeightProperty, value);
        }

        /// <summary>
        /// Gets or sets the legend column spacing.
        /// </summary>
        public double LegendColumnSpacing
        {
            get => (double)this.GetValue(LegendColumnSpacingProperty);
            set => this.SetValue(LegendColumnSpacingProperty, value);
        }

        /// <summary>
        /// Gets or sets the legend font.
        /// </summary>
        public string LegendFont
        {
            get => (string)this.GetValue(LegendFontProperty);
            set => this.SetValue(LegendFontProperty, value);
        }

        /// <summary>
        /// Gets or sets the legend font size.
        /// </summary>
        public double LegendFontSize
        {
            get => (double)this.GetValue(LegendFontSizeProperty);
            set => this.SetValue(LegendFontSizeProperty, value);
        }

        /// <summary>
        /// Gets or sets the legend font weight.
        /// </summary>
        public FontWeight LegendFontWeight
        {
            get => (FontWeight)this.GetValue(LegendFontWeightProperty);
            set => this.SetValue(LegendFontWeightProperty, value);
        }

        // XGoodFit port: WPF's LogicalChildren enumerator (annotations, axes, then series) isn't ported:
        // Avalonia keeps the logical children in a list, which SyncLogicalTree maintains.

        /// <summary>
        /// Invalidates the plot and updates the model.
        /// </summary>
        /// <param name="updateData">Whether to update data.</param>
        /// <remarks>
        /// <para>
        /// When <see cref="Plot.SuppressPropertyChanged"/> is set on this plot, all invalidation
        /// work is deferred. This gate catches not only the Plot's own direct InvalidatePlot calls
        /// but also the cascading calls that flow back in from child Series. There are two common
        /// cascades that previously bypassed suppression and produced the multi-second lag on
        /// many-series plots (e.g. a 20-chain MCMC trace):
        /// </para>
        /// <list type="number">
        /// <item>
        /// <description>
        /// <b>Logical-tree inherited-DP cascade:</b> adding a Series wrapper to <see cref="Series"/>
        /// causes WPF to re-evaluate every inherited DependencyProperty (Visibility, Background,
        /// FontFamily, FontSize, FontWeight, Foreground). Each change fires <c>AppearanceChanged</c>
        /// on the wrapper, which calls back into Plot.InvalidatePlot. For N series × 6 inherited
        /// DPs = 6N synchronous Model.Update calls during bulk Series.Add.
        /// </description>
        /// </item>
        /// <item>
        /// <description>
        /// <b>ItemsSource-per-series cascade:</b> assigning ItemsSource on each wrapper fires
        /// <c>OnItemsSourceChanged</c> → <c>OnDataChanged</c> → InvalidatePlot(true). For N series
        /// assignments that's N full Model.Update(true) cycles.
        /// </description>
        /// </item>
        /// </list>
        /// <para>
        /// The gate marks <c>_needsSynchronization = true</c> so the final explicit
        /// <c>InvalidatePlot(true)</c> (issued after <see cref="Series.SuppressPropertyChanged"/>
        /// is cleared) runs the full sync, but skips the ~120 redundant Update calls during the
        /// bulk-update window. Consumers MUST pair <c>SuppressPropertyChanged=true</c> with an
        /// explicit <c>InvalidatePlot(...)</c> after clearing — otherwise the plot state may be
        /// inconsistent until the next user interaction.
        /// </para>
        /// <para>
        /// The <paramref name="updateData"/> flag is preserved across the suppression window:
        /// any suppressed call with <c>updateData=true</c> sets a private pending flag that
        /// promotes the next non-gated call to <c>updateData=true</c>, even if the consumer's
        /// final flush passes <c>updateData=false</c>. This guarantees a data refresh requested
        /// during the suppression window is never silently dropped.
        /// </para>
        /// </remarks>
        public override void InvalidatePlot(bool updateData = true)
        {
            if (this.SuppressPropertyChanged)
            {
                this._needsSynchronization = true;
                // Preserve a deferred data-refresh request across the suppression window.
                // Without this, a suppressed InvalidatePlot(true) would be silently lost
                // when the consumer ends suppression with InvalidatePlot(false).
                if (updateData)
                {
                    this._pendingUpdateData = true;
                }
                return;
            }

            // Consume any data-refresh request that was deferred during a suppression window.
            // This must happen before the sync block so the consolidated updateData flag
            // controls both Synchronize* and base.InvalidatePlot below.
            if (this._pendingUpdateData)
            {
                updateData = true;
                this._pendingUpdateData = false;
            }

#if DEBUG
            // Per-phase timing to locate the real bottleneck when the aggregate InvalidatePlot
            // is slower than expected. Gated on the same flag as the outer diagnostic.
            bool diagnose = InvalidatePlotDiagnosticsEnabled && InvalidatePlotPhaseDiagnosticsEnabled
                            && this.ActualModel != null
                            && (string.IsNullOrEmpty(InvalidatePlotDiagnosticsTitleFilter)
                                || (this.ActualModel.Title ?? string.Empty).IndexOf(
                                        InvalidatePlotDiagnosticsTitleFilter,
                                        System.StringComparison.OrdinalIgnoreCase) >= 0);
            long t0 = 0, tSync = 0, tBase = 0;
            bool ranSync = false;
            bool entryNeedsSync = this._needsSynchronization;
            int gc0_0 = 0, gc1_0 = 0, gc2_0 = 0;
            if (diagnose)
            {
                t0 = System.Diagnostics.Stopwatch.GetTimestamp();
                gc0_0 = System.GC.CollectionCount(0);
                gc1_0 = System.GC.CollectionCount(1);
                gc2_0 = System.GC.CollectionCount(2);
                OxyPlot.PlotDiagnostics.Log("Plot.InvalidatePlot ENTER");
            }
#endif

            // Dispatch synchronization by per-collection dirty flags. _needsSynchronization
            // (legacy "force full sync" flag) and updateData=true both promote to "sync
            // everything" — preserves backward compatibility with external callers that set
            // _needsSynchronization directly. Otherwise only the dirty collections re-sync,
            // which on a 20-series plot means a single Series.Color change runs only
            // SynchronizeSeries (~20 CreateModel calls) instead of all four sync methods
            // (~22 CreateModel calls plus Plot-property and legend copy work).
            bool forceAll = this._needsSynchronization || updateData;
            bool needSyncProps = forceAll || this._propertiesDirty;
            bool needSyncSeries = forceAll || this._seriesDirty;
            bool needSyncAxes = forceAll || this._axesDirty;
            bool needSyncAnnotations = forceAll || this._annotationsDirty;

            if (needSyncProps || needSyncSeries || needSyncAxes || needSyncAnnotations)
            {
                if (needSyncProps) this.SynchronizeProperties();
                if (needSyncSeries) this.SynchronizeSeries();
                if (needSyncAxes) this.SynchronizeAxes();
                if (needSyncAnnotations) this.SynchronizeAnnotations();

                this._needsSynchronization = false;
                this._propertiesDirty = false;
                this._seriesDirty = false;
                this._axesDirty = false;
                this._annotationsDirty = false;
#if DEBUG
                ranSync = true;
#endif
            }

#if DEBUG
            if (diagnose) tSync = System.Diagnostics.Stopwatch.GetTimestamp();
#endif

            base.InvalidatePlot(updateData);

#if DEBUG
            if (diagnose)
            {
                tBase = System.Diagnostics.Stopwatch.GetTimestamp();
                double ticksPerMs = System.Diagnostics.Stopwatch.Frequency / 1000.0;
                double syncMs = (tSync - t0) / ticksPerMs;
                double baseMs = (tBase - tSync) / ticksPerMs;

                // GC counter delta — catches a stop-the-world collection eating wall-clock time
                // outside the InvalidatePlot pipeline.
                int gc0_d = System.GC.CollectionCount(0) - gc0_0;
                int gc1_d = System.GC.CollectionCount(1) - gc1_0;
                int gc2_d = System.GC.CollectionCount(2) - gc2_0;

                // Render-complete measurement: ContextIdle fires after the dispatcher has drained
                // the full render. Total includes the WPF composition cost.
                var paintSw = System.Diagnostics.Stopwatch.StartNew();
                long callId = System.Threading.Interlocked.Increment(ref _invalidatePhaseCallCounter);
                string phaseTag = $"sync={(ranSync ? "Y" : "N")} entryNeedsSync={(entryNeedsSync ? "Y" : "N")} updateData={(updateData ? "T" : "F")}";
                Dispatcher.UIThread.Post(
                    new System.Action(() =>
                    {
                        paintSw.Stop();
                        System.Diagnostics.Debug.WriteLine(
                            $"[InvalidatePhase #{callId,6}] {phaseTag} syncMs={syncMs,6:F2} baseMs={baseMs,6:F2} paintMs={paintSw.ElapsedMilliseconds,5} gc0={gc0_d} gc1={gc1_d} gc2={gc2_d}");
                    }),
                    DispatcherPriority.ContextIdle);

                // First-frame measurement: hook CompositionTarget.Rendering exactly once. This fires
                // on the next WPF render-tree commit, which is when the user actually sees pixels
                // change. If this is large but paintMs is small, the slowness lives in the WPF
                // visual tree commit / DWM compositor, not the OxyPlot pipeline.
                // Coalesce: if a previous InvalidatePlot already subscribed a handler that hasn't
                // fired yet (rapid 60Hz wheel zoom outpaces CompositionTarget.Rendering), skip
                // re-subscribing. Otherwise N stale handlers accumulate and fire simultaneously
                // on the next render tick, producing N redundant log lines and adding subscribe
                // overhead per call.
                // XGoodFit port: the next frame is the top level's next animation frame (Avalonia's
                // CompositionTarget.Rendering), which needs no unsubscribing.
                var topLevel = TopLevel.GetTopLevel(this);
                if (!_firstFrameHandlerPending && topLevel != null)
                {
                    _firstFrameHandlerPending = true;
                    long invalidateEntryTicks = t0;
                    int captureCallId = (int)callId;
                    topLevel.RequestAnimationFrame(_ =>
                    {
                        long now = System.Diagnostics.Stopwatch.GetTimestamp();
                        double firstFrameMs = (now - invalidateEntryTicks) * 1000.0 / System.Diagnostics.Stopwatch.Frequency;
                        System.Diagnostics.Debug.WriteLine(
                            $"[FirstFrame #{captureCallId,6}] msFromInvalidate={firstFrameMs,7:F2}");
                        _firstFrameHandlerPending = false;
                    });
                }

                OxyPlot.PlotDiagnostics.Log($"Plot.InvalidatePlot EXIT (renderQueued, callId={callId})");
            }
#endif
        }

#if DEBUG
        private static bool _invalidatePlotPhaseDiagnosticsEnabled;

        /// <summary>
        /// When true and <see cref="OxyPlot.Wpf.PlotViewBase.InvalidatePlotDiagnosticsEnabled"/>
        /// is also true, logs per-phase timing for every <see cref="InvalidatePlot"/> call:
        /// synchronization cost, Model.Update + render-queue cost, render-complete cost (via a
        /// ContextIdle callback), first-frame cost (via <c>CompositionTarget.Rendering</c>), GC
        /// counter delta, and a full wheel-zoom stack trace via <see cref="OxyPlot.PlotDiagnostics"/>.
        /// </summary>
        public static bool InvalidatePlotPhaseDiagnosticsEnabled
        {
            get => _invalidatePlotPhaseDiagnosticsEnabled;
            set
            {
                _invalidatePlotPhaseDiagnosticsEnabled = value;
                // Mirror to the core-side flag so the wheel-stack trace activates with the same toggle.
                OxyPlot.PlotDiagnostics.WheelTraceEnabled = value;
            }
        }

        private static long _invalidatePhaseCallCounter;

        /// <summary>
        /// True between subscribing a CompositionTarget.Rendering handler in InvalidatePlot's
        /// debug diagnostics and that handler firing+unsubscribing. Coalesces rapid InvalidatePlot
        /// calls (faster than 60Hz render tick) into a single subscribed handler so the log
        /// shows one [FirstFrame] line per render, not N stale lines from N redundant handlers.
        /// Per-instance so two Plot controls in the same process don't share state — otherwise
        /// a Rendering tick fired for one plot would clear the other's pending flag and produce
        /// misleading diagnostics.
        /// </summary>
        private bool _firstFrameHandlerPending;
#endif

        /// <summary>
        /// Called when visual appearance changes.
        /// </summary>
        /// <param name="d">The dependency object that changed.</param>
        /// <param name="e">The event arguments containing property change information.</param>
        private static void AppearanceChanged(AvaloniaObject d, AvaloniaPropertyChangedEventArgs e)
        {
            var plot = (Plot)d;
            if (plot.SuppressPropertyChanged) return;
            // A Plot-level DP changed (background, title, legend properties, etc.).
            // Only Plot-level properties need re-syncing; series, axes, and annotations are
            // unaffected. Setting only _propertiesDirty avoids re-creating their model
            // wrappers on every appearance tweak.
            plot._propertiesDirty = true;
            plot.InvalidatePlot(false);
            plot.OnPropertyChanged(e.Property.Name);
        }

        /// <summary>
        /// Called when the series collection changes. Subscribes/unsubscribes to item
        /// PropertyChanged events and fires Plot-level INPC with property name "Series".
        /// </summary>
        private void OnSeriesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            this.UpdateItemSubscriptions(e, this.subscribedSeries);
            this.SyncLogicalTree(e);
            this._seriesDirty = true;
            if (this.SuppressPropertyChanged) return;
            this.InvalidatePlot();
            this.OnPropertyChanged("Series");
        }

        /// <summary>
        /// Called when the axes collection changes. Subscribes/unsubscribes to item
        /// PropertyChanged events and fires Plot-level INPC with property name "Axes".
        /// </summary>
        private void OnAxesChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            this.UpdateItemSubscriptions(e, this.subscribedAxes);
            this.SyncLogicalTree(e);
            this._axesDirty = true;
            if (this.SuppressPropertyChanged) return;
            this.InvalidatePlot();
            this.OnPropertyChanged("Axes");
        }

        /// <summary>
        /// Called when the annotations collection changes. Subscribes/unsubscribes to item
        /// PropertyChanged events and fires Plot-level INPC with property name "Annotations".
        /// </summary>
        private void OnAnnotationsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            this.UpdateItemSubscriptions(e, this.subscribedAnnotations);
            this.SyncLogicalTree(e);
            this._annotationsDirty = true;
            if (this.SuppressPropertyChanged) return;
            this.InvalidatePlot();
            this.OnPropertyChanged("Annotations");
        }

        /// <summary>
        /// Notifies subscribers that annotation properties have been modified in bulk.
        /// Call after annotation modifications performed with
        /// <see cref="Annotation.SuppressPropertyChanged"/> enabled, so that undo bridges
        /// can rebuild their shadow state to match the current property values.
        /// </summary>
        public void NotifyAnnotationsModified()
        {
            this.OnPropertyChanged("Annotations");
        }

        /// <summary>
        /// Replaces an axis in the <see cref="Axes"/> collection, preserving its position,
        /// and fires the <see cref="AxisReplaced"/> event.
        /// </summary>
        /// <param name="oldAxis">The axis to remove.</param>
        /// <param name="newAxis">The axis to insert at the same position.</param>
        /// <remarks>
        /// Use this method instead of manual Remove/Add when changing axis types so that
        /// the undo system can record the replacement as a single undoable action.
        /// </remarks>
        public void ReplaceAxis(Axis oldAxis, Axis newAxis)
        {
            int index = this.Axes.IndexOf(oldAxis);
            if (index < 0) return;
            this.Axes.RemoveAt(index);
            this.Axes.Insert(index, newAxis);
            this.AxisReplaced?.Invoke(oldAxis, newAxis);
        }

        /// <summary>
        /// Updates PropertyChanged subscriptions for items in a collection based on the change event.
        /// On Reset (Clear), unsubscribes from all tracked items and clears the shadow list.
        /// On Add/Remove/Replace, subscribes to new items and unsubscribes from old items.
        /// </summary>
        /// <typeparam name="T">The item type, which must implement INotifyPropertyChanged.</typeparam>
        /// <param name="e">The collection change event args.</param>
        /// <param name="subscribedItems">The shadow list tracking subscribed items.</param>
        private void UpdateItemSubscriptions<T>(NotifyCollectionChangedEventArgs e, List<T> subscribedItems)
            where T : INotifyPropertyChanged
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                foreach (var item in subscribedItems)
                {
                    item.PropertyChanged -= this.OnCollectionItemPropertyChanged;
                }

                subscribedItems.Clear();
            }
            else
            {
                if (e.OldItems != null)
                {
                    foreach (T item in e.OldItems)
                    {
                        item.PropertyChanged -= this.OnCollectionItemPropertyChanged;
                        subscribedItems.Remove(item);
                    }
                }

                if (e.NewItems != null)
                {
                    foreach (T item in e.NewItems)
                    {
                        item.PropertyChanged += this.OnCollectionItemPropertyChanged;
                        subscribedItems.Add(item);
                    }
                }
            }
        }

        /// <summary>
        /// Relays PropertyChanged events from collection items through the Plot's own
        /// PropertyChanged event. The property name is prefixed with the item type
        /// (e.g., "Annotation.Color", "Series.Title", "Axis.Minimum").
        /// </summary>
        private void OnCollectionItemPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Dispatch sync work to only the affected collection. A Series.Color edit, for
            // example, no longer triggers SynchronizeAxes + SynchronizeAnnotations.
            string prefix;
            if (sender is Annotation)
            {
                this._annotationsDirty = true;
                prefix = "Annotation";
            }
            else if (sender is Series)
            {
                this._seriesDirty = true;
                prefix = "Series";
            }
            else if (sender is Axis)
            {
                this._axesDirty = true;
                prefix = "Axis";
            }
            else
            {
                // Unknown sender — fall back to the conservative full-sync path.
                this._needsSynchronization = true;
                prefix = "Item";
            }

            this.OnPropertyChanged(prefix + "." + e.PropertyName);
        }

        /// <summary>
        /// Synchronizes the logical tree with collection changes.
        /// </summary>
        /// <remarks>XGoodFit port: Avalonia keeps the logical children in a list, where WPF enumerated the
        /// three collections (and only set each item's logical parent). So an item is added once, and when
        /// a collection is cleared (a reset, which lists no old items) the items no longer in any of the
        /// collections are removed from the list, which also clears their logical parent.</remarks>
        private void SyncLogicalTree(NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (var item in e.NewItems)
                {
                    if (!this.LogicalChildren.Contains((ILogical)item))
                    {
                        this.LogicalChildren.Add((ILogical)item);
                    }
                }
            }

            if (e.OldItems != null)
            {
                foreach (var item in e.OldItems)
                {
                    this.LogicalChildren.Remove((ILogical)item);
                }
            }

            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                var current = new HashSet<object>(this.Series.Cast<object>().Concat(this.Axes).Concat(this.Annotations));
                var stale = this.LogicalChildren.Where(c => (c is Series || c is Axis || c is Annotation) && !current.Contains(c)).ToList();
                foreach (var child in stale)
                {
                    this.LogicalChildren.Remove(child);
                }
            }
        }

        /// <summary>
        /// Synchronizes the plot properties to the internal model.
        /// </summary>
        private void SynchronizeProperties()
        {
            var m = this.internalModel;

            m.Title = this.Title;
            m.Subtitle = this.Subtitle;
            m.TitleColor = this.TitleColor.ToOxyColor();
            m.SubtitleColor = this.SubtitleColor.ToOxyColor();
            m.TitleFont = this.TitleFont;
            m.TitleFontSize = this.TitleFontSize;
            m.TitleFontWeight = this.TitleFontWeight.ToOpenTypeWeight();
            m.TitlePadding = this.TitlePadding;
            m.SubtitleFont = this.SubtitleFont;
            m.SubtitleFontSize = this.SubtitleFontSize;
            m.SubtitleFontWeight = this.SubtitleFontWeight.ToOpenTypeWeight();
            m.TextColor = this.TextColor.ToOxyColor();
            m.Background = this.Background.ToOxyColor();
            m.PlotAreaBackground = this.PlotAreaBackground.ToOxyColor();
            m.PlotAreaBorderColor = this.PlotAreaBorderColor.ToOxyColor();
            m.PlotAreaBorderThickness = this.PlotAreaBorderThickness.ToOxyThickness();
            m.Padding = this.Padding.ToOxyThickness();
            m.IsLegendVisible = this.IsLegendVisible;

            // Synchronize legend
            this.SynchronizeLegend();
        }

        /// <summary>
        /// Synchronizes the legend properties to the internal model.
        /// </summary>
        private void SynchronizeLegend()
        {
            var m = this.internalModel;

            // Get or create the default legend
            var legend = m.Legends.FirstOrDefault() as Legend;
            if (legend == null)
            {
                legend = new Legend();
                m.Legends.Add(legend);
            }

            // Synchronize legend properties
            legend.IsLegendVisible = this.IsLegendVisible;
            legend.LegendBackground = this.LegendBackground.ToOxyColor();
            legend.LegendBorder = this.LegendBorder.ToOxyColor();
            legend.LegendBorderThickness = this.LegendBorderThickness;
            legend.LegendItemAlignment = this.LegendItemAlignment.ToHorizontalAlignment();
            legend.LegendItemClickTogglesSeriesVisibility = this.LegendItemClickTogglesSeriesVisibility;
            legend.LegendItemOrder = this.LegendItemOrder;
            legend.LegendItemSpacing = this.LegendItemSpacing;
            legend.LegendLineSpacing = this.LegendLineSpacing;
            legend.LegendMargin = this.LegendMargin;
            legend.LegendMaxHeight = this.LegendMaxHeight;
            legend.LegendMaxWidth = this.LegendMaxWidth;
            legend.LegendOrientation = this.LegendOrientation;
            legend.LegendPadding = this.LegendPadding;
            legend.LegendPlacement = this.LegendPlacement;
            legend.LegendPosition = this.LegendPosition;
            legend.LegendSymbolLength = this.LegendSymbolLength;
            legend.LegendSymbolMargin = this.LegendSymbolMargin;
            legend.LegendSymbolPlacement = this.LegendSymbolPlacement;
            legend.LegendTextColor = this.LegendTextColor.ToOxyColor();
            legend.LegendTitle = this.LegendTitle;
            legend.LegendTitleColor = this.LegendTitleColor.ToOxyColor();
            legend.LegendTitleFont = this.LegendTitleFont;
            legend.LegendTitleFontSize = this.LegendTitleFontSize;
            legend.LegendTitleFontWeight = this.LegendTitleFontWeight.ToOpenTypeWeight();
            legend.LegendColumnSpacing = this.LegendColumnSpacing;
            legend.LegendFont = this.LegendFont;
            legend.LegendFontSize = this.LegendFontSize;
            legend.LegendFontWeight = this.LegendFontWeight.ToOpenTypeWeight();
        }

        /// <summary>
        /// Synchronizes the series collection to the internal model.
        /// </summary>
        private void SynchronizeSeries()
        {
            this.internalModel.Series.Clear();
            foreach (var s in this.Series)
            {
                var model = s.CreateModel();
                if (model != null)
                {
                    this.internalModel.Series.Add(model);
                }
            }
        }

        /// <summary>
        /// Synchronizes the axes collection to the internal model.
        /// </summary>
        private void SynchronizeAxes()
        {
            this.internalModel.Axes.Clear();
            foreach (var a in this.Axes)
            {
                var model = a.CreateModel();
                if (model != null)
                {
                    this.internalModel.Axes.Add(model);
                }
            }
        }

        /// <summary>
        /// Synchronizes the annotations collection to the internal model.
        /// </summary>
        private void SynchronizeAnnotations()
        {
            this.internalModel.Annotations.Clear();
            foreach (var a in this.Annotations)
            {
                var model = a.CreateModel();
                if (model != null)
                {
                    this.internalModel.Annotations.Add(model);
                }
            }
        }
    }
}
