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

[HarmonyPatch(typeof(Player), nameof(Player.AddKnownBiome))]
public class AddKnownBiome
{
  public static void Postfix()
  {
    if (!Configuration.FixWaterColor) return;
    SetupMaterial.Refresh();
    ScaleGlobalWaterSurface.Refresh(EnvMan.instance);
  }
}
[HarmonyPatch(typeof(WaterVolume), nameof(WaterVolume.SetupMaterial))]
public class SetupMaterial
{
  public static void Refresh()
  {
    var objects = Object.FindObjectsOfType<WaterVolume>();
    foreach (var water in objects)
    {
      water.m_waterSurface.sharedMaterial.SetFloat("_WaterEdge", Configuration.WorldTotalRadius);
      FixColors(water.m_waterSurface.sharedMaterial);
    }
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
    FixColors(__instance.m_waterSurface.sharedMaterial);
  }
  public static void FixColors(Material mat)
  {
    InitColors(mat);
    if (Configuration.FixWaterColor && Player.m_localPlayer)
    {
      var isAshlands = Player.m_localPlayer.GetCurrentBiome() == Heightmap.Biome.AshLands;
      if (isAshlands)
      {
        mat.SetColor("_SurfaceColor", AshlandsSurface);
        mat.SetColor("_AshlandsSurfaceColor", AshlandsSurface);
        mat.SetColor("_ColorTop", AshlandsTop);
        mat.SetColor("_AshlandsColorTop", AshlandsTop);
        mat.SetColor("_ColorBottom", AshlandsBottom);
        mat.SetColor("_AshlandsColorBottom", AshlandsBottom);
        mat.SetColor("_ColorBottomShallow", AshlandsShallow);
        mat.SetColor("_AshlandsColorBottomShallow", AshlandsShallow);

      }
      else
      {
        mat.SetColor("_SurfaceColor", WaterSurface);
        mat.SetColor("_AshlandsSurfaceColor", WaterSurface);
        mat.SetColor("_ColorTop", WaterTop);
        mat.SetColor("_AshlandsColorTop", WaterTop);
        mat.SetColor("_ColorBottom", WaterBottom);
        mat.SetColor("_AshlandsColorBottom", WaterBottom);
        mat.SetColor("_ColorBottomShallow", WaterShallow);
        mat.SetColor("_AshlandsColorBottomShallow", WaterShallow);
      }
    }
    else if (Configuration.RemoveAshlandsWater)
    {
      mat.SetColor("_AshlandsSurfaceColor", WaterSurface);
      mat.SetColor("_AshlandsColorTop", WaterTop);
      mat.SetColor("_AshlandsColorBottom", WaterBottom);
      mat.SetColor("_AshlandsColorBottomShallow", WaterShallow);
    }
    else
    {
      mat.SetColor("_AshlandsSurfaceColor", AshlandsSurface);
      mat.SetColor("_AshlandsColorTop", AshlandsTop);
      mat.SetColor("_AshlandsColorBottom", AshlandsBottom);
      mat.SetColor("_AshlandsColorBottomShallow", AshlandsShallow);
    }
  }
  private static bool InitDone = false;
  private static void InitColors(Material mat)
  {
    if (InitDone) return;
    InitDone = true;
    WaterSurface = mat.GetColor("_SurfaceColor");
    AshlandsSurface = mat.GetColor("_AshlandsSurfaceColor");
    WaterTop = mat.GetColor("_ColorTop");
    AshlandsTop = mat.GetColor("_AshlandsColorTop");
    WaterBottom = mat.GetColor("_ColorBottom");
    AshlandsBottom = mat.GetColor("_AshlandsColorBottom");
    WaterShallow = mat.GetColor("_ColorBottomShallow");
    AshlandsShallow = mat.GetColor("_AshlandsColorBottomShallow");
  }
  private static Color WaterSurface;
  private static Color AshlandsSurface;
  private static Color WaterTop;
  private static Color AshlandsTop;
  private static Color WaterBottom;
  private static Color AshlandsBottom;
  private static Color WaterShallow;
  private static Color AshlandsShallow;
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
    SetupMaterial.FixColors(mat);
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