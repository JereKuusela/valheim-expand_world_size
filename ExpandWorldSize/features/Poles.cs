using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace ExpandWorldSize;

public class GetAshlandsHeight
{
  public static readonly double DefaultWidthRestriction = 7500f;
  public static readonly double DefaultLengthRestriction = 1000f;

  public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
  {
    return new CodeMatcher(instructions)
      .MatchForward(false, new CodeMatch(OpCodes.Ldc_R8, 1000.0))
      .SetOperandAndAdvance(Configuration.AshlandsLengthRestriction)
      .MatchForward(false, new CodeMatch(OpCodes.Ldc_R8, 7500.0))
      .SetOperandAndAdvance(Configuration.AshlandsWidthRestriction)
      .InstructionEnumeration();
  }
}

public class Poles
{
  public static bool DisableGap(ref double __result)
  {
    __result = 1d;
    return false;
  }
}
