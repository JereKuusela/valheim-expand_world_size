using System;
using System.Reflection.Emit;
using HarmonyLib;

namespace ExpandWorldSize;

public static class Helper
{
  public static CodeMatcher Replace(CodeMatcher instructions, float value, Func<float> call)
  {
    return instructions
      .MatchForward(false, new CodeMatch(OpCodes.Ldc_R4, value))
      .SetAndAdvance(OpCodes.Call, Transpilers.EmitDelegate(call).operand);
  }
  public static CodeMatcher ReplaceBiomeStretch(CodeMatcher instructions)
  {
    return instructions
      .MatchForward(false, new(OpCodes.Ldarg_1), new(OpCodes.Add))
      .Advance(1)
      .InsertAndAdvance(new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(Configuration), nameof(Configuration.BiomeStretch))))
      .InsertAndAdvance(new CodeInstruction(OpCodes.Div))
      .MatchForward(false, new(OpCodes.Ldarg_2), new(OpCodes.Add))
      .Advance(1)
      .InsertAndAdvance(new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(Configuration), nameof(Configuration.BiomeStretch))))
      .InsertAndAdvance(new CodeInstruction(OpCodes.Div));
  }
  public static CodeMatcher Replace(CodeMatcher instructions, sbyte value, Func<int> call)
  {
    return instructions
      .MatchForward(false, new CodeMatch(OpCodes.Ldc_I4_S, value))
      .SetAndAdvance(OpCodes.Call, Transpilers.EmitDelegate(call).operand);
  }
  public static CodeMatcher Replace(CodeMatcher instructions, int value, Func<int> call)
  {
    return instructions
      .MatchForward(false, new CodeMatch(OpCodes.Ldc_I4, value))
      .SetAndAdvance(OpCodes.Call, Transpilers.EmitDelegate(call).operand);
  }

  public static CodeMatcher ReplaceSeed(CodeMatcher instructions, string name, Func<WorldGenerator, int> call)
  {
    return instructions
      .MatchForward(false, new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(WorldGenerator), name)))
      .SetAndAdvance(OpCodes.Call, Transpilers.EmitDelegate(call).operand);
  }
  public static CodeMatcher ReplaceSeed(CodeMatcher instructions, string name, Func<WorldGenerator, float> call)
  {
    return instructions
      .MatchForward(false, new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(WorldGenerator), name)))
      .SetAndAdvance(OpCodes.Call, Transpilers.EmitDelegate(call).operand);
  }
  public static CodeMatcher ReplaceStretch(CodeMatcher instructions, OpCode code)
  {
    return instructions
      .MatchForward(false, new CodeMatch(code))
      .Advance(1)
      .InsertAndAdvance(new CodeInstruction(OpCodes.Ldsfld, AccessTools.Field(typeof(Configuration), nameof(Configuration.WorldStretch))))
      .InsertAndAdvance(new CodeInstruction(OpCodes.Div));
  }


  public static float HeightToBaseHeight(float altitude) => altitude / 200f;
  public static bool IsServer() => ZNet.instance && ZNet.instance.IsServer();
  // Note: Intended that is client when no Znet instance (so stuff isn't loaded in the main menu).
  public static bool IsClient() => !IsServer();
}

