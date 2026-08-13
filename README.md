# EVA — Error Values for C#

EVA is a Roslyn source generator and analyzer pair that turns enums into stack-allocated, discriminated union-style result types. It gives you exhaustiveness checking and mandatory payload extraction without runtime allocation or boxing.

## What it does

Annotate an enum with error states:

```csharp
[ErrorValues(nameof(ParseUser))]
public enum ParseError
{
    InvalidFormat,
    OutOfRange,
    [Payload<string>(happy: true)] Success
}
```

EVA generates a `ref struct` result type and enforces—at compile time—that every call site handles every case:

```csharp
RParseUser<User> result = ParseUser(input);

switch (result)
{
    case var r when r.IsInvalidFormat: /* ... */ break;
    case var r when r.IsOutOfRange:   /* ... */ break;
    case var r when r.IsSuccess:      var user = r.Success(); break;
}
```

Miss a case? The analyzer errors. Forget to extract a payload? The analyzer errors. Discard the result? The analyzer errors.

## Architecture

| Component | Responsibility |
|-----------|---------------|
| **Source Generator** | Scans `[ErrorValues]` enums and emits `R{EnumName}<T>` ref structs with typed storage, factory methods, and accessors. |
| **Analyzer** | Enforces exhaustiveness, payload consumption, result usage, and method return-type correctness. |

The generated result is a `ref struct`. It lives on the stack, cannot be boxed, and imposes no GC pressure.

## Attributes

### `[ErrorValues(string methodName)]`
Applied to an enum. Associates the enum with a method (by name) in the containing type and triggers generation of the result struct `R{EnumName}<T>`.

### `[Payload<T>(bool happy = false)]`
Applied to an enum field. Declares that this variant carries a payload of type `T`. If `happy = true`, the variant is treated as a success case.

### `[Generation(string enumName)]`
Applied to generated structs only. Marks them for analyzer recognition. Do not use manually.

## Analyzer Rules

| ID | Severity | Description |
|----|----------|-------------|
| `EVA0001` | Error | Result of a method returning a generated ref struct must be used. Cannot be silently discarded. |
| `EVA0002` | Error | Switch statement or expression does not handle all enum variants. |
| `EVA0003` | Error | Switch case for a payload-carrying variant does not invoke the payload accessor. |
| `EVA0004` | Warning | Method referenced by `[ErrorValues]` does not return the generated result struct. |

## Generated Code Shape

For an enum `MyError` with `[ErrorValues(nameof(DoWork))]`:

```csharp
[Generation(nameof(MyError))]
internal readonly struct RMyError<T> : IEVA<T>
{
    private readonly MyError Tag;
    private readonly Storage _storage;
    private readonly bool _happy;

    public bool Happy => _happy;

    // Factory methods
    internal static RDoWork<T> InvalidFormat() => ...;
    internal static RDoWork<T> OutOfRange()   => ...;
    internal static RDoWork<T> Success(string payload) => ...;

    // Predicates
    public bool IsInvalidFormat => MyError.InvalidFormat == Tag;
    public bool IsOutOfRange    => MyError.OutOfRange == Tag;
    public bool IsSuccess       => MyError.Success == Tag;

    // Payload accessors (throw on tag mismatch)
    internal string Success() => Tag == MyError.Success ? _storage.Success : throw ...;

    // Sequential storage struct
    [StructLayout(LayoutKind.Sequential)]
    internal struct Storage
    {
        public string Success;
    }
}
```

## Design Notes

- **Sequential storage.** The `Storage` struct contains all payload fields laid out sequentially. This is safe and simple, though not a true C-style union. For small error payloads the overhead is negligible.
- **Ref struct constraints.** Because results are `ref struct`, they cannot be stored in fields, arrays, or used as generic type arguments. They are strictly for stack-local error handling.
- **Method binding by name.** The `[ErrorValues]` attribute binds to a method by string name. Renaming the method will stale the binding; the analyzer's `EVA0004` catches this.
- **Payload accessors throw on mismatch.** The analyzer enforces correct access patterns at compile time. Runtime validation exists in debug builds as a safety net.

## Status

Experimental. Core generator and analyzer are functional. The generic parameter `T` on `IEVA<T>` and the result struct is reserved for future use.

## License

MIT