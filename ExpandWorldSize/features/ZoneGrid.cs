using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Service;

namespace ExpandWorldSize;

// Vanilla grid is 512 zones per axis (16384 m radius), anything beyond is saved to a single chunk.
// A chunk is 8x8 zones and the chunk key has one byte per axis, so 2048 zones (65536 m radius) is the most that fits.
public static class ZoneGrid
{
  public const int VanillaWidth = 512;
  public const int ExtendedWidth = 2048;
  public const float VanillaRadius = 16384f;
  public const float ExtendedRadius = 65536f;

  // ZNet.Awake transpiler reads this directly, so it must stay a plain static field.
  public static int Width = VanillaWidth;

  // Zones are 64 m, chunk sizes need a multiple of 64 zones.
  public static int GetWidth()
  {
    var zones = Configuration.SaveGridSize > 0
      ? Configuration.SaveGridSize * 64
      : (int)System.Math.Ceiling(Configuration.WorldTotalRadius / 32f / 64f) * 64;
    return System.Math.Max(VanillaWidth, System.Math.Min(ExtendedWidth, zones));
  }

  public static IEnumerable<CodeInstruction> Patch(IEnumerable<CodeInstruction> instructions, int widths, int halves)
  {
    CodeMatcher matcher = new(instructions);
    if (widths > 0) Helper.ReplaceAllInts(matcher, VanillaWidth, Width, widths);
    if (halves > 0) Helper.ReplaceAllInts(matcher, VanillaWidth / 2, Width / 2, halves);
    return matcher.InstructionEnumeration();
  }

  // Transpilers bake the width into the code, so it's only changed here together with the patches.
  public static void Patch(Harmony harmony, int width)
  {
    if (Width == width) return;
    Width = width;
    foreach (var (target, patch) in Targets())
    {
      var transpiler = AccessTools.Method(patch, "Transpiler");
      harmony.Unpatch(target, transpiler);
      if (width != VanillaWidth) harmony.Patch(target, transpiler: new(transpiler));
    }
  }

  private static IEnumerable<(MethodBase, System.Type)> Targets()
  {
    yield return (AccessTools.Method(typeof(ZoneSystem), nameof(ZoneSystem.SectorToIndex), [typeof(int), typeof(int)]), typeof(SectorToIndexGrid));
    yield return (AccessTools.Method(typeof(ZoneSystem), nameof(ZoneSystem.IndicesToIndex)), typeof(IndicesToIndexGrid));
    yield return (AccessTools.Method(typeof(ZoneSystem), nameof(ZoneSystem.IndexToSector)), typeof(IndexToSectorGrid));
    yield return (AccessTools.Method(typeof(ZoneSystem), nameof(ZoneSystem.GetZonesChunk)), typeof(GetZonesChunkGrid));
    yield return (AccessTools.Method(typeof(ZoneSystem), nameof(ZoneSystem.GetZoneFromChunk)), typeof(GetZoneFromChunkGrid));
    yield return (AccessTools.Method(typeof(ZDOMan), nameof(ZDOMan.GetSaveClonePerChunk)), typeof(GetSaveClonePerChunkGrid));
    yield return (AccessTools.Method(typeof(ZDOMan), nameof(ZDOMan.DecideChunkSize)), typeof(DecideChunkSizeGrid));
    yield return (AccessTools.Method(typeof(ZDOMan), nameof(ZDOMan.AddObjectsPerChunk)), typeof(AddObjectsPerChunkGrid));
  }

  public static void CheckSize()
  {
    if (Patcher.IsMenu) return;
    var radius = Configuration.WorldTotalRadius;
    if (radius > ExtendedRadius)
      Log.Warning($"World size {radius} m is past {ExtendedRadius} m which is the limit of the save system. Everything beyond is saved to a single chunk which causes massive lag.");
  }

  public static void Refresh()
  {
    CheckSize();
    var width = GetWidth();
    // Stale instance from the previous session exists until the new ZNet is created.
    var zdoMan = ZNet.instance ? ZDOMan.instance : null;
    if (zdoMan == null || width == Width) return;
    var server = Helper.IsServer();
    // Save thread uses the grid for the chunk layout and the world file, so it can't change mid-save.
    if (server) WaitForSave();
    Patcher.PatchZoneGrid(width);
    Rebuild(zdoMan);
    if (server)
    {
      Log.Warning($"Grid changed to {Width} zones. Converting the save, old files are removed on the next load.");
      Convert(zdoMan);
    }
  }

  private static void WaitForSave()
  {
    var thread = ZNet.instance.m_saveThread;
    if (thread == null || !thread.IsAlive) return;
    Log.Info("Waiting for the world save to finish before changing the grid.");
    thread.Join();
  }

  // Old chunk files stay on the disk until the next load, so new files must not overwrite them.
  public static uint MinChunkVersion = 0;

  // Chunk files are only containers (objects are placed by position), so a layout change just needs everything saved again.
  public static void Convert(ZDOMan zdoMan)
  {
    foreach (var info in zdoMan.m_chunkSaveMapping.Chunks.Values)
      MinChunkVersion = System.Math.Max(MinChunkVersion, info.m_version + 1);
    var zdos = zdoMan.m_objectsByID.Values;
    // Dedicated server host is at 1000000, so would trigger warning all the time.
    var outsideZdos = zdos.Where(zdo => ZoneSystem.GetSectorIndex(zdo.GetPosition()).Sector == 0u && zdo.m_position.x < 900000).ToList();
    if (outsideZdos.Count > 0)
    {
      int maxSize = Width * 32;
      var furthest = outsideZdos.Max(zdo =>
      {
        var pos = zdo.GetPosition();
        return System.Math.Max(System.Math.Abs(pos.x), System.Math.Abs(pos.z));
      });
      // Setting is multiplied by 64 zones, so one step is 64 * 32 m of radius.
      var suggestedLimit = System.Math.Min(ExtendedWidth / 64, (int)System.Math.Ceiling(furthest / (64 * 32f)));
      Log.Warning($"{outsideZdos.Count} objects are past {maxSize} m and are saved to a single chunk. The furthest is at {furthest:F0} m. Consider increasing the \"Save system size\" setting to {suggestedLimit}.");
    }
    // Empty mapping means no old chunk is reused, so all of them are written with the new keys.
    zdoMan.m_chunkSaveMapping = new ChunkSaveMapping();
    foreach (var zdo in zdos)
      zdoMan.SetDirtySector(zdo);
    zdoMan.SetDirtyPortals();
  }

  // Readonly field can only be set with reflection.
  private static readonly FieldInfo WidthField = AccessTools.Field(typeof(ZDOMan), nameof(ZDOMan.m_width));

  private static void Rebuild(ZDOMan zdoMan)
  {
    WidthField.SetValue(zdoMan, Width);
    zdoMan.ResetSectorArray();
    var zdos = zdoMan.m_objectsByID;
    var portals = zdoMan.m_portalObjects;
    var portalZdos = portals.Values.SelectMany(list => list).ToHashSet();
    portals.Clear();
    foreach (var zdo in zdos.Values)
    {
      var index = ZoneSystem.GetSectorIndex(zdo.GetPosition());
      if (portalZdos.Contains(zdo))
      {
        if (!portals.TryGetValue(index, out var list))
          portals[index] = list = [];
        list.Add(zdo);
        continue;
      }
      // Cached flag for objects stored in the overflow sector.
      zdo.OutsideZones = index.Sector == 0u;
      zdoMan.InitialAddToSector(zdo, index);
    }
  }
}

[HarmonyPatch(typeof(ZNet), nameof(ZNet.Awake))]
public class ZNetAwakeGrid
{
  // World settings must be loaded before anything else uses them.
  static void Prefix()
  {
    var world = ZNet.GetWorldIfIsHost();
    if (world != null) WorldSettings.Apply(world);
    Patcher.PatchZoneGrid(ZoneGrid.GetWidth());
  }

  // Awake is already running when dynamic patches could be applied, so this one is always on.
  // this.m_zdoMan = new ZDOMan(512);
  static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) =>
    Helper.ReplaceAllInts(new(instructions), ZoneGrid.VanillaWidth, AccessTools.Field(typeof(ZoneGrid), nameof(ZoneGrid.Width)), 1).InstructionEnumeration();
}

public class SectorToIndexGrid
{
  static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) => ZoneGrid.Patch(instructions, 3, 2);
}

public class IndicesToIndexGrid
{
  static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) => ZoneGrid.Patch(instructions, 3, 0);
}

public class IndexToSectorGrid
{
  static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) => ZoneGrid.Patch(instructions, 2, 2);
}

public class GetZonesChunkGrid
{
  static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) => ZoneGrid.Patch(instructions, 2, 0);
}

public class GetZoneFromChunkGrid
{
  static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) => ZoneGrid.Patch(instructions, 0, 2);
}

public class GetSaveClonePerChunkGrid
{
  static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
  {
    CodeMatcher matcher = new(instructions);
    var newarr = new CodeMatch(OpCodes.Newarr);
    // Chunks per axis for chunk sizes 0..3 (each size merges 2x2 of the previous one).
    var axis0 = ZoneGrid.Width / 8;
    var axis1 = ZoneGrid.Width / 16;
    var axis2 = ZoneGrid.Width / 32;
    var axis3 = ZoneGrid.Width / 64;
    // new int[4096], new int[1024], new int[256], new int[64]
    matcher = Helper.ReplaceIntBefore(matcher, 4096, newarr, axis0 * axis0);
    matcher = Helper.ReplaceIntBefore(matcher, 1024, newarr, axis1 * axis1);
    matcher = Helper.ReplaceIntBefore(matcher, 256, newarr, axis2 * axis2);
    matcher = Helper.ReplaceIntBefore(matcher, 64, newarr, axis3 * axis3);
    // index < 512U
    matcher = Helper.ReplaceAllInts(matcher, ZoneGrid.VanillaWidth, ZoneGrid.Width, 2);
    // DecideChunkSize(size, chunkSize, ...) is identified by the chunk size that follows.
    matcher = Helper.ReplaceIntBefore(matcher, 64, Helper.Int(1), axis0);
    matcher = Helper.ReplaceIntBefore(matcher, 32, Helper.Int(2), axis1);
    matcher = Helper.ReplaceIntBefore(matcher, 16, Helper.Int(3), axis2);
    // AddObjectsPerChunk(size, chunkSize, ...)
    matcher = Helper.ReplaceIntBefore(matcher, 64, Helper.Int(0), axis0);
    matcher = Helper.ReplaceIntBefore(matcher, 32, Helper.Int(1), axis1);
    matcher = Helper.ReplaceIntBefore(matcher, 16, Helper.Int(2), axis2);
    matcher = Helper.ReplaceIntBefore(matcher, 8, Helper.Int(3), axis3);
    return matcher.InstructionEnumeration();
  }
}

// uint num1 = 64U / size;
public class DecideChunkSizeGrid
{
  static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
  {
    CodeMatcher matcher = new(instructions);
    matcher = Helper.ReplaceAllInts(matcher, 64, ZoneGrid.Width / 8, 1);
    return matcher.InstructionEnumeration();
  }
}

// int num = 64 / size;
public class AddObjectsPerChunkGrid
{
  static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
  {
    CodeMatcher matcher = new(instructions);
    matcher = Helper.ReplaceAllInts(matcher, 64, ZoneGrid.Width / 8, 1);
    return matcher.InstructionEnumeration();
  }
}

[HarmonyPatch(typeof(ZDOMan), nameof(ZDOMan.LoadChunks))]
public class ConvertSaveGrid
{
  static void Postfix(ZDOMan __instance)
  {
    ZoneGrid.MinChunkVersion = 0;
    var world = ZNet.World;
    // Overflow chunk must be converted too, since its objects may belong to other chunks in the new grid.
    if (world == null || __instance.m_objectsByID.Count == 0) return;
    var savedWidth = WorldSettings.SavedGridWidth(world);
    if (savedWidth == ZoneGrid.Width) return;
    Log.Warning($"Save uses a {savedWidth} zone grid but the grid is {ZoneGrid.Width}. Converting the save, old files are removed on the next load.");
    ZoneGrid.Convert(__instance);
  }
}

[HarmonyPatch(typeof(ChunkSaveMapping), nameof(ChunkSaveMapping.CreateOrUpdate))]
public class ChunkVersionGrid
{
  static void Postfix(ChunkSaveMapping __instance, ZoneSystem.ChunkIndex chunkIndex)
  {
    if (ZoneGrid.MinChunkVersion == 0) return;
    var info = __instance.Get(chunkIndex);
    if (info != null && info.m_version < ZoneGrid.MinChunkVersion)
      info.m_version = ZoneGrid.MinChunkVersion;
  }
}
