using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GenericControls;
using RMC.BestFit.UI;

namespace RMC.BestFit.App.Tests.XGoodFit
{
    /// <summary>
    /// XGoodFit addition: the Point Process and Mixture examples (4-univariate-distribution-analysis), for the tests of
    /// their views (GUI/UnivariateAnalysis/PointProcess and Mixture).
    /// </summary>
    internal static class PointProcessMixtureTesting
    {
        /// <summary>The point process example: annual-maxima GEV fits and the stations' point-process fits.</summary>
        internal const string PointProcessExample = UnivariateAnalysisTesting.Folder + "3-point-process-analysis/point-process-examples.bestfit";

        /// <summary>The mixture example: two- and three-Normal mixtures, one zero-inflated.</summary>
        internal const string MixtureExample = UnivariateAnalysisTesting.Folder + "4-mixture-analysis/mixture-distribution-examples.bestfit";

        /// <summary>Big Bear Lake's point-process fit, which RMC-BestFit exported.</summary>
        internal const string BigBear = "USC00040741 - Point Process";

        /// <summary>De Sabla's point-process fit.</summary>
        internal const string DeSabla = "USC00042402 - Point Process";

        /// <summary>Big Bear Lake's two-season point-process fit.</summary>
        internal const string Seasonal = "USC00040741 - Seasonal Point Process";

        /// <summary>The two-Normal mixture, which RMC-BestFit exported.</summary>
        internal const string TwoNormals = "Mixture Distribution - 2 Normals";

        /// <summary>The three-Normal mixture.</summary>
        internal const string ThreeNormals = "Mixture Distribution - 3 Normals";

        /// <summary>The zero-inflated two-Normal mixture.</summary>
        internal const string ZeroInflated = "Mixture Distribution - 2 Normals - Zero-Inflated";

        /// <summary>
        /// Ticks or clears a property control's check box as a click does: the check box changes, and its binding
        /// writes the state to the control and on to the element.
        /// </summary>
        /// <param name="control">The property control.</param>
        /// <param name="value">The state.</param>
        internal static void Tick(BooleanPropertyControl control, bool value)
        {
            control.GetVisualDescendants().OfType<CheckBox>().Single().IsChecked = value;
            SharedControlsTesting.Settle();
        }

        /// <summary>Records the property changes of the objects given, as "Type.Property".</summary>
        /// <param name="changes">Where the changes go.</param>
        /// <param name="sources">The objects; those that don't notify are skipped.</param>
        internal static void Watch(List<string> changes, params object[] sources)
        {
            foreach (object source in sources)
                if (source is INotifyPropertyChanged notifier)
                    notifier.PropertyChanged += (sender, e) => changes.Add(sender.GetType().Name + "." + e.PropertyName);
        }
    }

    /// <summary>
    /// XGoodFit addition: an example project that a test class opens once, on the UI thread, when a test first asks for
    /// it, for the tests that only show it; the class closes it in its class cleanup. Tests that edit the example open a
    /// copy of their own.
    /// </summary>
    internal sealed class SharedExample
    {
        private readonly string _relative;
        private BestFitProject _project;

        /// <summary>Names the example.</summary>
        /// <param name="relative">The example's path under the examples folder.</param>
        internal SharedExample(string relative) => _relative = relative;

        /// <summary>The opened copy of the example.</summary>
        internal BestFitProject Project => _project ??= SharedControlsTesting.Open(_relative);

        /// <summary>Closes the example, if a test opened it, on the UI thread.</summary>
        internal void Close()
        {
            BestFitProject project = _project;
            _project = null;
            if (project == null) return;
            if (Dispatcher.UIThread.CheckAccess())
                SharedControlsTesting.Close(project);
            else
                HeadlessSession.Run(() => SharedControlsTesting.Close(project));
            GC.Collect();
        }
    }
}
