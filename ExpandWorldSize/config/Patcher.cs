using HarmonyLib;

namespace ExpandWorldSize;

[HarmonyPatch]
public static class Patcher
{
  private static Harmony? Harmony;
  public static void Init(Harmony harmony)
  {
    Harmony = harmony;
    Patch(WorldGenerator.instance);
  }
  public static void Patch(WorldGenerator? wg)
  {
    if (Harmony == null) throw new("Harmony not initialized.");
    Harmony.UnpatchSelf();
    Harmony.PatchAll();
    CheckWorldStretch(Harmony, wg);
    CheckWaterDepth(Harmony, wg);
    CheckForest(Harmony, wg);
    CheckAltitude(Harmony, wg);
  }

  // Server sync reads config after ZNet.Awake. So this is the first possible moment for the patches.
  [HarmonyPatch(typeof(ZNet), nameof(ZNet.Awake)), HarmonyPostfix, HarmonyPriority(Priority.Last)]
  static void GamePatch(ZNet __instance)
  {
    if (__instance.IsServer())
      WorldInfo.Refresh(WorldGenerator.instance);
  }
  // Main menu doesn't have ZNet, so it needs different point to clean up patches.
  [HarmonyPatch(typeof(WorldGenerator), nameof(WorldGenerator.VersionSetup)), HarmonyPostfix, HarmonyPriority(Priority.Last)]
  static void MainMenuPatch(WorldGenerator __instance)
  {
    if (__instance.m_world.m_menu)
      WorldInfo.Refresh(__instance);
  }


  private static void CheckWorldStretch(Harmony harmony, WorldGenerator? wg)
  {
    if (wg != null && !wg.m_world.m_menu && Configuration.WorldStretch != 1f) return;
    var method = AccessTools.Method(typeof(WorldGenerator), nameof(WorldGenerator.GetBiomeHeight));
    var patch = AccessTools.Method(typeof(Stretch), nameof(Stretch.GetBiomeHeight));
    harmony.Unpatch(method, patch);
    method = AccessTools.Method(typeof(WorldGenerator), nameof(WorldGenerator.GetBiome), new[] { typeof(float), typeof(float) });
    patch = AccessTools.Method(typeof(Stretch), nameof(Stretch.GetBiome));
    harmony.Unpatch(method, patch);
    method = AccessTools.Method(typeof(WorldGenerator), nameof(WorldGenerator.AddRivers));
    patch = AccessTools.Method(typeof(AddRivers), nameof(AddRivers.Prefix));
    harmony.Unpatch(method, patch);
  }
  private static void CheckWaterDepth(Harmony harmony, WorldGenerator? wg)
  {
    if (wg != null && !wg.m_world.m_menu && Configuration.WaterDepthMultiplier != 1f) return;
    var method = AccessTools.Method(typeof(WorldGenerator), nameof(WorldGenerator.GetBiomeHeight));
    var patch = AccessTools.Method(typeof(BiomeHeight), nameof(BiomeHeight.Postfix));
    harmony.Unpatch(method, patch);
  }
  private static void CheckForest(Harmony harmony, WorldGenerator? wg)
  {
    if (wg != null && !wg.m_world.m_menu && Configuration.ForestMultiplier != 1f && Configuration.ForestMultiplier != 0f) return;
    var method = AccessTools.Method(typeof(WorldGenerator), nameof(WorldGenerator.GetForestFactor));
    var patch = AccessTools.Method(typeof(Forest), nameof(Forest.Postfix));
    harmony.Unpatch(method, patch);
  }
  private static void CheckAltitude(Harmony harmony, WorldGenerator? wg)
  {
    if (wg != null && !wg.m_world.m_menu && (Configuration.AltitudeDelta != 0f || Configuration.AltitudeMultiplier != 1f)) return;
    var method = AccessTools.Method(typeof(WorldGenerator), nameof(WorldGenerator.GetBaseHeight));
    var patch = AccessTools.Method(typeof(BaseHeight), nameof(BaseHeight.Postfix));
    harmony.Unpatch(method, patch);
  }
}
