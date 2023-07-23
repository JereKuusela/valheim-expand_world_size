using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
namespace ExpandWorldSize;

[HarmonyPatch(typeof(WorldGenerator), nameof(WorldGenerator.FindLakes))]
public class FindLakes
{
  static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
  {
    var matcher = new CodeMatcher(instructions);
    matcher = Helper.Replace(matcher, -10000f, () => -Configuration.WorldRadius);
    matcher = Helper.Replace(matcher, -10000f, () => -Configuration.WorldRadius);
    matcher = Helper.Replace(matcher, 10000f, () => Configuration.WorldRadius);
    matcher = Helper.ReplaceStretch(matcher, OpCodes.Ldloc_3);
    matcher = Helper.ReplaceStretch(matcher, OpCodes.Ldloc_2);
    return matcher.InstructionEnumeration();
  }
}

[HarmonyPatch(typeof(WorldGenerator), nameof(WorldGenerator.IsRiverAllowed))]
public class IsRiverAllowed
{
  static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
  {
    var matcher = new CodeMatcher(instructions);
    matcher = Helper.ReplaceStretch(matcher, OpCodes.Ldfld);
    matcher = Helper.ReplaceStretch(matcher, OpCodes.Ldfld);
    return matcher.InstructionEnumeration();
  }
}

[HarmonyPatch(typeof(WorldGenerator), nameof(WorldGenerator.FindStreamStartPoint))]
public class FindStreamStartPoint
{
  static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
  {
    var matcher = new CodeMatcher(instructions);
    matcher = Helper.Replace(matcher, -10000f, () => -Configuration.WorldRadius);
    matcher = Helper.Replace(matcher, 10000f, () => Configuration.WorldRadius);
    matcher = Helper.Replace(matcher, -10000f, () => -Configuration.WorldRadius);
    matcher = Helper.Replace(matcher, 10000f, () => Configuration.WorldRadius);
    return matcher.InstructionEnumeration();
  }
}
[HarmonyPatch(typeof(WorldGenerator), nameof(WorldGenerator.AddRivers))]
public class AddRivers
{
  // Rivers are placed at unstretched positions.
  static void Prefix(ref float wx, ref float wy)
  {
    wx *= Configuration.WorldStretch;
    wy *= Configuration.WorldStretch;
  }
}