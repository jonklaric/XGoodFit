// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AssemblyInfo.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

// XGoodFit port: WPF's ThemeInfo has no Avalonia equivalent (this assembly has no themes); the XML
// namespace attributes are Avalonia's.
using System;
using Avalonia.Metadata;

[assembly: CLSCompliant(false)]

[assembly: XmlnsPrefix("http://oxyplot.org/wpf", "oxy")]
[assembly: XmlnsDefinition("http://oxyplot.org/wpf", "OxyPlot.Wpf")]
