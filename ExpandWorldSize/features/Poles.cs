using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using Service;

namespace ExpandWorldSize;

public class GetAshlandsHeight
{
  private static readonly double DefaultWidthRestriction = 7500f;
  private static double WidthRestriction = DefaultWidthRestriction;
  private static readonly double DefaultLengthRestriction = 1000f;
  private static double LengthRestriction = DefaultLengthRestriction;
  public static bool Patch(Harmony harmony, double widthRestriction, double lengthRestriction)
  {
    if (WidthRestriction == widthRestriction && LengthRestriction == lengthRestriction) return false;
    var method = AccessTools.Method(typeof(WorldGenerator), nameof(WorldGenerator.GetAshlandsHeight));
    var transpiler = AccessTools.Method(typeof(GetAshlandsHeight), nameof(Transpiler));
    WidthRestriction = widthRestriction;
    LengthRestriction = lengthRestriction;
    harmony.Unpatch(method, transpiler);
    if (WidthRestriction != DefaultWidthRestriction || LengthRestriction != DefaultLengthRestriction)
      harmony.Patch(method, transpiler: new HarmonyMethod(transpiler));

    return true;
  }

  static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
  {
    return new CodeMatcher(instructions)
      .MatchForward(false, new CodeMatch(OpCodes.Ldc_R8, 1000.0))
      .SetOperandAndAdvance(LengthRestriction)
      .MatchForward(false, new CodeMatch(OpCodes.Ldc_R8, 7500.0))
      .SetOperandAndAdvance(WidthRestriction)
      .InstructionEnumeration();
  }
}

public class CreateAshlandsGap
{
  private static bool IsPatched = false;
  public static bool Patch(Harmony harmony, bool doPatch)
  {
    if (IsPatched == doPatch) return false;
    var method = AccessTools.Method(typeof(WorldGenerator), nameof(WorldGenerator.CreateAshlandsGap));
    var prefix = AccessTools.Method(typeof(CreateAshlandsGap), nameof(DisableGap));
    IsPatched = doPatch;
    if (doPatch)
    {
      Log.Info("Patching CreateAshlandsGap");
      harmony.Patch(method, prefix: new HarmonyMethod(prefix));
    }
    else
    {
      Log.Info("Unpatching CreateAshlandsGap");
      harmony.Unpatch(method, prefix);
    }
    return true;
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
  public static bool Patch(Harmony harmony, bool doPatch)
  {
    if (IsPatched == doPatch) return false;
    var method = AccessTools.Method(typeof(WorldGenerator), nameof(WorldGenerator.CreateDeepNorthGap));
    var prefix = AccessTools.Method(typeof(CreateAshlandsGap), nameof(DisableGap));
    IsPatched = doPatch;
    if (doPatch)
    {
      Log.Info("Patching CreateDeepNorthGap");
      harmony.Patch(method, prefix: new HarmonyMethod(prefix));
    }
    else
    {
      Log.Info("Unpatching CreateDeepNorthGap");
      harmony.Unpatch(method, prefix);
    }
    return true;
  }

  static bool DisableGap(ref double __result)
  {
    __result = 1d;
    return false;
  }
}
