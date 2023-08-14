using System;
using System.Reflection.Emit;
using HarmonyLib;

namespace ExpandWorldSize;

public static class Helper
{
  public static CodeMatcher Replace(CodeMatcher instructions, double value, double newValue)
  {
    return instructions
      .MatchForward(false, new CodeMatch(OpCodes.Ldc_R8, value))
      .SetOperandAndAdvance(newValue);
  }
  public static CodeMatcher Replace(CodeMatcher instructions, float value, float newValue)
  {
    return instructions
      .MatchForward(false, new CodeMatch(OpCodes.Ldc_R4, value))
      .SetOperandAndAdvance(newValue);
  }

  public static CodeMatcher ReplaceSeed(CodeMatcher instructions, string name, int value)
  {
    return instructions
      .MatchForward(false, new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(WorldGenerator), name)))
      .MatchBack(false, new CodeMatch(OpCodes.Ldarg_0))
      .SetAndAdvance(OpCodes.Ldc_I4, value)
      .SetOpcodeAndAdvance(OpCodes.Nop);
  }
  public static CodeMatcher ReplaceSeed(CodeMatcher instructions, string name, float value)
  {
    return instructions
      .MatchForward(false, new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(WorldGenerator), name)))
      .MatchBack(false, new CodeMatch(OpCodes.Ldarg_0))
      .SetAndAdvance(OpCodes.Ldc_R4, value)
      .SetOpcodeAndAdvance(OpCodes.Nop);
  }

  public static float HeightToBaseHeight(float altitude) => altitude / 200f;
  public static bool IsServer() => ZNet.instance && ZNet.instance.IsServer();
  // Note: Intended that is client when no Znet instance (so stuff isn't loaded in the main menu).
  public static bool IsClient() => !IsServer();
}

