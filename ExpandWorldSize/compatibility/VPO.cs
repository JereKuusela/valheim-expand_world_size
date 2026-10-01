using System.Collections.Generic;
using System.Reflection.Emit;
using BepInEx.Bootstrap;
using HarmonyLib;
using Service;

namespace ExpandWorldSize;

// VPO replaces WaterVolume.UpdateFloaters with its own copy that hardcodes the 10500 world edge.
public class VPO
{
  public const string GUID = "dev.ontrigger.vpo";
  public static void Run()
  {
    if (!Chainloader.PluginInfos.TryGetValue(GUID, out var info)) return;
    var type = info.Instance.GetType().Assembly.GetType("ValheimPerformanceOptimizations.Patches.Water.VPOWaterVolumeManager");
    var method = type == null ? null : AccessTools.Method(type, "TryScheduleFloaterUpdates");
    if (method == null)
    {
      Log.Warning("\"Valheim Performance Optimizations\" detected but floater update method not found. Water edge may not scale.");
      return;
    }
    Log.Info("\"Valheim Performance Optimizations\" detected. Applying compatibility.");
    // Separate instance so that Patcher.Patch (UnpatchSelf) doesn't remove this.
    try
    {
      new Harmony(EWS.GUID + ".vpo").Patch(method, transpiler: new(AccessTools.Method(typeof(VPO), nameof(Transpiler))));
    }
    catch (System.Exception e)
    {
      Log.Warning($"Failed to apply VPO compatibility: {e.Message}");
    }
  }

  static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
  {
    // Getter call instead of a constant so config changes apply without repatching.
    return new CodeMatcher(instructions)
      .MatchForward(false, new CodeMatch(OpCodes.Ldc_R4, 10500f))
      .ThrowIfInvalid("Failed to find 10500f in VPO floater update.")
      .SetInstruction(new CodeInstruction(OpCodes.Call, AccessTools.PropertyGetter(typeof(Configuration), nameof(Configuration.WorldTotalRadius))))
      .InstructionEnumeration();
  }
}
