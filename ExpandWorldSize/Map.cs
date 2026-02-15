using System.Runtime.InteropServices;
using HarmonyLib;
using UnityEngine;

namespace ExpandWorldSize;

[HarmonyPatch(typeof(Minimap), nameof(Minimap.Awake))]
public class MinimapAwake
{
  // Applies the map parameter changes.
  public static float OriginalPixelSize;
  public static int OriginalTextureSize;
  public static float OriginalMaxZoom;
  public static void Postfix(Minimap __instance)
  {
    OriginalTextureSize = __instance.m_textureSize;
    OriginalMaxZoom = __instance.m_maxZoom;
    OriginalPixelSize = __instance.m_pixelSize;
    Refresh(__instance);
  }

  public static bool Refresh(Minimap instance)
  {
    var newTextureSize = (int)(OriginalTextureSize * Configuration.MapSize);
    var newMaxZoom = OriginalMaxZoom * Mathf.Max(1f, Configuration.MapSize);
    var newPixelSize = CalculatePixelSize();
    if (instance.m_textureSize == newTextureSize && instance.m_maxZoom == newMaxZoom && instance.m_pixelSize == newPixelSize) return false;
    MapGeneration.UpdateTextureSize(Minimap.instance, newTextureSize);
    instance.m_maxZoom = newMaxZoom;
    instance.m_pixelSize = newPixelSize;
    return true;
  }
  private static float CalculatePixelSize()
  {
    if (Configuration.MapPixelSize != 0f) return OriginalPixelSize * Configuration.MapPixelSize;
    var sizeMultiplier = Configuration.WorldTotalRadius / 10500f;
    return OriginalPixelSize * sizeMultiplier / Configuration.MapSize;
  }
}


[HarmonyPatch(typeof(Minimap), nameof(Minimap.SetMapData))]
public class InitializeWhenDimensionsChange
{
  public static bool Prefix(Minimap __instance, byte[] data)
  {
    var obj = __instance;
    ZPackage zpackage = new(data);
    var num = zpackage.ReadInt();
    if (num >= 7) zpackage = zpackage.ReadCompressedPackage();
    int num2 = zpackage.ReadInt();
    if (obj.m_textureSize == num2) return true;
    // Base game code would stop initializxing.
    obj.Reset();
    obj.m_fogTexture.Apply();
    return false;
  }
}
