using System.Collections.Generic;
using HarmonyLib;
namespace ExpandWorldSize;

[HarmonyPatch(typeof(WorldGenerator), nameof(WorldGenerator.GetBiome), new[] { typeof(float), typeof(float) })]
public class GetBiomeWG
{
  [HarmonyPriority(Priority.HigherThanNormal)]
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
    matcher = new CodeMatcher(matcher.InstructionEnumeration());
    matcher = Helper.Replace(matcher, -4000f, () => -GetBaseHeight.Radius4000);
    matcher = Helper.Replace(matcher, 12000f, () => GetBaseHeight.Radius12000);
    matcher = Helper.Replace(matcher, 4000f, () => GetBaseHeight.Radius4000);
    matcher = Helper.Replace(matcher, 12000f, () => GetBaseHeight.Radius12000);
    matcher = Helper.Replace(matcher, 2000f, () => GetBaseHeight.Radius2000);
    matcher = Helper.Replace(matcher, 6000f, () => GetBaseHeight.Radius6000);
    matcher = Helper.Replace(matcher, 10000f, () => GetBaseHeight.Radius10000);
    matcher = Helper.Replace(matcher, 3000f, () => GetBaseHeight.Radius3000);
    matcher = Helper.Replace(matcher, 8000f, () => GetBaseHeight.Radius8000);
    matcher = Helper.Replace(matcher, 600f, () => GetBaseHeight.Radius600);
    matcher = Helper.Replace(matcher, 6000f, () => GetBaseHeight.Radius6000);
    matcher = Helper.Replace(matcher, 5000f, () => GetBaseHeight.Radius5000);


    return matcher.InstructionEnumeration();
  }
}
