using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
    if (string.IsNullOrEmpty(obj.m_cachedMinimapMaskTexturePath) || !File.Exists(obj.m_cachedMinimapMaskTexturePath))
      return false;
    if (string.IsNullOrEmpty(obj.m_cachedMinimapBiomeTexturePath) || !File.Exists(obj.m_cachedMinimapBiomeTexturePath))
      return false;
    if (string.IsNullOrEmpty(obj.m_cachedMinimapHeightTexturePath) || !File.Exists(obj.m_cachedMinimapHeightTexturePath))
      return false;
    if (string.IsNullOrEmpty(obj.m_cachedMinimapMetaPath) || !File.Exists(obj.m_cachedMinimapMetaPath))
      return false;
    if (ZNet.World.m_worldVersion != Version.World.DeepNorth)
      return false;

    try
    {
      var value = File.ReadAllBytes(obj.m_cachedMinimapMetaPath);
      if (BitConverter.ToInt32(value, 0) != ZNet.World.m_seed)
      {
        ZLog.LogWarning("Regenerating minimap: Minimap seed is wrong.");
        return false;
      }
      var cachedMinimap = (Version.CachedMinimap)BitConverter.ToInt32(value, 4);
      if (cachedMinimap != Version.CachedMinimap.Original)
      {
        ZLog.Log($"Regenerating minimap: Minimap version changed from: {cachedMinimap} to {Version.CachedMinimap.Original}.");
        return false;
      }
    }
    catch (Exception)
    {
      return false;
    }

    Stopwatch stopwatch = Stopwatch.StartNew();
    try
    {
      var maskBytes = File.ReadAllBytes(obj.m_cachedMinimapMaskTexturePath);
      var biomeBytes = File.ReadAllBytes(obj.m_cachedMinimapBiomeTexturePath);
      var heightBytes = File.ReadAllBytes(obj.m_cachedMinimapHeightTexturePath);

      var maskColors = Utils.CompressedBufferToColors(maskBytes);
      var biomeColors = Utils.CompressedBufferToColors(biomeBytes);
      var heightColors = Utils.CompressedHalfBufferToRedChannel(heightBytes);

      var expectedLength = obj.m_textureSize * obj.m_textureSize;
      if (maskColors.Length != expectedLength || biomeColors.Length != expectedLength || heightColors.Length != expectedLength)
      {
        ZLog.LogWarning($"Regenerating minimap: Minimap texture size mismatch. Expected length: {expectedLength}, but got maskColors: {maskColors.Length}, biomeColors: {biomeColors.Length}, heightColors: {heightColors.Length}");
        return false;
      }

      obj.m_forestMaskTexture.SetPixels32(maskColors);
      obj.m_forestMaskTexture.Apply();
      obj.m_mapTexture.SetPixels32(biomeColors);
      obj.m_mapTexture.Apply();
      obj.m_heightTexture.SetPixels(heightColors);
      obj.m_heightTexture.Apply();
    }
    catch (Exception ex)
    {
      ZLog.LogWarning("Regenerating minimap: Error loading minimap files: " + ex.Message + " Stacktrace: " + ex.StackTrace);
      return false;
    }
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
    // BC and LPA both include multi-threaded map generation.
    if (BetterContinents.IsEnabled())
    {
      Log.Info($"Better Continents enabled, skipping map generation.");
      return true;
    }
    if (LPACompatibility.IsEnabled())
    {
      Log.Info($"Location Placement Accelerator detected, skipping map generation.");
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
    map.m_explored = new BitArray(map.m_textureSize * map.m_textureSize, false);
    map.m_exploredOthers = new BitArray(map.m_textureSize * map.m_textureSize, false);
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
    var biomePixels = new Color32[size];
    var maskPixels = new Color32[size];
    var heightPixels = new Color[size];
    var heightArray = new float[size];

    CancellationTokenSource cts = new();
    var ct = cts.Token;
    while (Marketplace.IsLoading())
      yield return null;
    var task = Generate(map, biomePixels, maskPixels, heightPixels, heightArray, ct);
    CTS = cts;
    while (!task.IsCompleted)
      yield return null;

    if (task.IsFaulted)
      Log.Error($"Map generation failed!\n{task.Exception}");
    else if (!ct.IsCancellationRequested)
    {
      map.m_mapTexture.SetPixels32(biomePixels);
      yield return null;
      map.m_mapTexture.Apply();
      yield return null;

      map.m_forestMaskTexture.SetPixels32(maskPixels);
      yield return null;
      map.m_forestMaskTexture.Apply();
      yield return null;

      map.m_heightTexture.SetPixels(heightPixels);
      yield return null;
      map.m_heightTexture.Apply();
      yield return null;
      // Some map mods may do stuff after generation which won't work with async.
      // So do one "fake" generate call to trigger those.
      DoFakeGenerate = true;
      map.GenerateWorldMap();
      var workers = Math.Max(1, Environment.ProcessorCount - 2);
      Log.Info($"Map generation finished ({stopwatch.Elapsed.TotalSeconds:F1}s, parallel, {workers} workers).");
      if (FileHelpers.LocalStorageSupport == LocalStorageSupport.Supported)
        map.SaveMapTextureDataToDisk(maskPixels, biomePixels, heightArray);
    }
    stopwatch.Stop();
    cts.Dispose();

    if (CTS == cts)
      CTS = null;
  }

  static async Task Generate(
      Minimap map, Color32[] biomePixels, Color32[] maskPixels, Color[] heightPixels, float[] heightArray, CancellationToken ct)
  {
    await Task
        .Run(
          () =>
          {
            var wg = WorldGenerator.instance;
            var textureSize = map.m_textureSize; // default 2048
            var halfTextureSize = textureSize / 2;
            var pixelSize = map.m_pixelSize;   // default 12
            var halfPixelSize = pixelSize / 2f;

            // Matches BC and LAP to leave 2 cores for other uses like GUI.
            var workers = Math.Max(1, Environment.ProcessorCount - 2);
            var pOpts = new ParallelOptions
            {
              MaxDegreeOfParallelism = workers,
              CancellationToken = ct
            };

            try
            {
              Parallel.For(0, textureSize, pOpts, i =>
                  {
                    for (var j = 0; j < textureSize; j++)
                    {
                      var wx = (j - halfTextureSize) * pixelSize + halfPixelSize;
                      var wy = (i - halfTextureSize) * pixelSize + halfPixelSize;
                      var biome = wg.GetBiome(wx, wy, 0.02f, false);
                      var biomeHeight = wg.GetBiomeHeight(biome, wx, wy, out var mask, false, true);
                      var index = i * textureSize + j;
                      biomePixels[index] = map.GetPixelColor(biome);
                      maskPixels[index] = map.GetMaskColor(wx, wy, biomeHeight, biome);
                      biomeHeight = EWD.GetMinimapHeight(biomeHeight, biome);
                      heightPixels[index] = new(biomeHeight, 0f, 0f);
                      heightArray[index] = biomeHeight;
                    }
                  });
            }
            catch (OperationCanceledException)
            {
              // Expected on Game.Logout. The coroutine gates texture upload on  ct.IsCancellationRequested, so no further action is needed here.
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