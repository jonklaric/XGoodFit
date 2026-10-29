// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AnnotationSerializer.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Provides serialization and deserialization of OxyPlot annotation properties to and from XML.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

#nullable enable annotations
#nullable disable warnings

namespace OxyPlot.Wpf.Serialization
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Windows;
    using System.Windows.Media;
    using System.Xml.Linq;
    using OxyPlot;
    using static OxyPlot.Wpf.Serialization.PlotSerializer;

    /// <summary>
    /// Provides serialization and deserialization of OxyPlot annotation properties to and from XML.
    /// Handles all annotation types including Arrow, Text, Ellipse, Rectangle, Point, Polygon,
    /// Line, Polyline, and Function annotations.
    /// </summary>
    public static class AnnotationSerializer
    {
        /// <summary>
        /// Visual property names on <see cref="Annotation"/> that are serialized for settings persistence.
        /// Includes properties common to all annotation types plus type-specific properties.
        /// Consumers can use this list to monitor these properties for change tracking.
        /// </summary>
        public static readonly IReadOnlyList<string> AnnotationVisualProperties = new[]
        {
            // General (Annotation base)
            "Name", "IsEnabled", "Layer", "XAxisKey", "YAxisKey",
            // TextualAnnotation
            "Text", "TextColor", "FontFamily", "FontSize", "FontWeight",
            "TextPosition", "TextRotation", "TextHorizontalAlignment", "TextVerticalAlignment",
            // ArrowAnnotation
            "Color", "ArrowDirection", "StartPoint", "EndPoint",
            "HeadLength", "HeadWidth", "Veeness", "LineJoin", "LineStyle", "StrokeThickness",
            // TextAnnotation
            "Background", "Offset", "Padding", "Stroke",
            // ShapeAnnotation
            "Fill",
            // EllipseAnnotation / RectangleAnnotation
            "MinimumX", "MaximumX", "MinimumY", "MaximumY",
            // EllipseAnnotation specific
            "X", "Y", "Width", "Height",
            // PointAnnotation
            "Size", "TextMargin", "Shape",
            // PolygonAnnotation
            "MinimumSegmentLength",
            // PathAnnotation
            "ClipByXAxis", "ClipByYAxis", "ClipText", "TextOrientation", "TextLinePosition",
            // LineAnnotation
            "Type", "Intercept", "Slope",
            // FunctionAnnotation
            "Resolution",
        };

        /// <summary>
        /// Serializes all annotations from a <see cref="Plot"/> to an <see cref="XElement"/>.
        /// </summary>
        /// <param name="plot">The plot containing annotations to serialize.</param>
        /// <returns>An <see cref="XElement"/> containing all serialized annotations.</returns>
        /// <remarks>
        /// Only <see cref="TextualAnnotation"/>-derived annotations are serialized. Non-textual
        /// annotations such as <c>ImageAnnotation</c> are skipped with a diagnostic trace —
        /// previously they were silently dropped on Save/Open. Full round-trip support for
        /// <c>ImageAnnotation</c> requires base64-encoding the <c>OxyImage</c> blob and is
        /// tracked as a follow-up; the diagnostic ensures the loss is no longer invisible.
        /// </remarks>
        public static XElement AnnotationsToXElement(Plot plot)
        {
            var annotationProperties = new XElement(AnnotationsPropertiesTag);
            foreach (var annotation in plot.Annotations)
            {
                var textualAnnotation = annotation as TextualAnnotation;
                if (textualAnnotation == null)
                {
                    System.Diagnostics.Trace.WriteLine(
                        $"[AnnotationSerializer] Annotation of type '{annotation?.GetType().FullName}' is not a TextualAnnotation and will not be persisted. " +
                        "Save/Open will lose this annotation. (Issue D-001 — full round-trip pending.)");
                    continue;
                }
                annotationProperties.Add(AnnotationToXElement(textualAnnotation));
            }

            return annotationProperties;
        }

        /// <summary>
        /// Deserializes annotations from an <see cref="XElement"/> and applies them to the plot.
        /// </summary>
        /// <param name="plot">The plot to add deserialized annotations to.</param>
        /// <param name="element">The <see cref="XElement"/> containing serialized annotations.</param>
        public static void XElementToAnnotations(Plot plot, XElement element)
        {
            // Early Exit
            if (element.Name != AnnotationsPropertiesTag) return;

            // Set up the annotations
            plot.Annotations.Clear();
            Annotation? tempAnnotation;
            foreach (var el in element.Elements(AnnotationPropertiesTag))
            {
                tempAnnotation = XElementToAnnotation(el);
                if (tempAnnotation == null) continue;
                plot.Annotations.Add(tempAnnotation);
            }
        }

        /// <summary>
        /// Serializes a single <see cref="TextualAnnotation"/> to an <see cref="XElement"/>.
        /// </summary>
        /// <param name="annotation">The annotation to serialize.</param>
        /// <returns>An <see cref="XElement"/> containing the serialized annotation properties.</returns>
        public static XElement AnnotationToXElement(TextualAnnotation annotation)
        {
            var annotationProperties = new XElement(AnnotationPropertiesTag);
            var annotationType = annotation.GetType();
            annotationProperties.SetAttributeValue("AnnotationType", annotationType.ToString());

            // Annotation Properties
            var generalProperties = new XElement("General");
            generalProperties.SetAttributeValue(nameof(annotation.Name), annotation.Name ?? "");
            generalProperties.SetAttributeValue(nameof(annotation.IsEnabled), annotation.IsEnabled.ToString());
            generalProperties.SetAttributeValue(nameof(annotation.Layer), annotation.Layer.ToString());
            generalProperties.SetAttributeValue(nameof(annotation.XAxisKey), annotation.XAxisKey);
            generalProperties.SetAttributeValue(nameof(annotation.YAxisKey), annotation.YAxisKey);
            annotationProperties.Add(generalProperties);

            // Textual Properties
            var weightConverter = new FontWeightConverter();
            var ffc = new System.Windows.Media.FontFamilyConverter();
            var textualProperties = new XElement("Textual");
            textualProperties.SetAttributeValue(nameof(annotation.Text), annotation.Text);
            textualProperties.SetAttributeValue(nameof(annotation.TextColor), annotation.TextColor.ToString());
            if (annotation.FontFamily != null) textualProperties.SetAttributeValue(nameof(annotation.FontFamily), ffc.ConvertToInvariantString(annotation.FontFamily));
            textualProperties.SetAttributeValue(nameof(annotation.FontSize), annotation.FontSize.ToString("G17", CultureInfo.InvariantCulture));
            textualProperties.SetAttributeValue(nameof(annotation.FontWeight), weightConverter.ConvertToInvariantString(annotation.FontWeight));
            textualProperties.SetAttributeValue(nameof(annotation.TextPosition), annotation.TextPosition.ToPrettyText());
            textualProperties.SetAttributeValue(nameof(annotation.TextRotation), annotation.TextRotation.ToString("G17", CultureInfo.InvariantCulture));
            textualProperties.SetAttributeValue(nameof(annotation.TextHorizontalAlignment), annotation.TextHorizontalAlignment.ToString());
            textualProperties.SetAttributeValue(nameof(annotation.TextVerticalAlignment), annotation.TextVerticalAlignment.ToString());
            annotationProperties.Add(textualProperties);

            // Must be either arrow (concrete), text (concrete), shape (abstract), or path (abstract)
            if (annotationType == typeof(ArrowAnnotation))
            {
                var arrowAnnotation = (ArrowAnnotation)annotation;
                var arrowProperties = new XElement("Arrow");
                arrowProperties.SetAttributeValue(nameof(arrowAnnotation.Color), arrowAnnotation.Color.ToString());
                arrowProperties.SetAttributeValue(nameof(arrowAnnotation.ArrowDirection), arrowAnnotation.ArrowDirection.ToPrettyText());
                arrowProperties.SetAttributeValue(nameof(arrowAnnotation.StartPoint), arrowAnnotation.StartPoint.ToPrettyText());
                arrowProperties.SetAttributeValue(nameof(arrowAnnotation.EndPoint), arrowAnnotation.EndPoint.ToPrettyText());
                arrowProperties.SetAttributeValue(nameof(arrowAnnotation.HeadLength), arrowAnnotation.HeadLength.ToString("G17", CultureInfo.InvariantCulture));
                arrowProperties.SetAttributeValue(nameof(arrowAnnotation.HeadWidth), arrowAnnotation.HeadWidth.ToString("G17", CultureInfo.InvariantCulture));
                arrowProperties.SetAttributeValue(nameof(arrowAnnotation.Veeness), arrowAnnotation.Veeness.ToString("G17", CultureInfo.InvariantCulture));
                arrowProperties.SetAttributeValue(nameof(arrowAnnotation.LineJoin), arrowAnnotation.LineJoin.ToString());
                arrowProperties.SetAttributeValue(nameof(arrowAnnotation.LineStyle), arrowAnnotation.LineStyle.ToString());
                arrowProperties.SetAttributeValue(nameof(arrowAnnotation.StrokeThickness), arrowAnnotation.StrokeThickness.ToString("G17", CultureInfo.InvariantCulture));
                textualProperties.Add(arrowProperties);
            }
            else if (annotationType == typeof(TextAnnotation))
            {
                var thickConvert = new ThicknessConverter();
                var textAnnotation = (TextAnnotation)annotation;
                var textProperties = new XElement("Text");
                textProperties.SetAttributeValue(nameof(textAnnotation.Background), textAnnotation.Background.ToString());
                textProperties.SetAttributeValue(nameof(textAnnotation.Offset), textAnnotation.Offset.ToPrettyText());
                textProperties.SetAttributeValue(nameof(textAnnotation.Padding), thickConvert.ConvertToInvariantString(textAnnotation.Padding));
                textProperties.SetAttributeValue(nameof(textAnnotation.Stroke), textAnnotation.Stroke.ToString());
                textProperties.SetAttributeValue(nameof(textAnnotation.StrokeThickness), textAnnotation.StrokeThickness.ToString("G17", CultureInfo.InvariantCulture));
                textualProperties.Add(textProperties);
            }

            // Must be shape or path annotation
            var shapeAnnotation = annotation as ShapeAnnotation;
            if (shapeAnnotation != null)
            {
                var shapeProperties = new XElement("Shape");
                shapeProperties.SetAttributeValue(nameof(shapeAnnotation.Fill), shapeAnnotation.Fill.ToString());
                shapeProperties.SetAttributeValue(nameof(shapeAnnotation.Stroke), shapeAnnotation.Stroke.ToString());
                shapeProperties.SetAttributeValue(nameof(shapeAnnotation.StrokeThickness), shapeAnnotation.StrokeThickness.ToString("G17", CultureInfo.InvariantCulture));
                textualProperties.Add(shapeProperties);

                // Must be Ellipse, Point, Rectangle, or Polygon Annotations (all concrete)
                if (annotationType == typeof(EllipseAnnotation))
                {
                    var ellipseAnnotation = (EllipseAnnotation)shapeAnnotation;
                    var ellipseProperties = new XElement("Ellipse");
                    // Serialize both Min/Max (backward compat) and X/Y/Width/Height (authoritative DPs)
                    ellipseProperties.SetAttributeValue(nameof(ellipseAnnotation.MinimumX), ellipseAnnotation.MinimumX.ToString("G17", CultureInfo.InvariantCulture));
                    ellipseProperties.SetAttributeValue(nameof(ellipseAnnotation.MaximumY), ellipseAnnotation.MaximumY.ToString("G17", CultureInfo.InvariantCulture));
                    ellipseProperties.SetAttributeValue(nameof(ellipseAnnotation.MaximumX), ellipseAnnotation.MaximumX.ToString("G17", CultureInfo.InvariantCulture));
                    ellipseProperties.SetAttributeValue(nameof(ellipseAnnotation.MinimumY), ellipseAnnotation.MinimumY.ToString("G17", CultureInfo.InvariantCulture));
                    ellipseProperties.SetAttributeValue(nameof(ellipseAnnotation.X), ellipseAnnotation.X.ToString("G17", CultureInfo.InvariantCulture));
                    ellipseProperties.SetAttributeValue(nameof(ellipseAnnotation.Y), ellipseAnnotation.Y.ToString("G17", CultureInfo.InvariantCulture));
                    ellipseProperties.SetAttributeValue(nameof(ellipseAnnotation.Width), ellipseAnnotation.Width.ToString("G17", CultureInfo.InvariantCulture));
                    ellipseProperties.SetAttributeValue(nameof(ellipseAnnotation.Height), ellipseAnnotation.Height.ToString("G17", CultureInfo.InvariantCulture));
                    shapeProperties.Add(ellipseProperties);
                }
                else if (annotationType == typeof(RectangleAnnotation))
                {
                    var rectangleAnnotation = (RectangleAnnotation)shapeAnnotation;
                    var rectangleProperties = new XElement("Rectangle");
                    rectangleProperties.SetAttributeValue(nameof(rectangleAnnotation.MinimumX), rectangleAnnotation.MinimumX.ToString("G17", CultureInfo.InvariantCulture));
                    rectangleProperties.SetAttributeValue(nameof(rectangleAnnotation.MaximumY), rectangleAnnotation.MaximumY.ToString("G17", CultureInfo.InvariantCulture));
                    rectangleProperties.SetAttributeValue(nameof(rectangleAnnotation.MaximumX), rectangleAnnotation.MaximumX.ToString("G17", CultureInfo.InvariantCulture));
                    rectangleProperties.SetAttributeValue(nameof(rectangleAnnotation.MinimumY), rectangleAnnotation.MinimumY.ToString("G17", CultureInfo.InvariantCulture));
                    shapeProperties.Add(rectangleProperties);
                }
                else if (annotationType == typeof(PointAnnotation))
                {
                    var pointAnnotation = (PointAnnotation)shapeAnnotation;
                    var pointProperties = new XElement("Point");
                    pointProperties.SetAttributeValue(nameof(pointAnnotation.X), pointAnnotation.X.ToString("G17", CultureInfo.InvariantCulture));
                    pointProperties.SetAttributeValue(nameof(pointAnnotation.Y), pointAnnotation.Y.ToString("G17", CultureInfo.InvariantCulture));
                    pointProperties.SetAttributeValue(nameof(pointAnnotation.Size), pointAnnotation.Size.ToString("G17", CultureInfo.InvariantCulture));
                    pointProperties.SetAttributeValue(nameof(pointAnnotation.TextMargin), pointAnnotation.TextMargin.ToString("G17", CultureInfo.InvariantCulture));
                    pointProperties.SetAttributeValue(nameof(pointAnnotation.Shape), pointAnnotation.Shape.ToString());
                    shapeProperties.Add(pointProperties);
                }
                else if (annotationType == typeof(PolygonAnnotation))
                {
                    var polygonAnnotation = (PolygonAnnotation)shapeAnnotation;
                    var polygonProperties = new XElement("Polygon");
                    polygonProperties.SetAttributeValue(nameof(polygonAnnotation.LineJoin), polygonAnnotation.LineJoin.ToString());
                    polygonProperties.SetAttributeValue(nameof(polygonAnnotation.LineStyle), polygonAnnotation.LineStyle.ToString());
                    polygonProperties.SetAttributeValue(nameof(polygonAnnotation.MinimumSegmentLength), polygonAnnotation.MinimumSegmentLength.ToString("G17", CultureInfo.InvariantCulture));
                    polygonProperties.Add(polygonAnnotation.Points.ToXElement(nameof(polygonAnnotation.Points)));
                    shapeProperties.Add(polygonProperties);
                }
            }
            else
            {
                var pathAnnotation = annotation as PathAnnotation;
                if (pathAnnotation != null)
                {
                    var pathProperties = new XElement("Path");
                    pathProperties.SetAttributeValue(nameof(pathAnnotation.Color), pathAnnotation.Color.ToString());
                    pathProperties.SetAttributeValue(nameof(pathAnnotation.ClipByXAxis), pathAnnotation.ClipByXAxis.ToString());
                    pathProperties.SetAttributeValue(nameof(pathAnnotation.ClipByYAxis), pathAnnotation.ClipByYAxis.ToString());
                    pathProperties.SetAttributeValue(nameof(pathAnnotation.ClipText), pathAnnotation.ClipText.ToString());
                    pathProperties.SetAttributeValue(nameof(pathAnnotation.LineJoin), pathAnnotation.LineJoin.ToString());
                    pathProperties.SetAttributeValue(nameof(pathAnnotation.LineStyle), pathAnnotation.LineStyle.ToString());
                    pathProperties.SetAttributeValue(nameof(pathAnnotation.StrokeThickness), pathAnnotation.StrokeThickness.ToString("G17", CultureInfo.InvariantCulture));
                    pathProperties.SetAttributeValue(nameof(pathAnnotation.TextMargin), pathAnnotation.TextMargin.ToString("G17", CultureInfo.InvariantCulture));
                    pathProperties.SetAttributeValue(nameof(pathAnnotation.TextOrientation), pathAnnotation.TextOrientation.ToString());
                    pathProperties.SetAttributeValue(nameof(pathAnnotation.TextLinePosition), pathAnnotation.TextLinePosition.ToString("G17", CultureInfo.InvariantCulture));
                    textualProperties.Add(pathProperties);

                    // Must be Line, Polyline, or Function Annotations (all concrete)
                    if (annotationType == typeof(LineAnnotation))
                    {
                        var lineAnnotation = (LineAnnotation)pathAnnotation;
                        var lineProperties = new XElement("Line");
                        lineProperties.SetAttributeValue(nameof(lineAnnotation.Type), lineAnnotation.Type.ToString());
                        lineProperties.SetAttributeValue(nameof(lineAnnotation.X), lineAnnotation.X.ToString("G17", CultureInfo.InvariantCulture));
                        lineProperties.SetAttributeValue(nameof(lineAnnotation.Y), lineAnnotation.Y.ToString("G17", CultureInfo.InvariantCulture));
                        lineProperties.SetAttributeValue(nameof(lineAnnotation.Intercept), lineAnnotation.Intercept.ToString("G17", CultureInfo.InvariantCulture));
                        lineProperties.SetAttributeValue(nameof(lineAnnotation.MinimumX), lineAnnotation.MinimumX.ToString("G17", CultureInfo.InvariantCulture));
                        lineProperties.SetAttributeValue(nameof(lineAnnotation.MaximumY), lineAnnotation.MaximumY.ToString("G17", CultureInfo.InvariantCulture));
                        lineProperties.SetAttributeValue(nameof(lineAnnotation.MaximumX), lineAnnotation.MaximumX.ToString("G17", CultureInfo.InvariantCulture));
                        lineProperties.SetAttributeValue(nameof(lineAnnotation.MinimumY), lineAnnotation.MinimumY.ToString("G17", CultureInfo.InvariantCulture));
                        lineProperties.SetAttributeValue(nameof(lineAnnotation.Slope), lineAnnotation.Slope.ToString("G17", CultureInfo.InvariantCulture));
                        pathProperties.Add(lineProperties);
                    }
                    else if (annotationType == typeof(PolylineAnnotation))
                    {
                        var polylineAnnotation = (PolylineAnnotation)pathAnnotation;
                        var polylineProperties = new XElement("Polyline");
                        polylineProperties.SetAttributeValue(nameof(polylineAnnotation.MinimumSegmentLength), polylineAnnotation.MinimumSegmentLength.ToString("G17", CultureInfo.InvariantCulture));
                        polylineProperties.Add(polylineAnnotation.Points.ToXElement(nameof(polylineAnnotation.Points)));
                        pathProperties.Add(polylineProperties);
                    }
                    else if (annotationType == typeof(FunctionAnnotation))
                    {
                        var functionAnnotation = (FunctionAnnotation)pathAnnotation;
                        var functionProperties = new XElement("Function");
                        functionProperties.SetAttributeValue(nameof(functionAnnotation.Type), functionAnnotation.Type.ToString());
                        functionProperties.SetAttributeValue(nameof(functionAnnotation.Resolution), functionAnnotation.Resolution.ToString(CultureInfo.InvariantCulture));
                        pathProperties.Add(functionProperties);
                    }
                }
            }

            return annotationProperties;
        }

        /// <summary>
        /// Deserializes a single annotation from an <see cref="XElement"/>.
        /// </summary>
        /// <param name="element">The <see cref="XElement"/> containing the serialized annotation properties.</param>
        /// <returns>A new <see cref="TextualAnnotation"/> with deserialized properties, or null if the element is invalid.</returns>
        public static TextualAnnotation? XElementToAnnotation(XElement element)
        {
            if (element.Name != AnnotationPropertiesTag) return null;

            var fontWeightConverter = new FontWeightConverter();
            var thicknessConverter = new System.Windows.ThicknessConverter();
            TextualAnnotation annotation;
            string annotationTypeString = "";

            var annotationTypeAttr = element.Attribute("AnnotationType");
            if (annotationTypeAttr != null) annotationTypeString = annotationTypeAttr.Value;

            if (annotationTypeString == typeof(ArrowAnnotation).ToString())
                annotation = new ArrowAnnotation();
            else if (annotationTypeString == typeof(TextAnnotation).ToString())
                annotation = new TextAnnotation();
            else if (annotationTypeString == typeof(EllipseAnnotation).ToString())
                annotation = new EllipseAnnotation();
            else if (annotationTypeString == typeof(RectangleAnnotation).ToString())
                annotation = new RectangleAnnotation();
            else if (annotationTypeString == typeof(PointAnnotation).ToString())
                annotation = new PointAnnotation();
            else if (annotationTypeString == typeof(PolygonAnnotation).ToString())
                annotation = new PolygonAnnotation();
            else if (annotationTypeString == typeof(LineAnnotation).ToString())
                annotation = new LineAnnotation();
            else if (annotationTypeString == typeof(PolylineAnnotation).ToString())
                annotation = new PolylineAnnotation();
            else if (annotationTypeString == typeof(FunctionAnnotation).ToString())
                annotation = new FunctionAnnotation();
            else
            {
                // Unknown annotation type. Previously fell through to `new TextAnnotation()`,
                // which silently substituted a phantom empty text annotation in the user's
                // plot. Return null instead — the caller (XElementToAnnotations) already
                // skips null entries, matching the SeriesSerializer pattern.
                System.Diagnostics.Trace.WriteLine(
                    $"[AnnotationSerializer] Unknown AnnotationType '{annotationTypeString}'; skipping.");
                return null;
            }

            // General Properties
            var generalElement = element.Element("General");
            if (generalElement != null)
            {
                if (GetStringAttribute(generalElement, nameof(annotation.Name), out var name)) annotation.Name = name;
                if (GetBooleanAttribute(generalElement, nameof(annotation.IsEnabled), out var isEnabled)) annotation.IsEnabled = isEnabled;
                if (GetEnumAttribute(generalElement, nameof(annotation.Layer), out OxyPlot.Annotations.AnnotationLayer layer)) annotation.Layer = layer;
                if (GetStringAttribute(generalElement, nameof(annotation.XAxisKey), out var xAxisKey)) annotation.XAxisKey = xAxisKey;
                if (GetStringAttribute(generalElement, nameof(annotation.YAxisKey), out var yAxisKey)) annotation.YAxisKey = yAxisKey;
            }

            // Textual Properties
            var textualElement = element.Element("Textual");
            if (textualElement != null)
            {
                var ffc = new System.Windows.Media.FontFamilyConverter();
                if (GetStringAttribute(textualElement, nameof(annotation.Text), out var text)) annotation.Text = text;
                if (GetColorAttribute(textualElement, nameof(annotation.TextColor), out var textColor)) annotation.TextColor = textColor;
                if (GetFontFamilyAttribute(textualElement, nameof(annotation.FontFamily), ffc, out var fontFamily)) annotation.FontFamily = fontFamily;
                if (GetDoubleAttribute(textualElement, nameof(annotation.FontSize), out var fontSize)) annotation.FontSize = fontSize;
                if (GetFontWeightAttribute(textualElement, nameof(annotation.FontWeight), fontWeightConverter, out var fontWeight)) annotation.FontWeight = fontWeight;
                if (GetDataPointAttribute(textualElement, nameof(annotation.TextPosition), out var textPosition)) annotation.TextPosition = textPosition;
                if (GetDoubleAttribute(textualElement, nameof(annotation.TextRotation), out var textRotation)) annotation.TextRotation = textRotation;
                if (GetStringAttribute(textualElement, nameof(annotation.TextHorizontalAlignment), out var hAlignStr) && Enum.TryParse(hAlignStr, out System.Windows.HorizontalAlignment hAlign))
                    annotation.TextHorizontalAlignment = hAlign;
                if (GetStringAttribute(textualElement, nameof(annotation.TextVerticalAlignment), out var vAlignStr) && Enum.TryParse(vAlignStr, out System.Windows.VerticalAlignment vAlign))
                    annotation.TextVerticalAlignment = vAlign;

                // Backwards Compatibility
                if (GetColorAttribute(textualElement, "Color", out textColor)) annotation.TextColor = textColor;
                if (GetFontFamilyAttribute(textualElement, "Font", ffc, out fontFamily)) annotation.FontFamily = fontFamily;
                if (GetDoubleAttribute(textualElement, "Size", out fontSize)) annotation.FontSize = fontSize;
                if (GetFontWeightAttribute(textualElement, "Weight", fontWeightConverter, out fontWeight)) annotation.FontWeight = fontWeight;
                if (GetDataPointAttribute(textualElement, "Position", out textPosition)) annotation.TextPosition = textPosition;
                if (GetDoubleAttribute(textualElement, "Rotation", out textRotation)) annotation.TextRotation = textRotation;
                if (GetStringAttribute(textualElement, "HorizontalAlignment", out hAlignStr) && Enum.TryParse(hAlignStr, out hAlign))
                    annotation.TextHorizontalAlignment = hAlign;
                if (GetStringAttribute(textualElement, "VerticalAlignment", out vAlignStr) && Enum.TryParse(vAlignStr, out vAlign))
                    annotation.TextVerticalAlignment = vAlign;
            }

            var currentAnnotationType = annotation.GetType();

            if (currentAnnotationType == typeof(ArrowAnnotation))
            {
                var arrowElement = textualElement?.Element("Arrow");
                if (arrowElement != null)
                {
                    var arrowAnnotation = (ArrowAnnotation)annotation;
                    if (GetColorAttribute(arrowElement, nameof(arrowAnnotation.Color), out var color)) arrowAnnotation.Color = color;
                    if (GetScreenVectorAttribute(arrowElement, nameof(arrowAnnotation.ArrowDirection), out var arrowDirection)) arrowAnnotation.ArrowDirection = arrowDirection;
                    if (GetDataPointAttribute(arrowElement, nameof(arrowAnnotation.StartPoint), out var startPoint)) arrowAnnotation.StartPoint = startPoint;
                    if (GetDataPointAttribute(arrowElement, nameof(arrowAnnotation.EndPoint), out var endPoint)) arrowAnnotation.EndPoint = endPoint;
                    if (GetDoubleAttribute(arrowElement, nameof(arrowAnnotation.HeadLength), out var headLength)) arrowAnnotation.HeadLength = headLength;
                    if (GetDoubleAttribute(arrowElement, nameof(arrowAnnotation.HeadWidth), out var headWidth)) arrowAnnotation.HeadWidth = headWidth;
                    if (GetDoubleAttribute(arrowElement, nameof(arrowAnnotation.Veeness), out var veeness)) arrowAnnotation.Veeness = veeness;
                    if (GetEnumAttribute(arrowElement, nameof(arrowAnnotation.LineJoin), out LineJoin lineJoin)) arrowAnnotation.LineJoin = lineJoin;
                    if (GetEnumAttribute(arrowElement, nameof(arrowAnnotation.LineStyle), out LineStyle lineStyle)) arrowAnnotation.LineStyle = lineStyle;
                    if (GetDoubleAttribute(arrowElement, nameof(arrowAnnotation.StrokeThickness), out var strokeThickness)) arrowAnnotation.StrokeThickness = strokeThickness;

                    // Backwards Compatibility
                    if (GetScreenVectorAttribute(arrowElement, "Direction", out arrowDirection)) arrowAnnotation.ArrowDirection = arrowDirection;
                    if (GetDoubleAttribute(arrowElement, "BarbLength", out veeness)) arrowAnnotation.Veeness = veeness;
                }
            }
            else if (currentAnnotationType == typeof(TextAnnotation))
            {
                var textElement = textualElement?.Element("Text");
                if (textElement != null)
                {
                    var textAnnotation = (TextAnnotation)annotation;
                    if (GetColorAttribute(textElement, nameof(textAnnotation.Background), out var background)) textAnnotation.Background = background;
                    if (GetVectorAttribute(textElement, nameof(textAnnotation.Offset), out var offset)) textAnnotation.Offset = offset;
                    if (GetThicknessAttribute(textElement, nameof(textAnnotation.Padding), thicknessConverter, out var padding)) textAnnotation.Padding = padding;
                    if (GetColorAttribute(textElement, nameof(textAnnotation.Stroke), out var stroke)) textAnnotation.Stroke = stroke;
                    if (GetDoubleAttribute(textElement, nameof(textAnnotation.StrokeThickness), out var strokeThickness)) textAnnotation.StrokeThickness = strokeThickness;
                }
            }

            // Must be shape or path annotation
            var shapeAnnotation = annotation as ShapeAnnotation;
            if (shapeAnnotation != null)
            {
                var shapeElement = textualElement?.Element("Shape");
                if (shapeElement != null)
                {
                    if (GetColorAttribute(shapeElement, nameof(shapeAnnotation.Fill), out var fill)) shapeAnnotation.Fill = fill;
                    if (GetColorAttribute(shapeElement, nameof(shapeAnnotation.Stroke), out var stroke)) shapeAnnotation.Stroke = stroke;
                    if (GetDoubleAttribute(shapeElement, nameof(shapeAnnotation.StrokeThickness), out var strokeThickness)) shapeAnnotation.StrokeThickness = strokeThickness;

                    // Must be Ellipse, Point, Rectangle, or Polygon Annotations (all concrete)
                    if (currentAnnotationType == typeof(EllipseAnnotation))
                    {
                        var ellipseElement = shapeElement.Element("Ellipse");
                        if (ellipseElement != null)
                        {
                            var ellipseAnnotation = (EllipseAnnotation)shapeAnnotation;
                            // Read Min/Max first (backward compat for old files)
                            if (GetDoubleAttribute(ellipseElement, nameof(ellipseAnnotation.MinimumX), out var minimumX)) ellipseAnnotation.MinimumX = minimumX;
                            if (GetDoubleAttribute(ellipseElement, nameof(ellipseAnnotation.MaximumY), out var maximumY)) ellipseAnnotation.MaximumY = maximumY;
                            if (GetDoubleAttribute(ellipseElement, nameof(ellipseAnnotation.MaximumX), out var maximumX)) ellipseAnnotation.MaximumX = maximumX;
                            if (GetDoubleAttribute(ellipseElement, nameof(ellipseAnnotation.MinimumY), out var minimumY)) ellipseAnnotation.MinimumY = minimumY;
                            // Read X/Y/Width/Height after — authoritative DPs take precedence when present
                            if (GetDoubleAttribute(ellipseElement, nameof(ellipseAnnotation.X), out var x)) ellipseAnnotation.X = x;
                            if (GetDoubleAttribute(ellipseElement, nameof(ellipseAnnotation.Y), out var y)) ellipseAnnotation.Y = y;
                            if (GetDoubleAttribute(ellipseElement, nameof(ellipseAnnotation.Width), out var width)) ellipseAnnotation.Width = width;
                            if (GetDoubleAttribute(ellipseElement, nameof(ellipseAnnotation.Height), out var height)) ellipseAnnotation.Height = height;
                        }
                    }
                    else if (currentAnnotationType == typeof(RectangleAnnotation))
                    {
                        var rectangleElement = shapeElement.Element("Rectangle");
                        if (rectangleElement != null)
                        {
                            var rectangleAnnotation = (RectangleAnnotation)shapeAnnotation;
                            if (GetDoubleAttribute(rectangleElement, nameof(rectangleAnnotation.MinimumX), out var minimumX)) rectangleAnnotation.MinimumX = minimumX;
                            if (GetDoubleAttribute(rectangleElement, nameof(rectangleAnnotation.MaximumY), out var maximumY)) rectangleAnnotation.MaximumY = maximumY;
                            if (GetDoubleAttribute(rectangleElement, nameof(rectangleAnnotation.MaximumX), out var maximumX)) rectangleAnnotation.MaximumX = maximumX;
                            if (GetDoubleAttribute(rectangleElement, nameof(rectangleAnnotation.MinimumY), out var minimumY)) rectangleAnnotation.MinimumY = minimumY;
                        }
                    }
                    else if (currentAnnotationType == typeof(PointAnnotation))
                    {
                        var pointElement = shapeElement.Element("Point");
                        if (pointElement != null)
                        {
                            var pointAnnotation = (PointAnnotation)shapeAnnotation;
                            if (GetDoubleAttribute(pointElement, nameof(pointAnnotation.X), out var x)) pointAnnotation.X = x;
                            if (GetDoubleAttribute(pointElement, nameof(pointAnnotation.Y), out var y)) pointAnnotation.Y = y;
                            if (GetDoubleAttribute(pointElement, nameof(pointAnnotation.Size), out var size)) pointAnnotation.Size = size;
                            if (GetDoubleAttribute(pointElement, nameof(pointAnnotation.TextMargin), out var textMargin)) pointAnnotation.TextMargin = textMargin;
                            if (GetEnumAttribute(pointElement, nameof(pointAnnotation.Shape), out MarkerType shape)) pointAnnotation.Shape = shape;
                        }
                    }
                    else if (currentAnnotationType == typeof(PolygonAnnotation))
                    {
                        var polygonElement = shapeElement.Element("Polygon");
                        if (polygonElement != null)
                        {
                            var polygonAnnotation = (PolygonAnnotation)shapeAnnotation;
                            if (GetEnumAttribute(polygonElement, nameof(polygonAnnotation.LineJoin), out LineJoin lineJoin)) polygonAnnotation.LineJoin = lineJoin;
                            if (GetEnumAttribute(polygonElement, nameof(polygonAnnotation.LineStyle), out LineStyle lineStyle)) polygonAnnotation.LineStyle = lineStyle;
                            if (GetDoubleAttribute(polygonElement, nameof(polygonAnnotation.MinimumSegmentLength), out var minimumSegmentLength)) polygonAnnotation.MinimumSegmentLength = minimumSegmentLength;
                            var polyPointsElement = polygonElement.Element(nameof(polygonAnnotation.Points));
                            if (polyPointsElement != null) polygonAnnotation.Points = polyPointsElement.PointsFromXElement();

                            // Backwards compatibility
                            polyPointsElement = polygonElement.Element("DataPoints");
                            if (polyPointsElement != null) polygonAnnotation.Points = polyPointsElement.PointsFromXElement();
                        }
                    }
                }
            }
            else
            {
                var pathAnnotation = annotation as PathAnnotation;
                if (pathAnnotation != null)
                {
                    var pathElement = textualElement?.Element("Path");
                    if (pathElement != null)
                    {
                        if (GetColorAttribute(pathElement, nameof(pathAnnotation.Color), out var color)) pathAnnotation.Color = color;
                        if (GetBooleanAttribute(pathElement, nameof(pathAnnotation.ClipByXAxis), out var clipByXAxis)) pathAnnotation.ClipByXAxis = clipByXAxis;
                        if (GetBooleanAttribute(pathElement, nameof(pathAnnotation.ClipByYAxis), out var clipByYAxis)) pathAnnotation.ClipByYAxis = clipByYAxis;
                        if (GetBooleanAttribute(pathElement, nameof(pathAnnotation.ClipText), out var clipText)) pathAnnotation.ClipText = clipText;
                        if (GetEnumAttribute(pathElement, nameof(pathAnnotation.LineJoin), out LineJoin lineJoin)) pathAnnotation.LineJoin = lineJoin;
                        if (GetEnumAttribute(pathElement, nameof(pathAnnotation.LineStyle), out LineStyle lineStyle)) pathAnnotation.LineStyle = lineStyle;
                        if (GetDoubleAttribute(pathElement, nameof(pathAnnotation.StrokeThickness), out var strokeThickness)) pathAnnotation.StrokeThickness = strokeThickness;
                        if (GetDoubleAttribute(pathElement, nameof(pathAnnotation.TextMargin), out var textMargin)) pathAnnotation.TextMargin = textMargin;
                        if (GetEnumAttribute(pathElement, nameof(pathAnnotation.TextOrientation), out OxyPlot.Annotations.AnnotationTextOrientation textOrientation)) pathAnnotation.TextOrientation = textOrientation;
                        if (GetDoubleAttribute(pathElement, nameof(pathAnnotation.TextLinePosition), out var textLinePosition)) pathAnnotation.TextLinePosition = textLinePosition;

                        // Must be Line, Polyline, or Function Annotations (all concrete)
                        if (currentAnnotationType == typeof(LineAnnotation))
                        {
                            var lineElement = pathElement.Element("Line");
                            if (lineElement != null)
                            {
                                var lineAnnotation = (LineAnnotation)pathAnnotation;
                                if (GetEnumAttribute(lineElement, nameof(lineAnnotation.Type), out OxyPlot.Annotations.LineAnnotationType type)) lineAnnotation.Type = type;
                                if (GetDoubleAttribute(lineElement, nameof(lineAnnotation.X), out var x)) lineAnnotation.X = x;
                                if (GetDoubleAttribute(lineElement, nameof(lineAnnotation.Y), out var y)) lineAnnotation.Y = y;
                                if (GetDoubleAttribute(lineElement, nameof(lineAnnotation.Intercept), out var intercept)) lineAnnotation.Intercept = intercept;
                                if (GetDoubleAttribute(lineElement, nameof(lineAnnotation.MinimumX), out var minimumX)) lineAnnotation.MinimumX = minimumX;
                                if (GetDoubleAttribute(lineElement, nameof(lineAnnotation.MaximumY), out var maximumY)) lineAnnotation.MaximumY = maximumY;
                                if (GetDoubleAttribute(lineElement, nameof(lineAnnotation.MaximumX), out var maximumX)) lineAnnotation.MaximumX = maximumX;
                                if (GetDoubleAttribute(lineElement, nameof(lineAnnotation.MinimumY), out var minimumY)) lineAnnotation.MinimumY = minimumY;
                                if (GetDoubleAttribute(lineElement, nameof(lineAnnotation.Slope), out var slope)) lineAnnotation.Slope = slope;
                            }
                        }
                        else if (currentAnnotationType == typeof(PolylineAnnotation))
                        {
                            var polylineElement = pathElement.Element("Polyline");
                            if (polylineElement != null)
                            {
                                var polylineAnnotation = (PolylineAnnotation)pathAnnotation;
                                if (GetDoubleAttribute(polylineElement, nameof(polylineAnnotation.MinimumSegmentLength), out var minimumSegmentLength)) polylineAnnotation.MinimumSegmentLength = minimumSegmentLength;
                                var polyPointsElement = polylineElement.Element(nameof(polylineAnnotation.Points));
                                if (polyPointsElement != null) polylineAnnotation.Points = polyPointsElement.PointsFromXElement();

                                // Backwards compatibility
                                polyPointsElement = polylineElement.Element("DataPoints");
                                if (polyPointsElement != null) polylineAnnotation.Points = polyPointsElement.PointsFromXElement();
                            }
                        }
                        else if (currentAnnotationType == typeof(FunctionAnnotation))
                        {
                            var functionElement = pathElement.Element("Function");
                            if (functionElement != null)
                            {
                                var functionAnnotation = (FunctionAnnotation)pathAnnotation;
                                if (GetEnumAttribute(functionElement, nameof(functionAnnotation.Type), out OxyPlot.Annotations.FunctionAnnotationType functionType))
                                    functionAnnotation.Type = functionType;
                                if (GetIntegerAttribute(functionElement, nameof(functionAnnotation.Resolution), out var resolution))
                                    functionAnnotation.Resolution = resolution;
                            }
                        }
                    }
                }
            }

            return annotation;
        }
    }
}
