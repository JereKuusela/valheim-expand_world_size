
using System.Collections.Generic;
using HarmonyLib;

namespace ExpandWorldSize;

public static class Patcher
{

  public static float WaterLevel = 30;
  public static float BaseWaterLevel = 0f;
  public static float BaseAltitudeDelta = 0f;

  public static void Patch(Harmony harmony)
  {
    harmony.UnpatchSelf();
    harmony.PatchAll();
    CheckWorldStretch(harmony);
    CheckWaterDepth(harmony);
    CheckForest(harmony);
  }

  private static void UpdateCache()
  {
    BaseWaterLevel = Helper.HeightToBaseHeight(WaterLevel);
    BaseAltitudeDelta = Helper.HeightToBaseHeight(Configuration.AltitudeDelta);
  }

  private static void CheckWorldStretch(Harmony harmony)
  {
    if (WorldGenerator.instance != null && !WorldGenerator.instance.m_world.m_menu && Configuration.WorldStretch != 1f) return;
    var method = AccessTools.Method(typeof(WorldGenerator), nameof(WorldGenerator.GetBiomeHeight));
    var patch = AccessTools.Method(typeof(BiomeHeight), nameof(Stretch.GetBiomeHeight));
    harmony.Unpatch(method, patch);
    method = AccessTools.Method(typeof(WorldGenerator), nameof(WorldGenerator.GetBiome), new[] { typeof(float), typeof(float) });
    patch = AccessTools.Method(typeof(BiomeHeight), nameof(Stretch.GetBiome));
    harmony.Unpatch(method, patch);
    method = AccessTools.Method(typeof(WorldGenerator), nameof(WorldGenerator.AddRivers));
    patch = AccessTools.Method(typeof(BiomeHeight), nameof(AddRivers.Prefix));
    harmony.Unpatch(method, patch);
  }
  private static void CheckWaterDepth(Harmony harmony)
  {
    if (WorldGenerator.instance != null && !WorldGenerator.instance.m_world.m_menu && Configuration.WaterDepthMultiplier != 1f) return;
    var method = AccessTools.Method(typeof(WorldGenerator), nameof(WorldGenerator.GetBiomeHeight));
    var patch = AccessTools.Method(typeof(BiomeHeight), nameof(BiomeHeight.Postfix));
    harmony.Unpatch(method, patch);
  }
  private static void CheckForest(Harmony harmony)
  {
    if (WorldGenerator.instance != null && !WorldGenerator.instance.m_world.m_menu && Configuration.ForestMultiplier != 1f && Configuration.ForestMultiplier != 0f) return;
    var method = AccessTools.Method(typeof(WorldGenerator), nameof(WorldGenerator.GetForestFactor));
    var patch = AccessTools.Method(typeof(Forest), nameof(Forest.Postfix));
    harmony.Unpatch(method, patch);
  }
  private static void CheckAltitude(Harmony harmony)
  {
    if (WorldGenerator.instance != null && !WorldGenerator.instance.m_world.m_menu && (Configuration.AltitudeDelta != 0f || Configuration.AltitudeMultiplier != 1f)) return;
    var method = AccessTools.Method(typeof(WorldGenerator), nameof(WorldGenerator.GetBaseHeight));
    var patch = AccessTools.Method(typeof(BaseHeight), nameof(BaseHeight.Postfix));
    harmony.Unpatch(method, patch);
  }
}
