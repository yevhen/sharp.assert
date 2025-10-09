- Move all tests to integration tests and test only via front door (Assert)
- Expose Assert(() => x => y) overload method in Sharp class to delegate to included PowerAssert directly
- Review dynamic support - make sure that dynamic = {...} objects are properly formatted
- Custom formatters
- Custom comparisons

----

Dynamic - only implement (general/binary). Need to be tested/extended to work properly with objects/collections/expando
```csharp
dynamic json = JsonSerializer.Deserialize<dynamic>(jsonString);
Console.WriteLine(json.user.name);
```

```csharp
dynamic person = new ExpandoObject();
person.Name = "Yevhen";
person.Age = 42;
Console.WriteLine($"{person.Name} ({person.Age})");
```

----

**External diff viewers** - this is important to implement.
- Need proper research. In pycharm when pytest assert fails it shows nice diff right inside ide using native means.
- It would be cool impl similar for large multiline strings, or maps, etc. Need to think where this could be useful.

----

**Debugging the Rewriter** - not sure what value this will bring and what changes these requires.
To facilitate debugging, the rewrite task will support a diagnostic MSBuild property:

```xml
<PropertyGroup>
  <SharpAssertEmitRewriteInfo>true</SharpAssertEmitRewriteInfo>
</PropertyGroup>
```

When enabled, the rewriter will output detailed logs of its analysis and decisions. This is crucial for troubleshooting unexpected rewrite behavior.

----

**Dependencies**
- PowerAssert (current) — is not used anymore as automatic fallback for unsupported features (need to find all references to this fact and update)
- DiffPlex — string & sequence diffs
- Compare‑Net‑Objects — deep object diffs
- Verify.*  — external diff tooling

All these packages need to be properly attributed in README.md

----

**Rewriter Robustness & Fallback - pending**

- Wrap rewriter logic in try-catch
- If analysis fails, leave original Assert call
- Add diagnostic logging controlled by MSBuild property - this looks like 4.3
- Test with edge cases and invalid code 