using System.Collections.Generic;
using HarmonyLib;
namespace ExpandWorldSize;

[HarmonyPatch(typeof(WorldGenerator), nameof(WorldGenerator.GetBiome), new[] { typeof(float), typeof(float) })]
public class GetBiomeWG
{
  static void Prefix(WorldGenerator __instance, ref float wx, ref float wy)
  {
    if (__instance.m_world.m_menu) return;
    wx /= Configuration.WorldStretch;
    wy /= Configuration.WorldStretch;
  }
  static float GetBiomeStretch() => Configuration.BiomeStretch;
  static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
  {
    var matcher = new CodeMatcher(instructions);
    matcher = Helper.ReplaceBiomeStretch(matcher);
    matcher = Helper.ReplaceBiomeStretch(matcher);
    matcher = Helper.ReplaceBiomeStretch(matcher);
    matcher = Helper.ReplaceBiomeStretch(matcher);
    return matcher.InstructionEnumeration();
  }
}
