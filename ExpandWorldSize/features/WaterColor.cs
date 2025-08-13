using HarmonyLib;
using UnityEngine;
namespace ExpandWorldSize;

[HarmonyPatch(typeof(Player), nameof(Player.AddKnownBiome))]
public class StartColorTransition
{
  public static void Postfix(Heightmap.Biome biome)
  {
    var isAshlands = biome == Heightmap.Biome.AshLands;
    WaterColor.StartTransition(isAshlands);
  }
}

[HarmonyPatch(typeof(Player), nameof(Player.OnSpawned))]
public class ResetColorTransition
{
  public static void Postfix()
  {
    WaterColor.StopTransition();
  }
}

public class WaterColor
{
  private static float TransitionProgress = 0f;
  private static bool Transitioning = false;
  public static bool? TargetAshlands;
  public static void Transition(float time)
  {
    if (!Transitioning) return;
    if (TargetAshlands == true)
    {
      TransitionProgress += 0.1f * time;
      if (TransitionProgress >= 1f)
        StopTransition();
    }
    else if (TargetAshlands == false)
    {
      TransitionProgress -= 0.1f * time;
      if (TransitionProgress <= 0f)
        StopTransition();
    }
    UpdateTransitions();
  }

  public static void StartTransition(bool toAshlands)
  {
    if (!Configuration.FixWaterColor) return;
    if (TargetAshlands == toAshlands) return;
    TargetAshlands = toAshlands;
    Transitioning = true;
  }
  public static void StopTransition()
  {
    if (!Transitioning) return;
    Transitioning = false;
    TransitionProgress = TargetAshlands == true ? 1f : 0f;
  }
  private static void UpdateTransitions()
  {
    foreach (var water in WaterVolume.Instances)
      UpdateTransition(water.m_waterSurface.sharedMaterial);
    var globalWater = EnvMan.instance?.transform.Find("WaterPlane").Find("watersurface");
    if (globalWater != null)
      UpdateTransition(globalWater.GetComponent<MeshRenderer>().sharedMaterial);
  }
  private static void UpdateTransition(Material mat)
  {
    InitColors(mat);
    var surfaceColor = Color.Lerp(WaterSurface, AshlandsSurface, TransitionProgress);
    var topColor = Color.Lerp(WaterTop, AshlandsTop, TransitionProgress);
    var bottomColor = Color.Lerp(WaterBottom, AshlandsBottom, TransitionProgress);
    var shallowColor = Color.Lerp(WaterShallow, AshlandsShallow, TransitionProgress);
    UpdateColors(mat, surfaceColor, topColor, bottomColor, shallowColor);
  }
  public static void Regenerate()
  {
    if (Player.m_localPlayer)
      StartTransition(Player.m_localPlayer.GetCurrentBiome() == Heightmap.Biome.AshLands);
    foreach (var water in WaterVolume.Instances)
      FixColors(water.m_waterSurface.sharedMaterial);
    var globalWater = EnvMan.instance?.transform.Find("WaterPlane").Find("watersurface");
    if (globalWater != null)
      FixColors(globalWater.GetComponent<MeshRenderer>().sharedMaterial);
  }
  public static void FixColors(Material mat)
  {
    InitColors(mat);
    if (Configuration.FixWaterColor)
    {
      UpdateTransition(mat);
    }
    else if (Configuration.RemoveAshlandsWater)
    {
      StopTransition();
      UpdateColors(mat, WaterSurface, WaterTop, WaterBottom, WaterShallow);
    }
    else
    {
      StopTransition();
      mat.SetColor("_SurfaceColor", WaterSurface);
      mat.SetColor("_AshlandsSurfaceColor", AshlandsSurface);
      mat.SetColor("_ColorTop", WaterTop);
      mat.SetColor("_AshlandsColorTop", AshlandsTop);
      mat.SetColor("_ColorBottom", WaterBottom);
      mat.SetColor("_AshlandsColorBottom", AshlandsBottom);
      mat.SetColor("_ColorBottomShallow", WaterShallow);
      mat.SetColor("_AshlandsColorBottomShallow", AshlandsShallow);
    }
  }

  private static void UpdateColors(Material mat, Color surface, Color top, Color bottom, Color shallow)
  {
    mat.SetColor("_SurfaceColor", surface);
    mat.SetColor("_AshlandsSurfaceColor", surface);
    mat.SetColor("_ColorTop", top);
    mat.SetColor("_AshlandsColorTop", top);
    mat.SetColor("_ColorBottom", bottom);
    mat.SetColor("_AshlandsColorBottom", bottom);
    mat.SetColor("_ColorBottomShallow", shallow);
    mat.SetColor("_AshlandsColorBottomShallow", shallow);
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