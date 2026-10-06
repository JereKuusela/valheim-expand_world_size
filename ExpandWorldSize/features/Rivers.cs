using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
namespace ExpandWorldSize;

public class FindLakes
{
  public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
  {
    CodeMatcher matcher = new(instructions);
    // Looped coordinates are NOT streched, so limits must NOT be streched.
    matcher = Helper.Replace(matcher, -10000f, -Configuration.WorldRadius);
    matcher = Helper.Replace(matcher, -10000f, -Configuration.WorldRadius);
    matcher = Helper.Replace(matcher, 10000f, Configuration.WorldRadius);
    // And coordinates must be streched for GetBaseHeight.
    matcher = Stretch.Replace(matcher, OpCodes.Ldloc_3);
    matcher = Stretch.Replace(matcher, OpCodes.Ldloc_2);
    matcher = Helper.Replace(matcher, 10000f, Configuration.WorldRadius);
    matcher = Helper.Replace(matcher, 10000f, Configuration.WorldRadius);
    return matcher.InstructionEnumeration();
  }
}

public class IsRiverAllowed
{
  public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
  {
    CodeMatcher matcher = new(instructions);
    // Coordinates are NOT streched, so they must be streched for GetBaseHeight.
    matcher = Stretch.Replace(matcher, OpCodes.Ldfld);
    matcher = Stretch.Replace(matcher, OpCodes.Ldfld);
    return matcher.InstructionEnumeration();
  }
}

public class FindStreamStartPoint
{
  public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
  {
    CodeMatcher matcher = new(instructions);
    matcher = Helper.Replace(matcher, -10000f, -Configuration.WorldRadius);
    matcher = Helper.Replace(matcher, 10000f, Configuration.WorldRadius);
    matcher = Helper.Replace(matcher, -10000f, -Configuration.WorldRadius);
    matcher = Helper.Replace(matcher, 10000f, Configuration.WorldRadius);
    return matcher.InstructionEnumeration();
  }
}
public class AddRivers
{
  // Incoming coordinates are stretched, so they must be unstreched for the river database.
  public static void Prefix(ref float wx, ref float wy)
  {
    wx *= Configuration.WorldStretch;
    wy *= Configuration.WorldStretch;
  }
}