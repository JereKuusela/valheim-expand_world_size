using HarmonyLib;

namespace ExpandWorldSize;

[HarmonyPatch]
public static class Patcher
{
  public static WorldGenerator? WG;
  public static bool IsMenu => WG == null || WG.m_world.m_menu;
  private static Harmony? Harmony;
  public static void Init(Harmony harmony)
  {
    Harmony = harmony;
    Patch();
  }
  public static void Patch()
  {
    if (Harmony == null) throw new("Harmony not initialized.");
    Harmony.UnpatchSelf();
    Harmony.PatchAll();
    CheckWorldStretch(Harmony);
    CheckWaterDepth(Harmony);
    CheckForest(Harmony);
    CheckAltitude(Harmony);
  }

  [HarmonyPatch(typeof(WorldGenerator), nameof(WorldGenerator.VersionSetup)), HarmonyPostfix, HarmonyPriority(Priority.Last)]
  static void PatchOnLoad(WorldGenerator __instance)
  {
    WG = __instance;
    WorldInfo.Refresh();
  }


  private static void CheckWorldStretch(Harmony harmony)
  {
    if (!IsMenu && Configuration.WorldStretch != 1f) return;
    var method = AccessTools.Method(typeof(WorldGenerator), nameof(WorldGenerator.GetBiomeHeight));
    var patch = AccessTools.Method(typeof(Stretch), nameof(Stretch.GetBiomeHeight));
    harmony.Unpatch(method, patch);
    method = AccessTools.Method(typeof(WorldGenerator), nameof(WorldGenerator.GetBiome), [typeof(float), typeof(float), typeof(float), typeof(bool)]);
    patch = AccessTools.Method(typeof(Stretch), nameof(Stretch.GetBiome));
    harmony.Unpatch(method, patch);
    method = AccessTools.Method(typeof(WorldGenerator), nameof(WorldGenerator.AddRivers));
    patch = AccessTools.Method(typeof(AddRivers), nameof(AddRivers.Prefix));
    harmony.Unpatch(method, patch);
  }
  private static void CheckWaterDepth(Harmony harmony)
  {
    if (!IsMenu && Configuration.WaterDepthMultiplier != 1f) return;
    var method = AccessTools.Method(typeof(WorldGenerator), nameof(WorldGenerator.GetBiomeHeight));
    var patch = AccessTools.Method(typeof(BiomeHeight), nameof(BiomeHeight.Postfix));
    harmony.Unpatch(method, patch);
  }
  private static void CheckForest(Harmony harmony)
  {
    if (!IsMenu && Configuration.ForestMultiplier != 1f && Configuration.ForestMultiplier != 0f) return;
    var method = AccessTools.Method(typeof(WorldGenerator), nameof(WorldGenerator.GetForestFactor));
    var patch = AccessTools.Method(typeof(Forest), nameof(Forest.Postfix));
    harmony.Unpatch(method, patch);
  }
  private static void CheckAltitude(Harmony harmony)
  {
    if (!IsMenu && (Configuration.AltitudeDelta != 0f || Configuration.AltitudeMultiplier != 1f)) return;
    var method = AccessTools.Method(typeof(WorldGenerator), nameof(WorldGenerator.GetBaseHeight));
    var patch = AccessTools.Method(typeof(BaseHeight), nameof(BaseHeight.Postfix));
    harmony.Unpatch(method, patch);
  }
}
