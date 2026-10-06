using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Service;

namespace ExpandWorldSize;

public static class Helper
{
  public static CodeMatcher Replace(CodeMatcher instructions, int value, int newValue)
  {
    instructions.MatchForward(false, new CodeMatch(OpCodes.Ldc_I4, value));
    if (instructions.IsInvalid)
    {
      Log.Warning($"Failed to find int {value} to replace with {newValue}.");
      return instructions;
    }

    return instructions.SetOperandAndAdvance(newValue);
  }
  public static CodeMatcher Replace(CodeMatcher instructions, double value, double newValue)
  {
    instructions.MatchForward(false, new CodeMatch(OpCodes.Ldc_R8, value));
    // For example BC patches some of these so needs a guard.
    if (instructions.IsInvalid)
    {
      Log.Warning($"Failed to find double {value} to replace with {newValue}.");
      return instructions;
    }

    return instructions.SetOperandAndAdvance(newValue);
  }
  public static CodeMatcher Replace(CodeMatcher instructions, float value, float newValue)
  {
    instructions.MatchForward(false, new CodeMatch(OpCodes.Ldc_R4, value));
    // For example BC patches some of these so needs a guard.
    if (instructions.IsInvalid)
    {
      Log.Warning($"Failed to find float {value} to replace with {newValue}.");
      return instructions;
    }

    return instructions.SetOperandAndAdvance(newValue);
  }

  public static CodeMatch Int(int value) => new(i => i.LoadsConstant(value));

  // Replaces every int constant (any ldc.i4 encoding) with a static field load.
  public static CodeMatcher ReplaceAllInts(CodeMatcher instructions, int value, FieldInfo field, int expected) => ReplaceAllInts(instructions, value, OpCodes.Ldsfld, field, expected);
  public static CodeMatcher ReplaceAllInts(CodeMatcher instructions, int value, int newValue, int expected) => ReplaceAllInts(instructions, value, OpCodes.Ldc_I4, newValue, expected);
  private static CodeMatcher ReplaceAllInts(CodeMatcher instructions, int value, OpCode opcode, object operand, int expected)
  {
    var count = 0;
    instructions.Start();
    while (true)
    {
      instructions.MatchForward(false, Int(value));
      if (instructions.IsInvalid) break;
      instructions.SetAndAdvance(opcode, operand);
      count++;
    }
    if (count != expected)
      Log.Warning($"Expected to replace int {value} {expected} times but replaced {count} times.");
    return instructions.Start();
  }

  // Replaces the first int constant that is directly followed by the given instruction.
  public static CodeMatcher ReplaceIntBefore(CodeMatcher instructions, int value, CodeMatch next, int newValue)
  {
    instructions.Start().MatchForward(false, Int(value), next);
    if (instructions.IsInvalid)
    {
      Log.Warning($"Failed to find int {value} to replace with {newValue}.");
      return instructions.Start();
    }
    instructions.SetAndAdvance(OpCodes.Ldc_I4, newValue);
    return instructions.Start();
  }

  public static CodeMatcher ReplaceSeed(CodeMatcher instructions, string name, float value)
  {
    instructions.MatchForward(false, new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(WorldGenerator), name)));
    // For example BC patches some of these so needs a guard.
    if (instructions.IsInvalid)
      return instructions;

    return instructions
      .Advance(-1)
      .SetAndAdvance(OpCodes.Ldc_R4, value)
      .RemoveInstruction();
  }

  public static float HeightToBaseHeight(float altitude) => altitude / 200f;
  public static bool IsServer() => ZNet.instance && ZNet.instance.IsServer();
  // Note: Intended that is client when no Znet instance (so stuff isn't loaded in the main menu).
  public static bool IsClient() => !IsServer();
}