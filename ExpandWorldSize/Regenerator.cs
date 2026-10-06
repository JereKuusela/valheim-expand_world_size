using System;
using Service;
using UnityEngine;
using UnityEngine.Rendering;

namespace ExpandWorldSize;

[Flags]
public enum Regen
{
  None = 0,
  // Cached values and dynamic patches.
  Patches = 1,
  ZoneGrid = 2,
  Minimap = 4,
  Water = 8,
  Seed = 16,
  World = 32,
}

// Central place for reacting to setting changes.
public static class Regenerator
{
  // Disable to bulk edit settings and then call Run.
  public static bool Automatic = true;
  // All changes are debounced and applied together to keep everything in sync.
  private static Regen Pending = Regen.None;

  public static void Request(Regen target)
  {
    // All settings affect patches and quick to apply anyways.
    Pending |= Regen.Patches;
    Pending |= target;
    if (!Automatic) return;
    EWS.Instance.Debounce();
  }

  // Skipped targets are dropped.
  public static void Run()
  {
    if (Pending == Regen.None) return;
    var target = Pending;
    Pending = Regen.None;
    Execute(target);
  }

  private static void Execute(Regen target)
  {
    if (target.HasFlag(Regen.Patches)) WorldInfo.Patch();
    if (target.HasFlag(Regen.ZoneGrid)) ZoneGrid.Refresh();
    if (target.HasFlag(Regen.Water)) WaterColor.Refresh();
    if (target.HasFlag(Regen.Minimap) && Minimap.instance)
      MinimapAwake.Refresh(Minimap.instance);
    if (Patcher.IsMenu) return;
    if (target.HasFlag(Regen.Seed)) RefreshSeed();
    if (target.HasFlag(Regen.World)) WorldInfo.Generate();
    if (target.HasFlag(Regen.Seed) || target.HasFlag(Regen.World) || target.HasFlag(Regen.Minimap)) RefreshMap();
  }

  private static void RefreshSeed()
  {
    if (Configuration.Seed == "") return;
    if (WorldGenerator.instance == null) return;
    var world = WorldGenerator.instance.m_world;
    if (world.m_menu) return;
    world.m_seedName = Configuration.Seed;
    world.m_seed = Configuration.Seed.GetStableHashCode();
    // Prevents default generate (World target generates).
    world.m_menu = true;
    WorldGenerator.Initialize(world);
    world.m_menu = false;
  }
  private static void RefreshMap()
  {
    MapGeneration.Cancel();
    if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null)
      Minimap.instance?.GenerateWorldMap();
  }
}
