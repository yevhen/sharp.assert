# Package Rename Plan: SharpAssert Main Package Strategy

## Status: ✅ COMPLETED

## Overview
Restructure packages to make "SharpAssert" the main branded package that users install, with the runtime as a dependency.

## Current Architecture
```
SharpAssert (runtime) + SharpAssert.Rewriter (MSBuild task)
├── User installs both packages
└── Two separate references required
```

## New Architecture
```
SharpAssert (MSBuild task + dependency on runtime)
├── User installs single "SharpAssert" package
├── Automatically references SharpAssert.Runtime (transitive)
└── Single branded entry point
```

## Benefits
✅ **Single Package Installation** - Users just install "SharpAssert"  
✅ **Clear Branding** - Main package has the project name  
✅ **Automatic Dependencies** - Runtime comes as transitive dependency  
✅ **Better Discoverability** - Users find and install the right package  
✅ **Clearer Responsibilities** - ".Runtime" suffix clearly indicates role

## Package Structure After Rename

### SharpAssert.Runtime (formerly SharpAssert)
- **Location**: `SharpAssert/` → `SharpAssert.Runtime/`
- **Package ID**: SharpAssert.Runtime
- **Contents**: Core assertion library (Sharp.cs, SharpInternal.cs, etc.)
- **Dependencies**: PowerAssert
- **Type**: Standard library package

### SharpAssert (formerly SharpAssert.Rewriter)
- **Location**: `SharpAssert.Rewriter/` → `SharpAssert/`
- **Package ID**: SharpAssert
- **Contents**: MSBuild task for source rewriting
- **Dependencies**: 
  - SharpAssert.Runtime (as PackageReference - users get it automatically)
  - Microsoft.Build.* (PrivateAssets="all")
  - Microsoft.CodeAnalysis.* (PrivateAssets="all")
- **Type**: MSBuild task package with runtime dependency

## User Experience

### Before (Current)
```xml
<PackageReference Include="SharpAssert" Version="1.0.0" />
<PackageReference Include="SharpAssert.Rewriter" Version="1.0.0" />
```

### After (New)
```xml
<PackageReference Include="SharpAssert" Version="1.0.0" />
<!-- SharpAssert.Runtime comes automatically as transitive dependency -->
```

## Implementation Steps

### 1. Rename Directories ✅ COMPLETED
```bash
mv SharpAssert SharpAssert.Runtime
mv SharpAssert.Rewriter SharpAssert
```

### 2. Update Project Files ✅ COMPLETED

#### SharpAssert.Runtime/SharpAssert.Runtime.csproj ✅
- ✅ Change `<PackageId>` to `SharpAssert.Runtime`
- ✅ Update `<Title>` and `<Description>`
- ✅ Keep all other properties

#### SharpAssert/SharpAssert.csproj (formerly Rewriter) ✅
- ✅ Change `<PackageId>` to `SharpAssert`
- ✅ Add dependency: `<PackageReference Include="SharpAssert.Runtime" Version="$(Version)" />`
- ✅ Update `<Title>` and `<Description>`
- ✅ Keep MSBuild task configuration

### 3. Update MSBuild Targets ✅ COMPLETED
- ✅ Rename `SharpAssert.Rewriter.targets` → `SharpAssert.targets`
- ✅ Update assembly references in targets file
- ✅ Ensure runtime DLL is available via transitive dependency

### 4. Update Solution File ✅ COMPLETED
- ✅ Rename project references
- ✅ Update project GUIDs if needed

### 5. Update Test Projects ✅ COMPLETED
- ✅ SharpAssert.Tests → Reference SharpAssert.Runtime
- ✅ IntegrationTests → Reference SharpAssert (gets Runtime transitively)
- ✅ PackageTest → Reference SharpAssert only
- ✅ PowerAssertTest → Reference SharpAssert only

### 6. Update Build Scripts ✅ COMPLETED
- ✅ publish-local.sh: Update project paths
- ✅ publish-nuget.sh: Update project paths
- ✅ test-local.sh: Update references

### 7. Update Documentation ✅ COMPLETED
- ✅ README.md: Update installation instructions
- ✅ CONTRIBUTING.md: Update development setup

## Technical Considerations

### Dependency Flow
```
User Project
  └── SharpAssert (NuGet package)
      ├── MSBuild task (tools/net9.0/)
      ├── MSBuild targets (build/)
      └── SharpAssert.Runtime (transitive dependency)
          └── PowerAssert (transitive dependency)
```

### Version Synchronization
- Both packages must always have same version
- Use `$(Version)` property in PackageReference to ensure sync
- Single version update in Directory.Build.props

### MSBuild Task Loading
- Task DLL loads from tools/net9.0/
- Runtime DLL available via standard reference resolution
- No changes needed to task implementation

## Risks & Mitigations

### Risk: Breaking Change for Existing Users
**Mitigation**: 
- Document migration clearly
- Consider publishing deprecated packages with warning for transition period
- Provide migration guide

### Risk: Transitive Dependency Issues
**Mitigation**:
- Test thoroughly with PackageTest project
- Ensure proper dependency flow in all scenarios
- Validate MSBuild task can find runtime assembly

### Risk: NuGet Package Confusion
**Mitigation**:
- Clear package descriptions
- Deprecate old packages with forwarding message
- Update all documentation immediately

## Migration Guide for Users

### For Existing Users
```xml
<!-- Old (remove these) -->
<PackageReference Include="SharpAssert" Version="0.9.0" />
<PackageReference Include="SharpAssert.Rewriter" Version="0.9.0" />

<!-- New (add this) -->
<PackageReference Include="SharpAssert" Version="1.0.0" />
```

### For New Users
Simply install the SharpAssert package - everything else is automatic.

## Testing Strategy

1. **Local Package Test**: Verify single package installation works
2. **Integration Test**: Ensure MSBuild task finds runtime
3. **PowerAssert Test**: Confirm fallback mechanism works
4. **Version Test**: Validate transitive dependency resolution
5. **Clean Environment Test**: Test on machine without local builds

## Success Criteria

✅ Single `<PackageReference Include="SharpAssert" />` provides full functionality  
✅ All existing tests pass without modification (except references)  
✅ MSBuild rewriting works with transitive runtime dependency  
✅ PowerAssert fallback continues to function  
✅ Package installation is simpler and more intuitive  

## Timeline

1. **Phase 1**: Directory and file renames (30 min)
2. **Phase 2**: Project file updates (45 min)
3. **Phase 3**: Test project updates (30 min)
4. **Phase 4**: Script updates (15 min)
5. **Phase 5**: Testing and validation (1 hour)
6. **Phase 6**: Documentation updates (30 min)

**Total Estimated Time**: 3-4 hours

## Decision

**Proceed with rename** - This architecture provides:
- Better user experience (single package)
- Clearer branding (SharpAssert as main package)
- Maintains clean separation (runtime vs build-time)
- Reduces installation friction significantly

The transitive dependency approach is well-established in .NET (e.g., ASP.NET Core metapackages) and will work reliably.