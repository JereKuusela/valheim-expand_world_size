using System;
using System.Diagnostics;
using HarmonyLib;
using UnityEngine;

namespace ExpandWorldSize;

[HarmonyPatch]
public static class Patcher
{
  public static WorldGenerator? WG;
  public static bool IsMenu => WG == null || WG.m_world.m_menu;
#nullable disable
  public static Harmony Harmony;
#nullable enable
  public static void Init(Harmony harmony)
  {
    Harmony = harmony;
    // Everything that depends on the config is applied dynamically in Patch.
    Harmony.PatchAll();
    Patch();
  }

  private const HarmonyPatchType Prefix = HarmonyPatchType.Prefix;
  private const HarmonyPatchType Postfix = HarmonyPatchType.Postfix;
  private const HarmonyPatchType Transpiler = HarmonyPatchType.Transpiler;
  private const HarmonyPatchType Finalizer = HarmonyPatchType.Finalizer;
  private static readonly Type[] GetBiomeArgs = [typeof(float), typeof(float), typeof(float), typeof(bool)];
  private static readonly Type[] GetBiomeSectorArgs = [typeof(int), typeof(int), typeof(bool)];
  private static readonly Type[] GenerateLocationsArgs = [typeof(ZoneSystem.ZoneLocation), typeof(Stopwatch), typeof(ZPackage)];

  // Skipped patches are removed, so vanilla values leave the game code untouched.
  private static void Apply(bool shouldPatch, Type type, string name, Type patchType, string patchName, HarmonyPatchType kind, object? state = null, Type[]? args = null, bool enumerator = false)
    => Patches.Apply(Harmony, shouldPatch, type, name, patchType, patchName, kind, null, args, enumerator, state);

  public static void Patch()
  {
    var menu = IsMenu;
    var bc = BetterContinents.IsEnabled();
    var radius = Configuration.WorldRadius;
    var total = Configuration.WorldTotalRadius;
    var stretch = Configuration.WorldStretch;
    var biomeStretch = Configuration.BiomeStretch;
    var stretchedRadius = Configuration.StrechedWorldRadius;
    var stretchedTotal = Configuration.StrechedWorldTotalRadius;
    var biomeMapSize = WorldSizeHelper.BiomeMapSize;
    var radiusChanged = radius != 10000f;
    var totalChanged = total != 10500f;
    var biomeMapChanged = biomeMapSize != 2048 || stretch != 1f || totalChanged;

    Stretch.UpdateAshlandsLimits(menu);

    PatchWorldStretch(menu, stretch, biomeStretch, stretchedRadius);
    PatchHeights(menu, bc, radius, total, stretch, stretchedRadius, stretchedTotal);
    PatchPoles(menu, stretchedRadius);
    PatchWorldSize(bc, radius, total, stretch, biomeMapSize, biomeMapChanged, totalChanged);
    PatchRivers(menu, radius, stretch, radiusChanged);
    PatchLocations(radius, radiusChanged);
  }

  public static void PatchZoneGrid(int width) => ZoneGrid.Patch(Harmony, width);

  [HarmonyPatch(typeof(WorldGenerator), nameof(WorldGenerator.VersionSetup)), HarmonyPostfix, HarmonyPriority(Priority.Last)]
  static void PatchOnLoad(WorldGenerator __instance)
  {
    WG = __instance;
    WorldInfo.Patch();
  }

  private static void PatchWorldStretch(bool menu, float stretch, float biomeStretch, float stretchedRadius)
  {
    var stretched = !menu && stretch != 1f;
    var biomeChanged = !menu && (biomeStretch != 1f || stretchedRadius != 10000f);
    Apply(stretched, typeof(WorldGenerator), nameof(WorldGenerator.GetBiomeHeight), typeof(Stretch), nameof(Stretch.GetBiomeHeight), Prefix);
    Apply(stretched, typeof(WorldGenerator), nameof(WorldGenerator.GetAshlandsOceanGradient), typeof(Stretch), nameof(Stretch.StretchVector3), Prefix, args: [typeof(Vector3)]);
    Apply(stretched, typeof(Minimap), nameof(Minimap.GetMaskColor), typeof(Stretch), nameof(Stretch.GetMaskColor), Prefix);
    Apply(stretched, typeof(WorldGenerator), nameof(WorldGenerator.GetBiome), typeof(Stretch), nameof(Stretch.GetBiome), Prefix, args: GetBiomeArgs);
    Apply(biomeChanged, typeof(WorldGenerator), nameof(WorldGenerator.GetBiome), typeof(Stretch), nameof(Stretch.Transpiler), Transpiler, (biomeStretch, stretchedRadius), GetBiomeArgs);
    Apply(stretched, typeof(WorldGenerator), nameof(WorldGenerator.AddRivers), typeof(AddRivers), nameof(AddRivers.Prefix), Prefix);
    Apply(stretched, typeof(AltBiomeWorldData), nameof(AltBiomeWorldData.GenerateAltBiomes), typeof(Stretch), nameof(Stretch.PrefixGenerateAltBiomes), Prefix);
    Apply(stretched, typeof(AltBiomeWorldData), nameof(AltBiomeWorldData.GenerateAltBiomes), typeof(Stretch), nameof(Stretch.FinalizerGenerateAltBiomes), Finalizer);
    Apply(stretched, typeof(Character), nameof(Character.UpdateLava), typeof(Stretch), nameof(Stretch.StretchIsAshlandsTranspiler), Transpiler, stretch);
    Apply(stretched, typeof(EnvMan), nameof(EnvMan.GetBiome), typeof(Stretch), nameof(Stretch.StretchIsAshlandsDeepNorthTranspiler), Transpiler, stretch);
    Apply(stretched, typeof(EnvMan), nameof(EnvMan.UpdateEnvironment), typeof(Stretch), nameof(Stretch.StretchIsAshlandsDeepNorthTranspiler), Transpiler, stretch);
  }

  private static void PatchHeights(bool menu, bool bc, float radius, float total, float stretch, float stretchedRadius, float stretchedTotal)
  {
    var heightSeed = Configuration.HeightSeed;
    var seeded = !menu && heightSeed != null;
    var offsetX = Configuration.OffsetX;
    var offsetY = Configuration.OffsetY;
    var baseHeightChanged = offsetX != null || offsetY != null || stretchedRadius != 10000f || stretchedTotal != 10500f || (total - 10f) / stretch != 10490f;
    var waterDepth = menu ? 1f : WorldInfo.WaterDepth;
    var altitudeDelta = menu ? 0f : WorldInfo.BaseAltitudeDelta;
    var altitudeMultiplier = menu ? 1f : WorldInfo.AltitudeMultiplier;
    var forest = menu ? 1f : WorldInfo.ForestMultiplier;

    Apply(!menu && !bc && stretchedTotal != 10500f, typeof(WorldGenerator), nameof(WorldGenerator.GetBiomeHeight), typeof(Stretch), nameof(Stretch.TranspilerGetBiomeHeight), Transpiler, (total, stretch));
    Apply(waterDepth != 1f, typeof(WorldGenerator), nameof(WorldGenerator.GetBiomeHeight), typeof(BiomeHeight), nameof(BiomeHeight.Postfix), Postfix);
    Apply(!menu && !bc && baseHeightChanged, typeof(WorldGenerator), nameof(WorldGenerator.GetBaseHeight), typeof(GetBaseHeight), nameof(GetBaseHeight.Transpiler), Transpiler, (offsetX, offsetY, radius, total, stretch));
    Apply(altitudeDelta != 0f || altitudeMultiplier != 1f, typeof(WorldGenerator), nameof(WorldGenerator.GetBaseHeight), typeof(BaseHeight), nameof(BaseHeight.Postfix), Postfix);
    Apply(forest != 1f, typeof(WorldGenerator), nameof(WorldGenerator.GetForestFactor), typeof(Forest), nameof(Forest.Postfix), Postfix);

    Apply(seeded, typeof(WorldGenerator), nameof(WorldGenerator.GetAshlandsHeight), typeof(HeightSeed), nameof(HeightSeed.Ashlands), Transpiler, heightSeed);
    Apply(seeded, typeof(WorldGenerator), nameof(WorldGenerator.GetForestHeight), typeof(HeightSeed), nameof(HeightSeed.Forest), Transpiler, heightSeed);
    Apply(seeded, typeof(WorldGenerator), nameof(WorldGenerator.GetMeadowsHeight), typeof(HeightSeed), nameof(HeightSeed.Meadows), Transpiler, heightSeed);
    Apply(seeded, typeof(WorldGenerator), nameof(WorldGenerator.GetDeepNorthHeight), typeof(HeightSeed), nameof(HeightSeed.DeepNorth), Transpiler, heightSeed);
    Apply(seeded, typeof(WorldGenerator), nameof(WorldGenerator.GetPlainsHeight), typeof(HeightSeed), nameof(HeightSeed.Plains), Transpiler, heightSeed);
    Apply(seeded, typeof(WorldGenerator), nameof(WorldGenerator.GetSnowMountainHeight), typeof(HeightSeed), nameof(HeightSeed.Mountain), Transpiler, heightSeed);
    Apply(seeded, typeof(WorldGenerator), nameof(WorldGenerator.GetMistlandsHeight), typeof(HeightSeed), nameof(HeightSeed.Mistlands), Transpiler, heightSeed);

    Apply(!menu && !bc && (total + 150f) / stretch != 10150f, typeof(WorldGenerator), nameof(WorldGenerator.GetAshlandsHeight), typeof(GetAshlandsHeightSize), nameof(GetAshlandsHeightSize.Transpiler), Transpiler, (total, stretch));
  }

  private static void PatchPoles(bool menu, float stretchedRadius)
  {
    var width = Configuration.AshlandsWidthRestriction;
    var length = Configuration.AshlandsLengthRestriction;
    var radiusChanged = !menu && stretchedRadius != 10000f;
    Apply(width != GetAshlandsHeight.DefaultWidthRestriction || length != GetAshlandsHeight.DefaultLengthRestriction, typeof(WorldGenerator), nameof(WorldGenerator.GetAshlandsHeight), typeof(GetAshlandsHeight), nameof(GetAshlandsHeight.Transpiler), Transpiler, (width, length));
    Apply(radiusChanged, typeof(WorldGenerator), nameof(WorldGenerator.IsDeepnorth), typeof(Stretch), nameof(Stretch.TranspilerIsDeepnorth), Transpiler, stretchedRadius);
    Apply(radiusChanged, typeof(WorldGenerator), nameof(WorldGenerator.CreateAshlandsGap), typeof(Stretch), nameof(Stretch.TranspilerCreateAshlandsGap), Transpiler, stretchedRadius);
    Apply(radiusChanged, typeof(WorldGenerator), nameof(WorldGenerator.CreateDeepNorthGap), typeof(Stretch), nameof(Stretch.TranspilerCreateDeepNorthGap), Transpiler, stretchedRadius);
    Apply(!Configuration.AshlandsGap, typeof(WorldGenerator), nameof(WorldGenerator.CreateAshlandsGap), typeof(Poles), nameof(Poles.DisableGap), Prefix);
    Apply(!Configuration.DeepNorthGap, typeof(WorldGenerator), nameof(WorldGenerator.CreateDeepNorthGap), typeof(Poles), nameof(Poles.DisableGap), Prefix);
  }

  private static void PatchWorldSize(bool bc, float radius, float total, float stretch, int biomeMapSize, bool biomeMapChanged, bool totalChanged)
  {
    var map = (biomeMapSize, stretch);
    Apply(biomeMapChanged, typeof(AltBiomeWorldData), nameof(AltBiomeWorldData.MapSpaceToWorldSpace), typeof(MapSpaceToWorldSpaceSize), nameof(MapSpaceToWorldSpaceSize.Transpiler), Transpiler, map, [typeof(float)]);
    Apply(biomeMapChanged, typeof(AltBiomeWorldData), nameof(AltBiomeWorldData.WorldSpaceToMapSpace), typeof(WorldSpaceToMapSpaceSize), nameof(WorldSpaceToMapSpaceSize.Transpiler), Transpiler, map, [typeof(float)]);
    Apply(biomeMapChanged, typeof(AltBiomeWorldData), nameof(AltBiomeWorldData.GenerateBiomePoints), typeof(GenerateBiomePointsSize), nameof(GenerateBiomePointsSize.Transpiler), Transpiler, (biomeMapSize, total));
    Apply(!bc && biomeMapSize != 2048, typeof(WorldGenerator), nameof(WorldGenerator.GetBiomeSector), typeof(GetBiomeSectorSize), nameof(GetBiomeSectorSize.Transpiler), Transpiler, biomeMapSize, GetBiomeSectorArgs);
    var edge = !bc && totalChanged;
    Apply(edge, typeof(Ship), nameof(Ship.ApplyEdgeForce), typeof(WorldSizeHelper), nameof(WorldSizeHelper.EdgeCheck), Transpiler, total);
    Apply(edge, typeof(Player), nameof(Player.EdgeOfWorldKill), typeof(WorldSizeHelper), nameof(WorldSizeHelper.EdgeCheck), Transpiler, total);
    Apply(edge, typeof(WaterVolume), nameof(WaterVolume.GetWaterSurface), typeof(GetWaterSurface), nameof(GetWaterSurface.Transpiler), Transpiler, total);
    // Also removes the edge width subtraction, so it's needed with the vanilla size too.
    Apply(!bc, typeof(EnvMan), nameof(EnvMan.UpdateWind), typeof(UpdateWind), nameof(UpdateWind.Transpiler), Transpiler, (radius, total));
  }

  private static void PatchRivers(bool menu, float radius, float stretch, bool radiusChanged)
  {
    Apply(!menu && (radiusChanged || stretch != 1f), typeof(WorldGenerator), nameof(WorldGenerator.FindLakes), typeof(FindLakes), nameof(FindLakes.Transpiler), Transpiler, (radius, stretch));
    Apply(!menu && stretch != 1f, typeof(WorldGenerator), nameof(WorldGenerator.IsRiverAllowed), typeof(IsRiverAllowed), nameof(IsRiverAllowed.Transpiler), Transpiler, stretch);
    Apply(!menu && radiusChanged, typeof(WorldGenerator), nameof(WorldGenerator.FindStreamStartPoint), typeof(FindStreamStartPoint), nameof(FindStreamStartPoint.Transpiler), Transpiler, radius);
  }

  private static void PatchLocations(float radius, bool radiusChanged)
  {
    Apply(radiusChanged, typeof(ZoneSystem), nameof(ZoneSystem.GetRandomZone), typeof(GetRandomZone), nameof(GetRandomZone.Transpiler), Transpiler, radius);
    Apply(radiusChanged, typeof(ZoneSystem), nameof(ZoneSystem.GenerateLocationsTimeSliced), typeof(GenerateLocations), nameof(GenerateLocations.TranspileMoveNext), Transpiler, radius, GenerateLocationsArgs, true);
  }
}
