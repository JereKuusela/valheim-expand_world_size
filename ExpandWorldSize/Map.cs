using System.Collections;
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
    var oldSize = instance.m_textureSize;
    var oldPixelSize = instance.m_pixelSize;
    var oldExplored = instance.m_explored;
    var oldExploredOthers = instance.m_exploredOthers;
    MapGeneration.UpdateTextureSize(instance, newTextureSize);
    instance.m_maxZoom = newMaxZoom;
    instance.m_pixelSize = newPixelSize;
    RemapExplored(instance, oldSize, oldPixelSize, oldExplored, oldExploredOthers);
    return true;
  }

  // The fog layout depends on the size and scale, so explored pixels are moved to the matching world positions.
  private static void RemapExplored(Minimap map, int oldSize, float oldPixelSize, BitArray? oldExplored, BitArray? oldExploredOthers)
  {
    // Textures are created later when they don't exist yet.
    if (map.m_fogTexture == null || oldExplored == null || oldExploredOthers == null) return;
    var size = map.m_textureSize;
    BitArray explored = new(size * size, false);
    BitArray exploredOthers = new(size * size, false);
    var scale = map.m_pixelSize / oldPixelSize;
    var half = size / 2;
    var oldHalf = oldSize / 2;
    for (var y = 0; y < size; y++)
    {
      var oldY = Mathf.RoundToInt((y - half) * scale + oldHalf);
      if (oldY < 0 || oldY >= oldSize) continue;
      for (var x = 0; x < size; x++)
      {
        var oldX = Mathf.RoundToInt((x - half) * scale + oldHalf);
        if (oldX < 0 || oldX >= oldSize) continue;
        var oldIndex = oldY * oldSize + oldX;
        var index = y * size + x;
        explored[index] = oldExplored[oldIndex];
        exploredOthers[index] = oldExploredOthers[oldIndex];
      }
    }
    map.ResetAndExplore(explored, exploredOthers);
    map.m_fogTexture.Apply();
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
