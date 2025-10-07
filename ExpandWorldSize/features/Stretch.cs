using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace ExpandWorldSize;


[HarmonyPatch]
public class Stretch
{
  public static IEnumerable<CodeInstruction> StretchIsAshlandsTranspiler(IEnumerable<CodeInstruction> instructions)
  {
    return new CodeMatcher(instructions)
      .MatchForward(false, new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(WorldGenerator), nameof(WorldGenerator.IsAshlands))))
      .MatchBack(false, new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(Vector3), nameof(Vector3.z))))
      .Advance(1)
      .InsertAndAdvance(new CodeInstruction(OpCodes.Ldc_R4, Configuration.WorldStretch))
      .InsertAndAdvance(new CodeInstruction(OpCodes.Div))
      .MatchBack(false, new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(Vector3), nameof(Vector3.x))))
      .Advance(1)
      .InsertAndAdvance(new CodeInstruction(OpCodes.Ldc_R4, Configuration.WorldStretch))
      .InsertAndAdvance(new CodeInstruction(OpCodes.Div))
      .InstructionEnumeration();
  }
  public static IEnumerable<CodeInstruction> StretchIsAshlandsDeepNorthTranspiler(IEnumerable<CodeInstruction> instructions)
  {
    return new CodeMatcher(instructions)
      .MatchForward(false, new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(WorldGenerator), nameof(WorldGenerator.IsAshlands))))
      .MatchBack(false, new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(Vector3), nameof(Vector3.z))))
      .Advance(1)
      .InsertAndAdvance(new CodeInstruction(OpCodes.Ldc_R4, Configuration.WorldStretch))
      .InsertAndAdvance(new CodeInstruction(OpCodes.Div))
      .MatchBack(false, new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(Vector3), nameof(Vector3.x))))
      .Advance(1)
      .InsertAndAdvance(new CodeInstruction(OpCodes.Ldc_R4, Configuration.WorldStretch))
      .InsertAndAdvance(new CodeInstruction(OpCodes.Div))
      .MatchForward(false, new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(WorldGenerator), nameof(WorldGenerator.IsDeepnorth))))
      .MatchBack(false, new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(Vector3), nameof(Vector3.z))))
      .Advance(1)
      .InsertAndAdvance(new CodeInstruction(OpCodes.Ldc_R4, Configuration.WorldStretch))
      .InsertAndAdvance(new CodeInstruction(OpCodes.Div))
      .MatchBack(false, new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(Vector3), nameof(Vector3.x))))
      .Advance(1)
      .InsertAndAdvance(new CodeInstruction(OpCodes.Ldc_R4, Configuration.WorldStretch))
      .InsertAndAdvance(new CodeInstruction(OpCodes.Div))
      .InstructionEnumeration();
  }
  [HarmonyPriority(Priority.HigherThanNormal)]
  public static void GetAshlandsOceanGradient(ref Vector3 pos)
  {
    pos = pos with { x = pos.x / Configuration.WorldStretch, z = pos.z / Configuration.WorldStretch };
  }
  [HarmonyPriority(Priority.HigherThanNormal)]
  public static void GetMaskColor(ref float wx, ref float wy)
  {
    wx /= Configuration.WorldStretch;
    wy /= Configuration.WorldStretch;
  }
  [HarmonyPriority(Priority.HigherThanNormal)]
  public static void GetBiomeHeight(ref float wx, ref float wy)
  {
    wx /= Configuration.WorldStretch;
    wy /= Configuration.WorldStretch;
  }
  [HarmonyPriority(Priority.HigherThanNormal)]
  public static void GetBiome(ref float wx, ref float wy)
  {
    // Stretch should "slow down" both GetBaseHeight and PerlinNoise.
    // So this is the easiest way to do it.
    // Alternatively could transpile each usage.
    wx /= Configuration.WorldStretch;
    wy /= Configuration.WorldStretch;
  }

  public static CodeMatcher Replace(CodeMatcher matcher, OpCode code)
  {
    if (Configuration.WorldStretch == 1f) return matcher;
    return matcher
      .MatchForward(false, new CodeMatch(code))
      .Advance(1)
      .InsertAndAdvance(new CodeInstruction(OpCodes.Ldc_R4, Configuration.WorldStretch))
      .InsertAndAdvance(new CodeInstruction(OpCodes.Div));
  }
  public static CodeMatcher ReplaceBiome(CodeMatcher matcher)
  {
    if (Configuration.BiomeStretch == 1f) return matcher;
    return matcher
      .MatchForward(false, new(OpCodes.Ldarg_1), new(OpCodes.Conv_R8), new(OpCodes.Add))
      .Advance(1)
      .InsertAndAdvance(new CodeInstruction(OpCodes.Ldc_R4, Configuration.BiomeStretch))
      .InsertAndAdvance(new CodeInstruction(OpCodes.Div))
      .MatchForward(false, new(OpCodes.Ldarg_2), new(OpCodes.Conv_R8), new(OpCodes.Add))
      .Advance(1)
      .InsertAndAdvance(new CodeInstruction(OpCodes.Ldc_R4, Configuration.BiomeStretch))
      .InsertAndAdvance(new CodeInstruction(OpCodes.Div));
  }

  [HarmonyPatch(typeof(WorldGenerator), nameof(WorldGenerator.GetBiome), typeof(float), typeof(float), typeof(float), typeof(bool))]

  static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
  {
    if (Patcher.IsMenu) return instructions;
    CodeMatcher matcher = new(instructions);
    matcher = ReplaceBiome(matcher);
    matcher = ReplaceBiome(matcher);
    matcher = ReplaceBiome(matcher);
    matcher = ReplaceBiome(matcher);
    matcher = new(matcher.InstructionEnumeration());

    var field = AccessTools.Field(typeof(WorldGenerator), nameof(WorldGenerator.ashlandsMinDistance));
    field.SetValue(null, 1.2f * Configuration.StrechedWorldRadius);
    field = AccessTools.Field(typeof(WorldGenerator), nameof(WorldGenerator.ashlandsYOffset));
    field.SetValue(null, -0.4f * Configuration.StrechedWorldRadius);

    matcher = Helper.Replace(matcher, 2000f, 0.2f * Configuration.StrechedWorldRadius);
    matcher = Helper.Replace(matcher, 6000d, 0.6 * Configuration.StrechedWorldRadius);
    matcher = Helper.Replace(matcher, 10000f, Configuration.StrechedWorldRadius);
    matcher = Helper.Replace(matcher, 3000d, 0.3 * Configuration.StrechedWorldRadius);
    matcher = Helper.Replace(matcher, 8000f, 0.8f * Configuration.StrechedWorldRadius);
    matcher = Helper.Replace(matcher, 600d, 0.06 * Configuration.StrechedWorldRadius);
    matcher = Helper.Replace(matcher, 6000f, 0.6f * Configuration.StrechedWorldRadius);
    matcher = Helper.Replace(matcher, 5000d, 0.5 * Configuration.StrechedWorldRadius);
    return matcher.InstructionEnumeration();
  }


  [HarmonyPatch(typeof(WorldGenerator), nameof(WorldGenerator.IsDeepnorth)), HarmonyTranspiler]

  static IEnumerable<CodeInstruction> TranspilerIsDeepnorth(IEnumerable<CodeInstruction> instructions)
  {
    if (Patcher.IsMenu) return instructions;
    CodeMatcher matcher = new(instructions);
    matcher = Helper.Replace(matcher, 4000d, 0.4 * Configuration.StrechedWorldRadius);
    matcher = Helper.Replace(matcher, 12000d, 1.2 * Configuration.StrechedWorldRadius);
    return matcher.InstructionEnumeration();
  }

  [HarmonyPatch(typeof(WorldGenerator), nameof(WorldGenerator.CreateAshlandsGap)), HarmonyTranspiler]
  static IEnumerable<CodeInstruction> TranspilerCreateAshlandsGap(IEnumerable<CodeInstruction> instructions)
  {
    if (Patcher.IsMenu) return instructions;
    CodeMatcher matcher = new(instructions);
    matcher = Helper.Replace(matcher, 400d, 0.04 * Configuration.StrechedWorldRadius);
    return matcher.InstructionEnumeration();
  }

  [HarmonyPatch(typeof(WorldGenerator), nameof(WorldGenerator.CreateDeepNorthGap)), HarmonyTranspiler]
  static IEnumerable<CodeInstruction> TranspilerCreateDeepNorthGap(IEnumerable<CodeInstruction> instructions)
  {
    if (Patcher.IsMenu) return instructions;
    CodeMatcher matcher = new(instructions);
    matcher = Helper.Replace(matcher, 4000f, 0.4f * Configuration.StrechedWorldRadius);
    matcher = Helper.Replace(matcher, 12000d, 1.2f * Configuration.StrechedWorldRadius);
    matcher = Helper.Replace(matcher, 400d, 0.04 * Configuration.StrechedWorldRadius);
    return matcher.InstructionEnumeration();
  }
  [HarmonyPatch(typeof(WorldGenerator), nameof(WorldGenerator.GetBiomeHeight)), HarmonyTranspiler]
  static IEnumerable<CodeInstruction> TranspilerGetBiomeHeight(IEnumerable<CodeInstruction> instructions)
  {
    if (Patcher.IsMenu) return instructions;
    CodeMatcher matcher = new(instructions);
    matcher = Helper.Replace(matcher, 10500f, Configuration.StrechedWorldTotalRadius);
    return matcher.InstructionEnumeration();
  }
}
