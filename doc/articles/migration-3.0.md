# Migrating to Dapplo.Windows 3.0

Dapplo.Windows 3.0 fixes many interop bugs. Where an API encoded a wrong concept, it was changed instead of kept for
compatibility. This page lists every breaking change with the code you need to update. The full list of changes is in
the [changelog](../../CHANGELOG.md).

## Window information and scrolling

### `Fill()` caches again

`InteropWindowExtensions.Fill()` used to ignore its settings and always refresh, auto-correct and query the
maximized state. It now does what the flags say.

- Pass `InteropWindowRetrieveSettings.ForceUpdate` when you need fresh values.
- `CacheAll`, `CacheAllWithChildren` and `CacheAllChildZorder` no longer auto-correct bounds. Use `CacheAllAutoCorrect`
  or add `AutoCorrectValues`.

### Wheel scroll lines

```csharp
// 2.x
int lines = WindowScroller.ScrollWheelLinesFromRegistry;
// 3.0: 0 means the wheel doesn't scroll, uint.MaxValue means one notch scrolls a page
uint lines = WindowScroller.ScrollWheelLines;
int delta = WindowScroller.CalculateWheelDelta(pageSize, lines);
```

## Geometry

`NativeRectExtensions.Intersect2` is removed; it returned the union on the Y axis. Use `Intersect`, which returns
`NativeRect.Empty` when the rectangles don't overlap.

```csharp
// 2.x
var overlap = rect1.Intersect2(rect2);
// 3.0
var overlap = rect1.Intersect(rect2);
```

## Kernel32

`ProcessAccessRights.QueryLimitedInformation` had the value of `QueryInformation` (0x400). It is now 0x1000, which also
works for elevated processes. If you relied on the full query right, use `QueryInformation`. `All` is now the
Vista-and-later value 0x1FFFFF.

## Multimedia

`WinMm.Play(byte[])` returns `bool` and copies the data, so you no longer need to keep the array pinned. Callers must
recompile.
