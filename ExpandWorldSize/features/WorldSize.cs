using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
namespace ExpandWorldSize;

public class WorldSizeHelper
{
  public static IEnumerable<CodeInstruction> EdgeCheck(IEnumerable<CodeInstruction> instructions)
  {
    CodeMatcher matcher = new(instructions);
    matcher = Helper.Replace(matcher, 10420f, Configuration.WorldTotalRadius - 80);
    matcher = Helper.Replace(matcher, 10500f, Configuration.WorldTotalRadius);
    return matcher.InstructionEnumeration();
  }
}

[HarmonyPatch(typeof(Ship), nameof(Ship.ApplyEdgeForce))]
public class ApplyEdgeForce
{
  static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) => WorldSizeHelper.EdgeCheck(instructions);
}

[HarmonyPatch(typeof(Player), nameof(Player.EdgeOfWorldKill))]
public class EdgeOfWorldKill
{
  static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) => WorldSizeHelper.EdgeCheck(instructions);

  // Safer to simply skip when in dungeons.
  static bool Prefix(Player __instance) => __instance.transform.position.y < 4000f;
}

[HarmonyPatch(typeof(WorldGenerator), nameof(WorldGenerator.GetAshlandsHeight))]
public class GetAshlandsHeightSize
{
  static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
  {
    if (Patcher.IsMenu) return instructions;
    CodeMatcher matcher = new(instructions);
    // Incoming coordinates are stretched, so all limits must be stretched as well.
    matcher = Helper.Replace(matcher, 10150d, (Configuration.WorldTotalRadius + 150f) / Configuration.WorldStretch);
    return matcher.InstructionEnumeration();
  }
}

[HarmonyPatch(typeof(WorldGenerator), nameof(WorldGenerator.GetBaseHeight))]
public class GetBaseHeight
{
  static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
  {
    if (Patcher.IsMenu) return instructions;
    CodeMatcher matcher = new(instructions);
    // Skipping the menu part.
    matcher = matcher.MatchForward(false, new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(WorldGenerator), nameof(WorldGenerator.m_offset1))));
    if (Configuration.OffsetX != null)
      matcher = Helper.ReplaceSeed(matcher, nameof(WorldGenerator.m_offset0), Configuration.OffsetX.Value);
    if (Configuration.OffsetY != null)
      matcher = Helper.ReplaceSeed(matcher, nameof(WorldGenerator.m_offset1), Configuration.OffsetY.Value);
    // Incoming coordinates are stretched, so all limits must be stretched as well.
    matcher = Helper.Replace(matcher, 10000f, Configuration.StrechedWorldRadius);
    matcher = Helper.Replace(matcher, 10000f, Configuration.StrechedWorldRadius);
    matcher = Helper.Replace(matcher, 10500f, Configuration.StrechedWorldTotalRadius);
    matcher = Helper.Replace(matcher, 10490f, (Configuration.WorldTotalRadius - 10f) / Configuration.WorldStretch);
    matcher = Helper.Replace(matcher, 10500f, Configuration.StrechedWorldTotalRadius);
    return matcher.InstructionEnumeration();
  }
}

[HarmonyPatch(typeof(WaterVolume), nameof(WaterVolume.SetupMaterial))]
public class SetupMaterial
{
  public static void Refresh()
  {
    foreach (var water in WaterVolume.Instances)
      water.m_waterSurface.sharedMaterial.SetFloat("_WaterEdge", Configuration.WorldTotalRadius);
  }
  public static void Prefix(WaterVolume __instance)
  {
    __instance.m_waterSurface.sharedMaterial.SetFloat("_WaterEdge", Configuration.WorldTotalRadius);
    // Zone water should be at y 30, anything above that is dungeon water and anything below is end of the world.
    if (__instance.transform.position.y < 30.001f && __instance.transform.position.y > 29.999f && __instance.m_collider is BoxCollider box)
    {
      // Default is -20 center with 60 size, probably 10 meters extra for waves.
      // -100 meters should give plenty of space for deeper water.
      box.center = new Vector3(0, -100, 0);
      box.size = new Vector3(64, 220, 64);
    }
    WaterColor.FixColors(__instance.m_waterSurface.sharedMaterial);
  }
}

[HarmonyPatch(typeof(EnvMan), nameof(EnvMan.Awake))]
public class ScaleGlobalWaterSurface
{
  public static void Refresh(EnvMan obj)
  {
    var water = obj.transform.Find("WaterPlane").Find("watersurface");
    if (!water) return;
    var mat = water.GetComponent<MeshRenderer>().sharedMaterial;
    mat.SetFloat("_WaterEdge", Configuration.WorldTotalRadius);
  }
  public static void Postfix(EnvMan __instance) => Refresh(__instance);
}

[HarmonyPatch(typeof(EnvMan), nameof(EnvMan.UpdateWind))]
public class UpdateWind
{
  static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
  {
    CodeMatcher matcher = new(instructions);
    matcher = Helper.Replace(matcher, 10500f, Configuration.WorldRadius);
    // Removes the subtraction of m_edgeOfWorldWidth (already applied above).
    matcher = matcher
      .SetOpcodeAndAdvance(OpCodes.Nop)
      .SetOpcodeAndAdvance(OpCodes.Nop)
      .SetOpcodeAndAdvance(OpCodes.Nop);
    matcher = Helper.Replace(matcher, 10500f, Configuration.WorldRadius);
    // Removes the subtraction of m_edgeOfWorldWidth (already applied above).
    matcher = matcher
      .SetOpcodeAndAdvance(OpCodes.Nop)
      .SetOpcodeAndAdvance(OpCodes.Nop)
      .SetOpcodeAndAdvance(OpCodes.Nop);
    matcher = Helper.Replace(matcher, 10500f, Configuration.WorldTotalRadius);
    return matcher.InstructionEnumeration();
  }
}

[HarmonyPatch(typeof(WaterVolume), nameof(WaterVolume.GetWaterSurface))]
public class GetWaterSurface
{
  static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
  {
    CodeMatcher matcher = new(instructions);
    matcher = Helper.Replace(matcher, 10500f, Configuration.WorldTotalRadius);
    return matcher.InstructionEnumeration();
  }
}