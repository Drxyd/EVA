# Fallible.EVA.SDK

> **Status: V0 (Experimental Preview)**
> *This SDK is currently in early preview and is not yet production-ready. V0 strictly enforces total resolution via `switch` statements and expressions.*

**Fallible.EVA.SDK** (Error Values Analysis) provides compile-time safety for zero-allocation error handling in C#. By combining Roslyn source generators with strict static analyzers, EVA ensures that errors returned from your methods cannot be accidentally ignored, partial resolutions are caught at compile time, and payloads are explicitly consumed.

---

## Core Features

* **Zero Allocation:** Uses generated `ref struct` wrappers (`R{MethodName}`) to avoid heap allocations.
* **Mandatory Error Handling:** Discarding or ignoring an error-returning result triggers a compile-time error.
* **Exhaustive Resolution:** Ensures every error variant (including payload types) is explicitly handled in `switch` statements and expressions.
* **Type-Safe Payloads:** Requires payload extraction methods (`.VariantName()`) to be called within matching arms.

---

## Analyzer Rules

| Rule ID | Severity | Category | Description |
| --- | --- | --- | --- |
| **`EV0001`** | `Error` | Reliability | **Unused Result:** Prevents calling an error-returning method without assigning or checking its result. |
| **`EV0002`** | `Error` | Reliability | **Incomplete Switch:** Enforces exhaustive pattern matching over all enum variants in `switch` blocks/expressions. |
| **`EV0003`** | `Error` | Reliability | **Unconsumed Payload:** Ensures variants carrying a `[Payload<T>]` attribute call their corresponding payload accessor. |
| **`EV0004`** | `Error` | Design | **Unwrapped Return Type:** Ensures the target method returns the generated wrapper struct (`R{MethodName}`). |

---

## Getting Started

### 1. Define Your Error States

Decorate an `enum` with `[ErrorStates]` pointing to your target method name, and attach `[Payload<T>]` to any variant carrying additional data:

```csharp
using ErrorValues.Attributes;

[ErrorStates(nameof(ParseInput))]
public enum ParseInputErrors
{
    None,
    [Payload<string>] InvalidFormat,
    [Payload<int>] OutOfRange
}

```

### 2. Declare Your Function

Return the generated `ref struct` (`R{MethodName}`) on your method implementation:

```csharp
public RParseInput ParseInput(string input)
{
    if (string.IsNullOrEmpty(input))
        return ParseInputErrors.InvalidFormat("Input cannot be empty");

    // Success path
    return ParseInputErrors.None;
}

```

### 3. Exhaustively Resolve the Result

EVA forces total resolution at the call site:

```csharp
// ❌ EV0001: The return value of 'ParseInput' must be assigned or checked
ParseInput(""); 

// ❌ EV0002: Switch does not handle variant 'OutOfRange'
// ❌ EV0003: Unconsumed payload for 'InvalidFormat'
var result = ParseInput("test");
switch (result.Tag)
{
    case ParseInputErrors.None:
        Console.WriteLine("Success!");
        break;
    case ParseInputErrors.InvalidFormat:
        // Must call result.InvalidFormat() to consume the string payload!
        string msg = result.InvalidFormat(); 
        Console.WriteLine($"Error: {msg}");
        break;
}

```

---

## Roadmap

* **V0 (Current):** `switch` statements, `switch` expressions, result enforcement (`EV0001`–`EV0004`), and Roslyn source generation.
* **V1 (Planned):** Control Flow Graph (CFG) path analysis for `if` / `else` guards, short-circuit boolean evaluation, and cross-statement mutual suppression.