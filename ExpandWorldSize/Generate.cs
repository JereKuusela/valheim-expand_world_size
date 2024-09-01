
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using Service;
using UnityEngine;
using UnityEngine.Rendering;

namespace ExpandWorldSize;

[HarmonyPatch(typeof(WorldGenerator), nameof(WorldGenerator.Pregenerate)), HarmonyPriority(Priority.HigherThanNormal)]
public class Pregenerate
{
  static void Prefix(WorldGenerator __instance)
  {
    // River points must at least be cleaned.
    // But better clean up everything.
    __instance.m_riverCacheLock.EnterWriteLock();
    __instance.m_riverPoints = [];
    __instance.m_rivers = [];
    __instance.m_streams = [];
    __instance.m_lakes = [];
    __instance.m_cachedRiverGrid = new(-999999, -999999);
    __instance.m_cachedRiverPoints = [];
    __instance.m_riverCacheLock.ExitWriteLock();
  }
}

// Cache might have wrong map size so has to be fully reimplemented.
// This could be transpiled too but more complex.
[HarmonyPatch(typeof(Minimap), nameof(Minimap.TryLoadMinimapTextureData))]
public class PatchTryLoadMinimapTextureData
{
  static bool Prefix(Minimap __instance, ref bool __result)
  {
    __result = TryLoadMinimapTextureData(__instance);
    return false;
  }

  private static bool TryLoadMinimapTextureData(Minimap obj)
  {
    if (string.IsNullOrEmpty(obj.m_forestMaskTexturePath) || !File.Exists(obj.m_forestMaskTexturePath) || !File.Exists(obj.m_mapTexturePath) || !File.Exists(obj.m_heightTexturePath) || 33 != ZNet.World.m_worldVersion)
    {
      return false;
    }
    Stopwatch stopwatch = Stopwatch.StartNew();
    Texture2D texture2D = new Texture2D(obj.m_forestMaskTexture.width, obj.m_forestMaskTexture.height, TextureFormat.ARGB32, false);
    if (!texture2D.LoadImage(File.ReadAllBytes(obj.m_forestMaskTexturePath)))
      return false;
    if (obj.m_forestMaskTexture.width != texture2D.width || obj.m_forestMaskTexture.height != texture2D.height)
      return false;
    obj.m_forestMaskTexture.SetPixels(texture2D.GetPixels());
    obj.m_forestMaskTexture.Apply();
    if (!texture2D.LoadImage(File.ReadAllBytes(obj.m_mapTexturePath)))
      return false;
    if (obj.m_mapTexture.width != texture2D.width || obj.m_mapTexture.height != texture2D.height)
      return false;
    obj.m_mapTexture.SetPixels(texture2D.GetPixels());
    obj.m_mapTexture.Apply();
    if (!texture2D.LoadImage(File.ReadAllBytes(obj.m_heightTexturePath)))
      return false;
    if (obj.m_heightTexture.width != texture2D.width || obj.m_heightTexture.height != texture2D.height)
      return false;
    Color[] pixels = texture2D.GetPixels();
    for (int i = 0; i < obj.m_textureSize; i++)
    {
      for (int j = 0; j < obj.m_textureSize; j++)
      {
        int num = i * obj.m_textureSize + j;
        int num2 = (int)(pixels[num].r * 255f);
        int num3 = (int)(pixels[num].g * 255f);
        int num4 = (num2 << 8) + num3;
        float num5 = 127.5f;
        pixels[num].r = (float)num4 / num5;
      }
    }
    obj.m_heightTexture.SetPixels(pixels);
    obj.m_heightTexture.Apply();
    ZLog.Log("Loading minimap textures done [" + stopwatch.ElapsedMilliseconds.ToString() + "ms]");
    return true;
  }
}

[HarmonyPatch(typeof(Minimap), nameof(Minimap.GenerateWorldMap))]
public class MapGeneration
{
  // Some map mods may do stuff after generation which won't work with async.
  // So do one "fake" generate call to trigger those.
  static bool DoFakeGenerate = false;
  static bool Prefix(Minimap __instance)
  {
    if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null) return true;
    if (DoFakeGenerate)
    {
      DoFakeGenerate = false;
      return false;
    }
    if (BetterContinents.IsEnabled())
    {
      Log.Info($"Better Continents enabled, skipping map generation.");
      return true;
    }
    Game.instance.StartCoroutine(Coroutine(__instance));
    return false;
  }
  public static void Cancel()
  {
    if (CTS != null)
    {
      Log.Info($"Cancelling previous map generation.");
      CTS.Cancel();
      CTS = null;
    }
  }
  public static void UpdateTextureSize(Minimap map, int textureSize)
  {
    if (map.m_textureSize == textureSize) return;
    map.m_textureSize = textureSize;
    map.m_mapTexture = new(map.m_textureSize, map.m_textureSize, TextureFormat.RGB24, false)
    {
      wrapMode = TextureWrapMode.Clamp
    };
    map.m_forestMaskTexture = new(map.m_textureSize, map.m_textureSize, TextureFormat.RGBA32, false)
    {
      wrapMode = TextureWrapMode.Clamp
    };
    map.m_heightTexture = new(map.m_textureSize, map.m_textureSize, TextureFormat.RFloat, false)
    {
      wrapMode = TextureWrapMode.Clamp
    };
    map.m_fogTexture = new(map.m_textureSize, map.m_textureSize, TextureFormat.RGBA32, false)
    {
      wrapMode = TextureWrapMode.Clamp
    };
    map.m_explored = new bool[map.m_textureSize * map.m_textureSize];
    map.m_exploredOthers = new bool[map.m_textureSize * map.m_textureSize];
    map.m_mapImageLarge.material.SetTexture("_MainTex", map.m_mapTexture);
    map.m_mapImageLarge.material.SetTexture("_MaskTex", map.m_forestMaskTexture);
    map.m_mapImageLarge.material.SetTexture("_HeightTex", map.m_heightTexture);
    map.m_mapImageLarge.material.SetTexture("_FogTex", map.m_fogTexture);
    map.m_mapImageSmall.material.SetTexture("_MainTex", map.m_mapTexture);
    map.m_mapImageSmall.material.SetTexture("_MaskTex", map.m_forestMaskTexture);
    map.m_mapImageSmall.material.SetTexture("_HeightTex", map.m_heightTexture);
    map.m_mapImageSmall.material.SetTexture("_FogTex", map.m_fogTexture);
    map.Reset();
  }
  public static bool Generating => CTS != null;
  static CancellationTokenSource? CTS = null;
  static IEnumerator Coroutine(Minimap map)
  {
    Cancel();

    Log.Info($"Starting map generation.");
    Stopwatch stopwatch = Stopwatch.StartNew();
    Minimap.DeleteMapTextureData(ZNet.World.m_name);

    int size = map.m_textureSize * map.m_textureSize;
    var mapTexture = new Color32[size];
    var forestMaskTexture = new Color32[size];
    var heightTexture = new Color[size];
    var cachedTexture = new Color32[size];

    CancellationTokenSource cts = new();
    var ct = cts.Token;
    while (Marketplace.IsLoading())
      yield return null;
    var task = Generate(map, mapTexture, forestMaskTexture, heightTexture, cachedTexture, ct);
    CTS = cts;
    while (!task.IsCompleted)
      yield return null;

    if (task.IsFaulted)
      Log.Error($"Map generation failed!\n{task.Exception}");
    else if (!ct.IsCancellationRequested)
    {
      map.m_mapTexture.SetPixels32(mapTexture);
      yield return null;
      map.m_mapTexture.Apply();
      yield return null;

      map.m_forestMaskTexture.SetPixels32(forestMaskTexture);
      yield return null;
      map.m_forestMaskTexture.Apply();
      yield return null;

      map.m_heightTexture.SetPixels(heightTexture);
      yield return null;
      map.m_heightTexture.Apply();
      yield return null;
      // Some map mods may do stuff after generation which won't work with async.
      // So do one "fake" generate call to trigger those.
      DoFakeGenerate = true;
      map.GenerateWorldMap();
      Texture2D cached = new(map.m_textureSize, map.m_textureSize);
      cached.SetPixels32(cachedTexture);
      cached.Apply();
      Log.Info($"Map generation finished ({stopwatch.Elapsed.TotalSeconds:F0} seconds).");
      map.SaveMapTextureDataToDisk(map.m_forestMaskTexture, map.m_mapTexture, cached);
    }
    stopwatch.Stop();
    cts.Dispose();

    if (CTS == cts)
      CTS = null;
  }

  static async Task Generate(
      Minimap map, Color32[] mapTexture, Color32[] forestMaskTexture, Color[] heightTexture, Color32[] cachedtexture, CancellationToken ct)
  {
    await Task
        .Run(
          () =>
          {
            if (ct.IsCancellationRequested)
              ct.ThrowIfCancellationRequested();

            var wg = WorldGenerator.m_instance;
            var textureSize = map.m_textureSize; // default 2048
            var halfTextureSize = textureSize / 2;
            var pixelSize = map.m_pixelSize;   // default 12
            var halfPixelSize = pixelSize / 2f;
            var half = 127.5f;

            for (var i = 0; i < textureSize; i++)
            {
              for (var j = 0; j < textureSize; j++)
              {
                var wx = (j - halfTextureSize) * pixelSize + halfPixelSize;
                var wy = (i - halfTextureSize) * pixelSize + halfPixelSize;
                while (Marketplace.IsLoading())
                {
                  Log.Info("Waiting 100 ms for Marketplace to load...");
                  Thread.Sleep(100);
                }
                var biome = wg.GetBiome(wx, wy);
                var biomeHeight = wg.GetBiomeHeight(biome, wx, wy, out var mask);
                mapTexture[i * textureSize + j] = map.GetPixelColor(biome);
                forestMaskTexture[i * textureSize + j] = map.GetMaskColor(wx, wy, biomeHeight, biome);
                heightTexture[i * textureSize + j] = new(biomeHeight, 0f, 0f);

                var num = Mathf.Clamp((int)(biomeHeight * half), 0, 65025);
                var r = (byte)(num >> 8);
                var g = (byte)(num & 255);
                cachedtexture[i * textureSize + j] = new(r, g, 0, byte.MaxValue);
                if (ct.IsCancellationRequested)
                  ct.ThrowIfCancellationRequested();
              }
            }
          })
        .ConfigureAwait(continueOnCapturedContext: false);
  }
}

[HarmonyPatch(typeof(Game), nameof(Game.Logout))]
public class CancelOnLogout
{
  static void Prefix()
  {
    MapGeneration.Cancel();
  }
}
