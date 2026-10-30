# The batch run window can be closed during a run, leaving the run going with no way to cancel it

**Target:** [USACE-RMC/RMC-BestFit](https://github.com/USACE-RMC/RMC-BestFit), `src/RMC.BestFit.App/GUI/Support/Controls/BatchRunWindow.xaml.cs`, with `MetroWindow` from [USACE-RMC/WPF-Framework](https://github.com/USACE-RMC/WPF-Framework) `src/GenericControls/Windows/MetroWindow.cs`. Found in RMC-BestFit 2.0.1 at commit [`bfce2dd`](https://github.com/USACE-RMC/RMC-BestFit/tree/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269) and WPF-Framework 1.0.5 at [`7f897e1`](https://github.com/USACE-RMC/WPF-Framework/tree/7f897e107db4a25cca29127ce9d564898c5fde93), both still `main` on 2026-10-31.

**Type:** bug · **Severity:** medium

**Verification:** command routing reproduced; the rest verified by reading. A small WPF program with the same two classes (a `MetroWindow` that binds `SystemCommands.CloseWindowCommand` to a virtual `OnCloseWindow`, and a subclass that adds a second binding to a `new` method) executed the command on the subclass: only the base class's handler ran. Not checked in the running application.

## Summary

`BatchRunWindow` tries to refuse closing while a batch runs by binding the close command to its own `OnCloseWindow`. But its base class, `MetroWindow`, has already bound the same command to its own `OnCloseWindow`, which closes the window. WPF runs the first matching binding and stops, so the batch window's check never runs, and the title bar's close button closes the window mid-run. Alt+F4 closes it too, since there's no `Closing` handler. The run carries on with nothing to show its progress and no way to cancel it, and until it finishes the shell silently refuses to close documents or the application.

## Details and how to reproduce

1. Open the batch run window, select several analyses and start the run.
2. Click the window's close button, or press Alt+F4.
3. Expected: the window stays open while the run is in progress (as `OnCloseWindow`'s comment says), or closing it cancels the run. Actual, by reading and from the program below: the window closes; the run continues; Cancel and Escape, the only ways to stop it, were in the closed window.

The program:

```csharp
class MetroWindow : Window
{
    public MetroWindow() { CommandBindings.Add(new CommandBinding(SystemCommands.CloseWindowCommand, OnCloseWindow)); }
    protected virtual void OnCloseWindow(object sender, ExecutedRoutedEventArgs e) { Console.WriteLine("MetroWindow.OnCloseWindow ran"); }
}
class BatchRunWindow : MetroWindow
{
    public BatchRunWindow() { CommandBindings.Add(new CommandBinding(SystemCommands.CloseWindowCommand, OnCloseWindow)); }
    private new void OnCloseWindow(object target, ExecutedRoutedEventArgs e) { Console.WriteLine("BatchRunWindow.OnCloseWindow ran"); }
}

SystemCommands.CloseWindowCommand.Execute(null, new BatchRunWindow());
// prints only: MetroWindow.OnCloseWindow ran
```

Because the subclass's method is declared `new`, not `override`, the base class's binding calls the base implementation.

## Why this is an issue and not intended behaviour

- `OnCloseWindow` is documented as "Prevents window closure if a simulation is currently in progress", so the window is meant to stay open during a run.
- The window is modeless (`Show()`), so after it closes the user is back in the main window with the run still going. `ShellPublicVariables.SimulationInProgress` stays `true` until the run ends, and the main window's `Document_Closing` and `MainWindow_Closing` then cancel every close without a message. Batch runs of Bayesian analyses can take a long time.

## Where in the source

- The batch window's binding and handler: [`src/RMC.BestFit.App/GUI/Support/Controls/BatchRunWindow.xaml.cs` line 71](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/Support/Controls/BatchRunWindow.xaml.cs#L71) and [lines 111-121](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/Support/Controls/BatchRunWindow.xaml.cs#L111-L121)
  ```csharp
  private new void OnCloseWindow(object target, ExecutedRoutedEventArgs e)
  {
      if (FrameworkUI.ShellPublicVariables.SimulationInProgress == false)
          SystemCommands.CloseWindow(this);
  }
  ```
- The base class's binding, added first: [WPF-Framework `src/GenericControls/Windows/MetroWindow.cs` lines 41-58](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/GenericControls/Windows/MetroWindow.cs#L41-L58). The title bar's close button sends this command: [`src/Themes/Resources/Controls/MetroWindowStyle.xaml` line 187](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/Themes/Resources/Controls/MetroWindowStyle.xaml#L187).
- The run, its cleanup and the only ways to cancel it: [`BatchRunWindow.xaml.cs` lines 162-222](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/Support/Controls/BatchRunWindow.xaml.cs#L162-L222), [lines 243-246](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/Support/Controls/BatchRunWindow.xaml.cs#L243-L246) (Cancel) and [lines 257-260](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/Support/Controls/BatchRunWindow.xaml.cs#L257-L260) (Escape).
- The window is shown modeless: [`src/RMC.BestFit.App/GUI/MainProjectNode.cs` lines 158-159](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/MainProjectNode.cs#L158-L159) (and three more places).
- The shell's silent refusals: [WPF-Framework `src/FrameworkUI/Main Window/MainWindow.xaml.cs` lines 1351-1355](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/FrameworkUI/Main%20Window/MainWindow.xaml.cs#L1351-L1355) and [lines 1780-1784](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/FrameworkUI/Main%20Window/MainWindow.xaml.cs#L1780-L1784).

## Suggested fix

1. Make the check an override, and drop the second binding:
   ```csharp
   protected override void OnCloseWindow(object sender, ExecutedRoutedEventArgs e)
   {
       if (!FrameworkUI.ShellPublicVariables.SimulationInProgress)
           base.OnCloseWindow(sender, e);
   }
   ```
2. Handle `Closing` as well, so Alt+F4 and the taskbar's Close are covered: either cancel it during a run, or ask whether to cancel the run and, if so, call `_cancellationTokenSource.Cancel()` and `_runner.Cancel()` before closing.

Asking (2) is friendlier than refusing silently. No compatibility risk.

## How XGoodFit handles it

XGoodFit's Avalonia port keeps upstream's code for now ([`src/BestFit/RMC.BestFit.App/GUI/Support/Controls/BatchRunWindow.axaml.cs` lines 81 and 134](https://github.com/jonklaric/XGoodFit/blob/a7aced180aba4493b1f8f7b676db89543ae9a0a7/src/BestFit/RMC.BestFit.App/GUI/Support/Controls/BatchRunWindow.axaml.cs#L81)). Whether its command routing behaves as WPF's does hasn't been checked.
