// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AssemblyInfo.cs" company="OxyPlot">
//   Copyright (c) 2020 OxyPlot contributors
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

// XGoodFit port: WPF's ThemeInfo, which applies Themes/Generic.xaml by itself, has no Avalonia
// equivalent; PlotViewBase and TrackerControl load the ported Themes/Generic.axaml themselves
// (GenericTheme). XmlnsDefinition is Avalonia's.
using Avalonia.Metadata;

[assembly: XmlnsDefinition("http://oxyplot.org/wpf", "OxyPlot.Wpf")]
