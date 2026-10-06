using System.Collections.Generic;
using HarmonyLib;

namespace ExpandWorldSize;

public class HeightSeed
{
  static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions)
  {
    CodeMatcher matcher = new(instructions);
    matcher = Helper.ReplaceSeed(matcher, nameof(WorldGenerator.m_offset3), Configuration.HeightSeed!.Value);
    return matcher.InstructionEnumeration();
  }
  public static IEnumerable<CodeInstruction> Ashlands(IEnumerable<CodeInstruction> instructions) => Transpile(instructions);
  public static IEnumerable<CodeInstruction> Forest(IEnumerable<CodeInstruction> instructions) => Transpile(instructions);
  public static IEnumerable<CodeInstruction> Meadows(IEnumerable<CodeInstruction> instructions) => Transpile(instructions);
  public static IEnumerable<CodeInstruction> DeepNorth(IEnumerable<CodeInstruction> instructions) => Transpile(instructions);
  public static IEnumerable<CodeInstruction> Plains(IEnumerable<CodeInstruction> instructions) => Transpile(instructions);
  public static IEnumerable<CodeInstruction> Mountain(IEnumerable<CodeInstruction> instructions) => Transpile(instructions);
  public static IEnumerable<CodeInstruction> Mistlands(IEnumerable<CodeInstruction> instructions) => Transpile(instructions);
}
