using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;

namespace ExpandWorldSize;

public class WorldInfo
{
  // Everything used in prefix or postfix should have cached value here.
  // Transpilers use the direct value so doesn't need to be cached.
  public static float WaterLevel = 30f;
  public static float BaseWaterLevel = 0f;
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
    Refresh(WorldGenerator.instance);
  }
  public static void Generate()
  {
    if (WorldGenerator.instance == null) return;
    EWS.Log.LogInfo("Regenerating the world.");
    Refresh(WorldGenerator.instance);
    MapGeneration.Cancel();
    WorldGenerator.instance.Pregenerate();
    foreach (var heightmap in Object.FindObjectsOfType<Heightmap>())
    {
      heightmap.m_buildData = null;
      heightmap.Regenerate();
    }
    ClutterSystem.instance?.ClearAll();
    SetupMaterial.Refresh();
    WaterLayerFix.Refresh(EnvMan.instance);
    if (SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null)
      Minimap.instance?.GenerateWorldMap();
  }
  public static void Refresh(WorldGenerator wg)
  {
    if (wg == null) return;
    BaseWaterLevel = Helper.HeightToBaseHeight(WaterLevel);
    WorldStretch = Configuration.WorldStretch;
    BiomeStretch = Configuration.BiomeStretch;
    WaterDepth = Configuration.WaterDepthMultiplier;
    AltitudeMultiplier = Configuration.AltitudeMultiplier;
    ForestMultiplier = Configuration.ForestMultiplier;
    BaseAltitudeDelta = Helper.HeightToBaseHeight(Configuration.AltitudeDelta);
    wg.maxMarshDistance = VersionSetup.MaxMarshDistance * Configuration.WorldRadius / 10000f / Configuration.WorldStretch;
    EWD.RefreshSize();
    Patcher.Patch(wg);
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
    if (Configuration.Seed != "" && !result.m_menu)
    {
      result.m_seedName = Configuration.Seed;
      result.m_seed = Configuration.Seed.GetStableHashCode();
    }
    return result;
  }
}