- ALWAYS use constants to declare variable that are never updated
```csharp
    // BAD
    var left = true;
    var right = false;
    Expression<Func<bool>> expr = () => left && right;
```
```csharp
    // GOOD
    const left = true;
    const right = false;
    Expression<Func<bool>> expr = () => left && right;
```