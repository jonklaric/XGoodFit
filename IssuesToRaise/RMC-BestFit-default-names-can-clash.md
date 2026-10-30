# New element names suggested from the element count can already be taken

**Target:** [USACE-RMC/RMC-BestFit](https://github.com/USACE-RMC/RMC-BestFit), `src/RMC.BestFit.App/GUI/MainProjectNode.cs`. Found in 2.0.1 at commit [`bfce2dd`](https://github.com/USACE-RMC/RMC-BestFit/tree/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269), still `main` on 2026-10-31.

**Type:** user interface · **Severity:** low

**Verification:** verified by reading the source; not run.

## Summary

When creating a new element, the name dialog suggests `"<Type>_{collection.Count + 1}"`. After an element has been deleted or renamed, that name can belong to an existing element. The dialog knows the existing names, so clicking OK with the suggestion shows an "Invalid Name" error and the user has to make up a name.

## Details and how to reproduce

1. Create two Input Data elements, accepting the suggested names "Input Data_1" and "Input Data_2".
2. Delete "Input Data_1".
3. Create another Input Data element.
4. Expected: a free name, such as "Input Data_3". Actual, by reading: the dialog suggests "Input Data_2" (one element, plus one), and OK reports that the name is invalid.

## Why this is an issue and not intended behaviour

The suggestion is meant to be accepted as it is; one that is guaranteed to be rejected isn't useful. The fix is local and cheap.

## Where in the source

- The 12 "New …" handlers, one per element type, from [`src/RMC.BestFit.App/GUI/MainProjectNode.cs` line 602](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/MainProjectNode.cs#L602) to [line 769](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/MainProjectNode.cs#L769), for example:
  ```csharp
  string newName = CreateNewNameDialog("New Input Data", $"Input Data_{collection.Count + 1}", collection.Select(x => x.Name.ToString()).ToList());
  ```
- `CreateNewNameDialog` passes the existing names to the dialog as invalid strings: [lines 788-801](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/MainProjectNode.cs#L788-L801). The dialog rejects them on OK: [WPF-Framework `src/GenericControls/Windows/NameDialog.xaml.cs` lines 112-121](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/GenericControls/Windows/NameDialog.xaml.cs#L112-L121).

## Suggested fix

Suggest the first free number, in one helper used by all 12 handlers:

```csharp
private static string NextFreeName(string prefix, IEnumerable<string> existing)
{
    var names = new HashSet<string>(existing, StringComparer.OrdinalIgnoreCase);
    int n = names.Count + 1;
    while (names.Contains($"{prefix}_{n}")) n++;
    return $"{prefix}_{n}";
}
```

Match the comparison to the one `NameTextBox` uses for duplicates. No compatibility risk.

## How XGoodFit handles it

XGoodFit's Avalonia port keeps upstream's suggestions for now ([`src/BestFit/RMC.BestFit.App/GUI/MainProjectNode.cs` line 671](https://github.com/jonklaric/XGoodFit/blob/a7aced180aba4493b1f8f7b676db89543ae9a0a7/src/BestFit/RMC.BestFit.App/GUI/MainProjectNode.cs#L671) and the following handlers).
