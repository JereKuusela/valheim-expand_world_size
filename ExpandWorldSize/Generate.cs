
using System.Collections;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
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
    __instance.m_riverPoints = new();
    __instance.m_rivers = new();
    __instance.m_streams = new();
    __instance.m_lakes = new();
    __instance.m_cachedRiverGrid = new(-999999, -999999);
    __instance.m_cachedRiverPoints = new WorldGenerator.RiverPoint[0];
    __instance.m_riverCacheLock.ExitWriteLock();
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
      EWS.Log.LogInfo($"Better Contintents enabled, skipping map generation.");
      return true;
    }
    Game.instance.StartCoroutine(Coroutine(__instance));
    return false;
  }
  public static void Cancel()
  {
    if (CTS != null)
    {
      EWS.Log.LogInfo($"Cancelling previous map generation.");
      CTS.Cancel();
      CTS = null;
    }
  }
  public static void UpdateTextureSize(Minimap map, int textureSize)
  {
    if (map.m_textureSize == textureSize) return;
    map.m_textureSize = textureSize;
    map.m_mapTexture = new Texture2D(map.m_textureSize, map.m_textureSize, TextureFormat.RGBA32, false)
    {
      wrapMode = TextureWrapMode.Clamp
    };
    map.m_forestMaskTexture = new Texture2D(map.m_textureSize, map.m_textureSize, TextureFormat.RGBA32, false)
    {
      wrapMode = TextureWrapMode.Clamp
    };
    map.m_heightTexture = new Texture2D(map.m_textureSize, map.m_textureSize, TextureFormat.RFloat, false)
    {
      wrapMode = TextureWrapMode.Clamp
    };
    map.m_fogTexture = new Texture2D(map.m_textureSize, map.m_textureSize, TextureFormat.RGBA32, false)
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

    EWS.Log.LogInfo($"Starting map generation.");
    Stopwatch stopwatch = Stopwatch.StartNew();

    int size = map.m_textureSize * map.m_textureSize;
    var mapTexture = new Color32[size];
    var forestMaskTexture = new Color32[size];
    var heightTexture = new Color[size];

    CancellationTokenSource cts = new();
    var ct = cts.Token;
    while (Marketplace.IsLoading())
      yield return null;
    var task = Generate(map, mapTexture, forestMaskTexture, heightTexture, ct);
    CTS = cts;
    while (!task.IsCompleted)
      yield return null;

    if (task.IsFaulted)
      EWS.Log.LogError($"Map generation failed!\n{task.Exception}");
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
      EWS.Log.LogInfo($"Map generation finished ({stopwatch.Elapsed.TotalSeconds:F0} seconds).");
    }
    stopwatch.Stop();
    cts.Dispose();

    if (CTS == cts)
      CTS = null;
  }

  static async Task Generate(
      Minimap map, Color32[] mapTexture, Color32[] forestMaskTexture, Color[] heightTexture, CancellationToken ct)
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

            for (var i = 0; i < textureSize; i++)
            {
              for (var j = 0; j < textureSize; j++)
              {
                var wx = (j - halfTextureSize) * pixelSize + halfPixelSize;
                var wy = (i - halfTextureSize) * pixelSize + halfPixelSize;
                while (Marketplace.IsLoading())
                {
                  EWS.Log.LogInfo("Waiting 100 ms for Marketplace to load...");
                  Thread.Sleep(100);
                }
                var biome = wg.GetBiome(wx, wy);
                var biomeHeight = wg.GetBiomeHeight(biome, wx, wy, out var mask);
                mapTexture[i * textureSize + j] = map.GetPixelColor(biome);
                forestMaskTexture[i * textureSize + j] = map.GetMaskColor(wx, wy, biomeHeight, biome);
                heightTexture[i * textureSize + j] = new Color(biomeHeight, 0f, 0f);
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
