using System.Runtime.CompilerServices;
using HarmonyLib;

namespace ExpandWorldSize;

public class WorldInfo
{
  // Everything used in prefix or postfix should have cached value here.
  // Transpilers use the direct value so doesn't need to be cached.
  public static float WaterLevel = 30f;
  public static float BaseWaterLevel = Helper.HeightToBaseHeight(WaterLevel);
  public static float WorldStretch = 1f;
  public static float BiomeStretch = 1f;
  public static float WaterDepth = 1f;
  public static float AltitudeMultiplier = 1f;
  public static float BaseAltitudeDelta = 0f;
  public static float ForestMultiplier = 1f;


  public static void SetWaterLevel(float waterLevel)
  {
    if (WaterLevel == waterLevel) return;
    WaterLevel = waterLevel;
    BaseWaterLevel = Helper.HeightToBaseHeight(WaterLevel);
  }

  // Seed of the world file, so that clearing the setting can restore it.
  private sealed record OriginalSeed(string Name, int Seed);
  private static readonly ConditionalWeakTable<World, OriginalSeed> OriginalSeeds = new();

  public static void StoreSeed(World world)
  {
    if (world.m_menu) return;
    OriginalSeeds.AddOrUpdate(world, new(world.m_seedName, world.m_seed));
    if (Configuration.Seed != "") ApplySeed(world, Configuration.Seed, Configuration.Seed.GetStableHashCode());
  }

  private static void ApplySeed(World world, string name, int seed)
  {
    world.m_seedName = name;
    world.m_seed = seed;
  }

  public static void RefreshSeed()
  {
    var generator = WorldGenerator.instance;
    if (generator == null) return;
    var world = generator.m_world;
    if (world.m_menu) return;
    if (Configuration.Seed != "") ApplySeed(world, Configuration.Seed, Configuration.Seed.GetStableHashCode());
    else if (OriginalSeeds.TryGetValue(world, out var original)) ApplySeed(world, original.Name, original.Seed);
    else return;
    // Prevents default generate (Biomes step generates).
    world.m_menu = true;
    try { WorldGenerator.Initialize(world); }
    finally { world.m_menu = false; }
  }
  public static void Patch()
  {
    WorldStretch = Configuration.WorldStretch;
    BiomeStretch = Configuration.BiomeStretch;
    WaterDepth = Configuration.WaterDepthMultiplier;
    AltitudeMultiplier = Configuration.AltitudeMultiplier;
    ForestMultiplier = Configuration.ForestMultiplier == 0 ? 1f : Configuration.ForestMultiplier;
    BaseAltitudeDelta = Helper.HeightToBaseHeight(Configuration.AltitudeDelta);
    if (Patcher.WG != null)
      Patcher.WG.maxMarshDistance = VersionSetup.MaxMarshDistance * Configuration.WorldRadius / 10000f / Configuration.WorldStretch;
    EWD.RefreshSize();
    BetterContinents.RefreshSize();
    WorldSizeHelper.GrowBiomeData();
    Patcher.Patch();
  }

}
[HarmonyPatch(typeof(WorldGenerator), nameof(WorldGenerator.VersionSetup))]
public class VersionSetup
{
  // Different value depending on world version so must track it.
  public static float MaxMarshDistance = 6000f;
  static void Prefix(WorldGenerator __instance)
  {
    __instance.maxMarshDistance = 6000f;
  }
  static void Postfix(WorldGenerator __instance)
  {
    MaxMarshDistance = __instance.maxMarshDistance;
  }
}

[HarmonyPatch(typeof(World), nameof(World.LoadWorld))]
public class LoadWorld
{
  static World Postfix(World result)
  {
    WorldInfo.StoreSeed(result);
    return result;
  }
}
