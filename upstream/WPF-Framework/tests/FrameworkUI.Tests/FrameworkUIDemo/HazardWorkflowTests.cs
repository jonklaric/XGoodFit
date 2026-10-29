using System.Threading;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using FrameworkUI.Demo;
using FrameworkUI.Demo.UI;
using NumericControls;
using Numerics.Distributions;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace FrameworkUI.Tests.FrameworkUIDemo;

/// <summary>Checks hazard editing, undo, validation, and control-binding contracts.</summary>
public class HazardWorkflowTests
{
    /// <summary>Verifies that assigning an element preserves the one-way probability-ordinate binding.</summary>
    [Fact]
    public void SettingElement_OnPropertiesControl_DoesNotThrowAndKeepsProbabilityOrdinatesOneWay()
    {
        DispatcherTestHost.Run(() =>
        {
            using var resources = EnsureApplicationResources();
            var element = CreateElement("Hazard_Binding");
            var control = new HazardPropertiesControl();

            var exception = Record.Exception(() => control.Element = element);

            Assert.Null(exception);
            control.ApplyTemplate();
            control.UpdateLayout();
            var ordinatesControl = FindDescendant<ProbabilityOrdinatesControl>(control);
            Assert.NotNull(ordinatesControl);

            var binding = BindingOperations.GetBinding(ordinatesControl!, ProbabilityOrdinatesControl.ProbabilityOrdinatesProperty);
            Assert.NotNull(binding);
            Assert.Equal(BindingMode.OneWay, binding!.Mode);
        });
    }

    /// <summary>Verifies that distribution assignment clones its input and undo/redo restores independent snapshots.</summary>
    [Fact]
    public void ParentDistribution_SetterClonesInputAndUndoRedoRestoresSnapshots()
    {
        DispatcherTestHost.Run(() =>
        {
            var element = CreateElement("Hazard_DistributionUndo");
            var originalType = element.ParentDistribution.Type;
            var incomingDistribution = UnivariateDistributionFactory.CreateDistribution(UnivariateDistributionType.Normal);

            element.ParentDistribution = incomingDistribution;

            Assert.Equal(UnivariateDistributionType.Normal, element.ParentDistribution.Type);
            Assert.NotSame(incomingDistribution, element.ParentDistribution);
            var elementParameters = element.ParentDistribution.GetParameters.ToArray();
            var mutatedIncomingParameters = incomingDistribution.GetParameters.ToArray();
            mutatedIncomingParameters[0] += 10d;
            incomingDistribution.SetParameters(mutatedIncomingParameters);
            AssertParametersEqual(elementParameters, element.ParentDistribution.GetParameters.ToArray());

            Assert.True(element.UndoManager.CanUndo);
            element.UndoManager.Undo();
            Assert.Equal(originalType, element.ParentDistribution.Type);
            Assert.True(element.UndoManager.CanRedo);
            element.UndoManager.Redo();
            Assert.Equal(UnivariateDistributionType.Normal, element.ParentDistribution.Type);
        });
    }

    /// <summary>Verifies that parameter edits invalidate estimates and undo/redo restores parameter snapshots.</summary>
    [Fact]
    public void SetDistributionParameters_InvalidatesEstimateAndUndoRedoRestoresSnapshots()
    {
        DispatcherTestHost.Run(() =>
        {
            var element = CreateElement("Hazard_ParameterUndo");
            element.Estimate();
            Assert.True(element.IsEstimated);
            Assert.True(element.IsValid);

            var originalParameters = element.ParentDistribution.GetParameters.ToArray();
            var updatedParameters = originalParameters.ToArray();
            updatedParameters[0] += 0.25d;

            element.SetDistributionParameters(updatedParameters);

            Assert.False(element.IsEstimated);
            Assert.False(element.IsValid);
            AssertParametersEqual(updatedParameters, element.ParentDistribution.GetParameters.ToArray());
            Assert.True(element.UndoManager.CanUndo);

            element.UndoManager.Undo();
            AssertParametersEqual(originalParameters, element.ParentDistribution.GetParameters.ToArray());
            Assert.True(element.UndoManager.CanRedo);

            element.UndoManager.Redo();
            AssertParametersEqual(updatedParameters, element.ParentDistribution.GetParameters.ToArray());
        });
    }

    /// <summary>Verifies that empty ordinates and invalid sampling requests return the documented absent-result values.</summary>
    [Fact]
    public void SamplingAndMinMax_HandleEmptyOrdinatesAndInvalidRequests()
    {
        DispatcherTestHost.Run(() =>
        {
            var element = CreateElement("Hazard_EmptyOrdinates");
            element.ProbabilityOrdinates.Clear();

            Assert.Null(element.SampleFunction());
            Assert.Null(element.SampleFunction(-0.01d));
            Assert.Null(element.SampleFunction(1.01d));
            Assert.Null(element.SampleFunction(-1));
            Assert.Null(element.SampleFunction(0));
            Assert.True(double.IsNaN(element.MinHazard(meanOnly: true)));
            Assert.True(double.IsNaN(element.MaxHazard(meanOnly: false)));
        });
    }

    /// <summary>Verifies that the hazard control clears plot data when result dimensions no longer match the ordinates.</summary>
    [Fact]
    public void HazardControl_ClearsPlotDataWhenEstimatedResultsDoNotMatchOrdinates()
    {
        DispatcherTestHost.Run(() =>
        {
            using var resources = EnsureApplicationResources();
            var element = CreateElement("Hazard_MismatchedResults");
            element.Estimate();
            Assert.True(element.IsEstimated);

            var control = new HazardControl { Element = element };
            control.UpdatePlot();
            Assert.NotEmpty(control.ModeLinePoints);

            element.Results.ModeCurve = element.Results.ModeCurve!.Take(1).ToArray();
            var exception = Record.Exception(() =>
            {
                control.UpdatePlot();
                control.BindFrequencyCurveTable();
            });

            Assert.Null(exception);
            Assert.Empty(control.ModeLinePoints);
        });
    }

    private static HazardElement CreateElement(string name)
    {
        var element = new HazardElement(name, new HazardElementCollection(DemoProject.GetInstance()));
        element.IsUndoEnabled = false;
        element.IsUncertain = false;
        element.IsUndoEnabled = true;
        element.UndoManager.Clear();
        return element;
    }

    private static IDisposable EnsureApplicationResources()
    {
        if (Application.Current == null)
        {
            _ = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        }

        var addedDictionaries = new List<ResourceDictionary>();
        AddResourceDictionary("pack://application:,,,/FrameworkUI;component/Icons/IconDictionary.xaml", addedDictionaries);
        AddResourceDictionary("pack://application:,,,/GenericControls;component/Themes/GenericControlsTheme.xaml", addedDictionaries);
        AddResourceDictionary("pack://application:,,,/OxyPlotControls;component/Themes/OxyPlotControlsTheme.xaml", addedDictionaries);
        AddResourceDictionary("pack://application:,,,/NumericControls;component/Themes/NumericControlsTheme.xaml", addedDictionaries);
        AddResourceDictionary("pack://application:,,,/DatabaseControls;component/Themes/DatabaseControlsTheme.xaml", addedDictionaries);
        return new ResourceScope(addedDictionaries);
    }

    private static void AddResourceDictionary(string source, IList<ResourceDictionary> addedDictionaries)
    {
        var uri = new Uri(source, UriKind.Absolute);
        if (Application.Current!.Resources.MergedDictionaries.Any(x => x.Source == uri)) return;
        var dictionary = new ResourceDictionary { Source = uri };
        Application.Current.Resources.MergedDictionaries.Add(dictionary);
        addedDictionaries.Add(dictionary);
    }

    private sealed class ResourceScope : IDisposable
    {
        private readonly IList<ResourceDictionary> dictionaries;

        public ResourceScope(IList<ResourceDictionary> dictionaries)
        {
            this.dictionaries = dictionaries;
        }

        public void Dispose()
        {
            foreach (var dictionary in dictionaries)
            {
                Application.Current?.Resources.MergedDictionaries.Remove(dictionary);
            }
        }
    }

    private static T? FindDescendant<T>(DependencyObject root)
        where T : DependencyObject
    {
        if (root is T match) return match;

        foreach (object child in LogicalTreeHelper.GetChildren(root))
        {
            if (child is DependencyObject dependencyChild)
            {
                var logicalMatch = FindDescendant<T>(dependencyChild);
                if (logicalMatch != null) return logicalMatch;
            }
        }

        int visualChildren = 0;
        try
        {
            visualChildren = VisualTreeHelper.GetChildrenCount(root);
        }
        catch (InvalidOperationException)
        {
            return null;
        }

        for (int i = 0; i < visualChildren; i++)
        {
            var visualMatch = FindDescendant<T>(VisualTreeHelper.GetChild(root, i));
            if (visualMatch != null) return visualMatch;
        }

        return null;
    }

    private static void AssertParametersEqual(double[] expected, double[] actual)
    {
        Assert.Equal(expected.Length, actual.Length);
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i], actual[i], precision: 12);
        }
    }
}

internal static class DispatcherTestHost
{
    private static readonly object Gate = new();
    private static Dispatcher? dispatcher;
    private static Thread? thread;

    public static void Run(Action body)
    {
        EnsureRunning().Invoke(body);
    }

    private static Dispatcher EnsureRunning()
    {
        lock (Gate)
        {
            if (dispatcher != null)
            {
                return dispatcher;
            }

            using var ready = new ManualResetEventSlim(false);
            Dispatcher? captured = null;

            thread = new Thread(() =>
            {
                if (Application.Current == null)
                {
                    _ = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
                }

                captured = Dispatcher.CurrentDispatcher;
                ready.Set();
                Dispatcher.Run();
            })
            {
                IsBackground = true,
                Name = "FrameworkUIDemoHazardTestsDispatcher",
            };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            ready.Wait();
            dispatcher = captured!;
            return dispatcher;
        }
    }
}