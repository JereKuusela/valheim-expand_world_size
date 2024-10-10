using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using Service;

namespace ExpandWorldSize;

public class GetAshlandsHeight
{
  private static readonly double DefaultWidthRestriction = 7500f;
  private static double PatchedWidthRestriction = DefaultWidthRestriction;
  private static readonly double DefaultLengthRestriction = 1000f;
  private static double PatchedLengthRestriction = DefaultLengthRestriction;
  public static void Patch(Harmony harmony, double widthRestriction, double lengthRestriction)
  {
    if (PatchedWidthRestriction == widthRestriction && PatchedLengthRestriction == lengthRestriction) return;
    var method = AccessTools.Method(typeof(WorldGenerator), nameof(WorldGenerator.GetAshlandsHeight));
    var transpiler = AccessTools.Method(typeof(GetAshlandsHeight), nameof(Transpiler));
    PatchedWidthRestriction = widthRestriction;
    PatchedLengthRestriction = lengthRestriction;
    harmony.Unpatch(method, transpiler);
    if (PatchedWidthRestriction != DefaultWidthRestriction || PatchedLengthRestriction != DefaultLengthRestriction)
      harmony.Patch(method, transpiler: new HarmonyMethod(transpiler));
  }

  static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
  {
    return new CodeMatcher(instructions)
      .MatchForward(false, new CodeMatch(OpCodes.Ldc_R8, 1000.0))
      .SetOperandAndAdvance(PatchedLengthRestriction)
      .MatchForward(false, new CodeMatch(OpCodes.Ldc_R8, 7500.0))
      .SetOperandAndAdvance(PatchedWidthRestriction)
      .InstructionEnumeration();
  }
}

public class CreateAshlandsGap
{
  private static bool IsPatched = false;
  public static void Patch(Harmony harmony, bool doPatch)
  {
    if (IsPatched == doPatch) return;
    var method = AccessTools.Method(typeof(WorldGenerator), nameof(WorldGenerator.CreateAshlandsGap));
    var prefix = AccessTools.Method(typeof(CreateAshlandsGap), nameof(DisableGap));
    IsPatched = doPatch;
    if (doPatch)
      harmony.Patch(method, prefix: new HarmonyMethod(prefix));
    else
      harmony.Unpatch(method, prefix);

  }

  static bool DisableGap(ref double __result)
  {
    __result = 1d;
    return false;
  }
}

public class CreateDeepNorthGap
{
  private static bool IsPatched = false;
  public static void Patch(Harmony harmony, bool doPatch)
  {
    if (IsPatched == doPatch) return;
    var method = AccessTools.Method(typeof(WorldGenerator), nameof(WorldGenerator.CreateDeepNorthGap));
    var prefix = AccessTools.Method(typeof(CreateAshlandsGap), nameof(DisableGap));
    IsPatched = doPatch;
    if (doPatch)
      harmony.Patch(method, prefix: new HarmonyMethod(prefix));
    else
      harmony.Unpatch(method, prefix);
  }

  static bool DisableGap(ref double __result)
  {
    __result = 1d;
    return false;
  }
}
