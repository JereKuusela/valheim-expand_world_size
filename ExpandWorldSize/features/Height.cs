using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace ExpandWorldSize;

[HarmonyPatch]
public class HeightSeed
{
  static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions)
  {
    var matcher = new CodeMatcher(instructions);
    matcher = Helper.ReplaceSeed(matcher, nameof(WorldGenerator.m_offset3), (WorldGenerator instance) => Configuration.HeightSeed ?? instance.m_offset3);
    return matcher.InstructionEnumeration();
  }
  [HarmonyPatch(typeof(WorldGenerator), nameof(WorldGenerator.GetAshlandsHeight)), HarmonyTranspiler]
  static IEnumerable<CodeInstruction> Ashlands(IEnumerable<CodeInstruction> instructions) => Transpile(instructions);
  [HarmonyPatch(typeof(WorldGenerator), nameof(WorldGenerator.GetForestHeight)), HarmonyTranspiler]
  static IEnumerable<CodeInstruction> Forest(IEnumerable<CodeInstruction> instructions) => Transpile(instructions);
  [HarmonyPatch(typeof(WorldGenerator), nameof(WorldGenerator.GetMeadowsHeight)), HarmonyTranspiler]
  static IEnumerable<CodeInstruction> Meadows(IEnumerable<CodeInstruction> instructions) => Transpile(instructions);
  [HarmonyPatch(typeof(WorldGenerator), nameof(WorldGenerator.GetDeepNorthHeight)), HarmonyTranspiler]
  static IEnumerable<CodeInstruction> DeepNorth(IEnumerable<CodeInstruction> instructions) => Transpile(instructions);
  [HarmonyPatch(typeof(WorldGenerator), nameof(WorldGenerator.GetPlainsHeight)), HarmonyTranspiler]
  static IEnumerable<CodeInstruction> Plains(IEnumerable<CodeInstruction> instructions) => Transpile(instructions);
  [HarmonyPatch(typeof(WorldGenerator), nameof(WorldGenerator.GetSnowMountainHeight)), HarmonyTranspiler]
  static IEnumerable<CodeInstruction> Mountain(IEnumerable<CodeInstruction> instructions) => Transpile(instructions);
}

[HarmonyPatch(typeof(WorldGenerator), nameof(WorldGenerator.GetBaseHeight))]
public class BaseHeight
{
  static void Postfix(WorldGenerator __instance, ref float __result)
  {
    if (__instance.m_world.m_menu) return;
    var waterLevel = Helper.HeightToBaseHeight(World.WaterLevel);
    __result = waterLevel + (__result - waterLevel) * Configuration.AltitudeMultiplier;
    __result += Helper.HeightToBaseHeight(Configuration.AltitudeDelta);
  }
}
[HarmonyPatch(typeof(WorldGenerator), nameof(WorldGenerator.GetBiomeHeight))]
public class BiomeHeight
{
  static void Prefix(WorldGenerator __instance, ref float wx, ref float wy)
  {
    if (__instance.m_world.m_menu) return;
    wx /= Configuration.WorldStretch;
    wy /= Configuration.WorldStretch;
  }
  static void Postfix(WorldGenerator __instance, ref float __result)
  {
    if (__instance.m_world.m_menu) return;
    __result -= World.WaterLevel;
    if (__result < 0f)
      __result *= Configuration.WaterDepthMultiplier;
    __result += World.WaterLevel;
  }
}
