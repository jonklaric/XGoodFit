using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;
using Avalonia.Threading;

namespace RMC.BestFit.App.Tests.GUI.Support
{
    /// <summary>
    /// Supplies application resources and dispatcher draining for real WPF control regressions.
    /// </summary>
    /// <remarks>
    /// Call from an STA test marked nonparallel because application resources are shared.
    /// <para>
    /// XGoodFit port: the same dictionaries, the ports' (avares URIs; the application's own assembly is
    /// XGoodFit), merged into the headless session's application, which exists already (upstream made a
    /// WPF Application when there was none). The dispatcher is the session's UI thread's.
    /// </para>
    /// </remarks>
    internal static class WpfTestHost
    {
        /// <summary>Resource dictionaries installed for the current STA test.</summary>
        private static readonly List<ResourceInclude> _dictionaries = new List<ResourceInclude>();
        /// <summary>The STA thread that created the current resource instances.</summary>
        private static int _resourceThreadId;
        /// <summary>Whether this host supplied the shell-only separator brush.</summary>
        private static bool _addedSeparatorBrush;
        /// <summary>
        /// Loads the production resource dictionaries required by analysis controls.
        /// </summary>
        internal static void EnsureResources()
        {
            if (_dictionaries.Count > 0 && _resourceThreadId == Environment.CurrentManagedThreadId) return;
            ReleaseResources();
            var resources = Application.Current.Resources;
            _resourceThreadId = Environment.CurrentManagedThreadId;

            // Load the same theme graph directly. A test's Application may have been created on
            // an earlier MSTest STA thread, so invoking the desktop theme service could deadlock.
            foreach (var resource in new[]
            {
                ("Themes", "Resources/Controls/Merged.axaml"),
                ("Themes", "Resources/Colors/LightColors.axaml"),
                ("FrameworkUI", "Themes/VS2013/LightTheme.axaml"),
                ("GenericControls", "Themes/GenericControlsTheme.axaml"),
                ("OxyPlotControls", "Themes/OxyPlotControlsTheme.axaml"),
                ("NumericControls", "Themes/NumericControlsTheme.axaml"),
                ("DatabaseControls", "Themes/DatabaseControlsTheme.axaml"),
                ("RMC.BestFit.UI", "Resources/IconDictionary.axaml"),
                ("XGoodFit", "Resources/IconDictionary.axaml"),
                ("GenericControls", "Resources/ResourceDictionary.axaml")
            })
            {
                var source = new Uri($"avares://{resource.Item1}/{resource.Item2}");
                var dictionary = new ResourceInclude(source) { Source = source };
                resources.MergedDictionaries.Add(dictionary);
                _dictionaries.Add(dictionary);
            }

            // The desktop shell supplies this brush; control-only hosts do not create that shell.
            _addedSeparatorBrush = !resources.ContainsKey("SeparatorBackground");
            if (_addedSeparatorBrush) resources["SeparatorBackground"] = Brushes.Gray;
        }

        /// <summary>
        /// Removes test-owned dictionaries so vector icons are not reused by a different STA thread.
        /// </summary>
        internal static void ReleaseResources()
        {
            if (Application.Current != null)
            {
                foreach (var dictionary in _dictionaries)
                    Application.Current.Resources.MergedDictionaries.Remove(dictionary);
                if (_addedSeparatorBrush) Application.Current.Resources.Remove("SeparatorBackground");
            }
            _dictionaries.Clear();
            _addedSeparatorBrush = false;
        }

        /// <summary>
        /// Processes queued bindings, result notifications, and background cursor resets on the owning thread.
        /// </summary>
        internal static void DrainDispatcher()
        {
            Dispatcher.UIThread.RunJobs();
        }
    }
}
