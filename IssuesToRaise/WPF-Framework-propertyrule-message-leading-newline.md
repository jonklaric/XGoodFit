# Validation messages start with a blank line when only a later rule fails

**Target:** [USACE-RMC/WPF-Framework](https://github.com/USACE-RMC/WPF-Framework), `src/GenericControls/DataGrid/ValidationDataGridObjects/PropertyRule.cs`. Found in 1.0.5 at commit [`7f897e1`](https://github.com/USACE-RMC/WPF-Framework/tree/7f897e107db4a25cca29127ce9d564898c5fde93), still `main` on 2026-10-30.

**Type:** user interface · **Severity:** low

**Verification:** verified by reading the source. XGoodFit's NumericControls tests, which run the same rule logic, see the leading newline in a cell's error message.

## Summary

`PropertyRule.ExecuteRules` puts a newline before every message except rule 0's. If rule 0 passes and rule 2 fails, the error message (shown in the `ValidationDataGrid` cell's tooltip) starts with a blank line. In the same method, an exception sets `_errorMessage` directly, bypassing the setter, so bindings never see that message.

## Details and how to reproduce

Give a property two rules, for example "is NaN" and "is negative", and enter a negative number. **Expected:** "Value must be positive." **Actual:** a newline followed by "Value must be positive.", so the tooltip's first line is empty.

## Why this is an issue and not intended behaviour

The newline is meant to separate messages, which is clear from joining them. It should depend on whether a message was already added, not on the rule's position.

## Where in the source

[`PropertyRule.cs` lines 157-177](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/GenericControls/DataGrid/ValidationDataGridObjects/PropertyRule.cs#L157-L177)

```csharp
if (_rules[i].Expression() == true)
{
    HasError = true;
    ErrorMessage += i == 0 ? _rules[i].Message : Environment.NewLine + _rules[i].Message;   // line 168
}
...
catch (Exception e)
{
    _errorMessage = e.Message;   // line 174: no PropertyChanged
```

## Suggested fix

```csharp
ErrorMessage += string.IsNullOrEmpty(ErrorMessage) ? _rules[i].Message : Environment.NewLine + _rules[i].Message;
...
catch (Exception e) { ErrorMessage = e.Message; HasError = true; }
```

Messages are only shown, never saved.

## How XGoodFit handles it

XGoodFit (a port of RMC-BestFit to Avalonia) compiles this file unchanged and lists the behaviour among the upstream bugs kept in [`docs/migration-plan.md`, Phase 5](https://github.com/jonklaric/XGoodFit/blob/fcb3902c2937662ed5f1b3869e0ea019e88e3c63/docs/migration-plan.md#L576).
