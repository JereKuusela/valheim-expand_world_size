using HarmonyLib;
using Service;
using UnityEngine;

namespace ExpandWorldSize;

[HarmonyPatch]
public static class Patcher
{
  public static WorldGenerator? WG;
  public static bool IsMenu => WG == null || WG.m_world.m_menu;
#nullable disable
  private static Harmony Harmony;
  private static Harmony DynamicHarmony;
#nullable enable
  public static void Init(Harmony harmony, Harmony dynamicHarmony)
  {
    Harmony = harmony;
    DynamicHarmony = dynamicHarmony;
    Patch();
  }
  public static void Patch()
  {
    Harmony.UnpatchSelf();
    Harmony.PatchAll();
    CheckWorldStretch(DynamicHarmony);
    CheckWaterDepth(DynamicHarmony);
    CheckForest(DynamicHarmony);
    CheckAltitude(DynamicHarmony);
    CreateAshlandsGap.Patch(DynamicHarmony, !Configuration.AshlandsGap);
    CreateDeepNorthGap.Patch(DynamicHarmony, !Configuration.DeepNorthGap);
    GetAshlandsHeight.Patch(DynamicHarmony, Configuration.AshlandsWidthRestriction, Configuration.AshlandsLengthRestriction);
  }

  [HarmonyPatch(typeof(WorldGenerator), nameof(WorldGenerator.VersionSetup)), HarmonyPostfix, HarmonyPriority(Priority.Last)]
  static void PatchOnLoad(WorldGenerator __instance)
  {
    WG = __instance;
    WorldInfo.Refresh();
  }

  private static float PatchedWorldStretch = 1f;
  private static void CheckWorldStretch(Harmony harmony)
  {
    var worldStretch = IsMenu ? 1f : WorldInfo.WorldStretch;
    if (PatchedWorldStretch == worldStretch) return;
    PatchedWorldStretch = worldStretch;
    var method = AccessTools.Method(typeof(WorldGenerator), nameof(WorldGenerator.GetBiomeHeight));
    var patch = AccessTools.Method(typeof(Stretch), nameof(Stretch.GetBiomeHeight));
    harmony.Unpatch(method, patch);
    if (worldStretch != 1f)
      harmony.Patch(method, prefix: new(patch));
    method = AccessTools.Method(typeof(WorldGenerator), nameof(WorldGenerator.GetAshlandsOceanGradient), [typeof(Vector3)]);
    patch = AccessTools.Method(typeof(Stretch), nameof(Stretch.GetAshlandsOceanGradient));
    harmony.Unpatch(method, patch);
    if (worldStretch != 1f)
      harmony.Patch(method, prefix: new(patch));
    method = AccessTools.Method(typeof(Minimap), nameof(Minimap.GetMaskColor));
    patch = AccessTools.Method(typeof(Stretch), nameof(Stretch.GetMaskColor));
    harmony.Unpatch(method, patch);
    if (worldStretch != 1f)
      harmony.Patch(method, prefix: new(patch));
    method = AccessTools.Method(typeof(WorldGenerator), nameof(WorldGenerator.GetBiome), [typeof(float), typeof(float), typeof(float), typeof(bool)]);
    patch = AccessTools.Method(typeof(Stretch), nameof(Stretch.GetBiome));
    harmony.Unpatch(method, patch);
    if (worldStretch != 1f)
      harmony.Patch(method, prefix: new(patch));
    method = AccessTools.Method(typeof(WorldGenerator), nameof(WorldGenerator.AddRivers));
    patch = AccessTools.Method(typeof(AddRivers), nameof(AddRivers.Prefix));
    harmony.Unpatch(method, patch);
    if (worldStretch != 1f)
      harmony.Patch(method, prefix: new(patch));
    PatchAshlandsDeepNorthChecks(harmony, worldStretch);
  }
  private static void PatchAshlandsDeepNorthChecks(Harmony harmony, float worldStretch)
  {
    var method = AccessTools.Method(typeof(Character), nameof(Character.UpdateLava));
    var patch = AccessTools.Method(typeof(Stretch), nameof(Stretch.StretchIsAshlandsTranspiler));
    harmony.Unpatch(method, patch);
    if (worldStretch != 1f)
      harmony.Patch(method, transpiler: new(patch));
    method = AccessTools.Method(typeof(EnvMan), nameof(EnvMan.GetBiome));
    patch = AccessTools.Method(typeof(Stretch), nameof(Stretch.StretchIsAshlandsDeepNorthTranspiler));
    harmony.Unpatch(method, patch);
    if (worldStretch != 1f)
      harmony.Patch(method, transpiler: new(patch));
    method = AccessTools.Method(typeof(EnvMan), nameof(EnvMan.UpdateEnvironment));
    patch = AccessTools.Method(typeof(Stretch), nameof(Stretch.StretchIsAshlandsDeepNorthTranspiler));
    harmony.Unpatch(method, patch);
    if (worldStretch != 1f)
      harmony.Patch(method, transpiler: new(patch));
  }
  private static float PatchedWaterDepthMultiplier = 1f;
  private static float PatchedWaterLevel = 30f;
  private static void CheckWaterDepth(Harmony harmony)
  {
    var waterDepthMultiplier = IsMenu ? 1f : WorldInfo.WaterDepth;
    if (PatchedWaterDepthMultiplier == waterDepthMultiplier && WorldInfo.WaterLevel == PatchedWaterLevel) return;
    PatchedWaterDepthMultiplier = waterDepthMultiplier;
    PatchedWaterLevel = WorldInfo.WaterLevel;
    var method = AccessTools.Method(typeof(WorldGenerator), nameof(WorldGenerator.GetBiomeHeight));
    var patch = AccessTools.Method(typeof(BiomeHeight), nameof(BiomeHeight.Postfix));
    harmony.Unpatch(method, patch);
    // Water level is used in the patch but doesn't do anything without the multiplier.
    if (waterDepthMultiplier == 1f) return;
    harmony.Patch(method, postfix: new(patch));
  }
  private static float PatchedForestMultiplier = 1f;
  private static void CheckForest(Harmony harmony)
  {
    var forestMultiplier = IsMenu ? 1f : WorldInfo.ForestMultiplier;
    if (PatchedForestMultiplier == forestMultiplier) return;
    PatchedForestMultiplier = forestMultiplier;
    var method = AccessTools.Method(typeof(WorldGenerator), nameof(WorldGenerator.GetForestFactor));
    var patch = AccessTools.Method(typeof(Forest), nameof(Forest.Postfix));
    harmony.Unpatch(method, patch);
    if (forestMultiplier == 1f) return;
    harmony.Patch(method, postfix: new(patch));
  }
  private static float PatchedAltitudeDelta = 0f;
  private static float PatchedAltitudeMultiplier = 1f;
  private static void CheckAltitude(Harmony harmony)
  {
    var altitudeDelta = IsMenu ? 0f : WorldInfo.BaseAltitudeDelta;
    var altitudeMultiplier = IsMenu ? 1f : WorldInfo.AltitudeMultiplier;
    if (PatchedAltitudeDelta == altitudeDelta && PatchedAltitudeMultiplier == altitudeMultiplier) return;
    PatchedAltitudeDelta = altitudeDelta;
    PatchedAltitudeMultiplier = altitudeMultiplier;
    var method = AccessTools.Method(typeof(WorldGenerator), nameof(WorldGenerator.GetBaseHeight));
    var patch = AccessTools.Method(typeof(BaseHeight), nameof(BaseHeight.Postfix));
    harmony.Unpatch(method, patch);
    if (altitudeDelta == 0f && altitudeMultiplier == 1f) return;
    harmony.Patch(method, postfix: new(patch));
  }
}
