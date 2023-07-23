
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using HarmonyLib;
using UnityEngine;

namespace ExpandWorldSize;

public class Generate
{
  public static int LastTaskIndex = 0;
  public static bool Generating => WorldGeneration.Generating || MapGeneration.Generating;
  public static void World()
  {
    EWD.RefreshSize();
    GetBaseHeight.Refresh();
    var wg = WorldGenerator.instance;
    if (wg != null && !wg.m_world.m_menu)
      Game.instance.StartCoroutine(WorldGeneration.Coroutine(wg));
  }
  public static void Cancel()
  {
    WorldGeneration.Cancel();
    MapGeneration.Cancel();
  }
}


[HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.GenerateLocations), new Type[0])]
public class LocationGeneration
{
  static void Prefix()
  {
    if (WorldGeneration.HasLoaded) return;
    WorldGeneration.GenerateSync(WorldGenerator.instance);
    // This is called at ZNet.Start before Minimap.Start.
    // So doing Minimap.instance.GenerateWorldMap() is pointless and may even cause issues with other mods.
  }
}

[HarmonyPatch(typeof(WorldGenerator), nameof(WorldGenerator.Pregenerate))]
public class WorldGeneration
{
  public static void GenerateSync(WorldGenerator wg)
  {
    EWS.CancelRegenerate();
    EWS.Log.LogInfo("Started world generation.");
    var stopwatch = Stopwatch.StartNew();
    wg.m_riverPoints.Clear();
    wg.m_cachedRiverGrid = new Vector2i(-999999, -999999);
    wg.m_cachedRiverPoints = null;
    wg.FindLakes();
    wg.m_rivers = wg.PlaceRivers();
    wg.m_streams = wg.PlaceStreams();
    EWS.Log.LogInfo($"Finished world generation ({stopwatch.Elapsed.TotalSeconds:F0} seconds).");
    stopwatch.Stop();
    HasLoaded = true;
  }
  public static bool HasLoaded = false;
  static bool Prefix(WorldGenerator __instance)
  {
    if (__instance.m_world.m_menu) return true;
    if (HasLoaded)
      Game.instance.StartCoroutine(Coroutine(__instance));
    else
      GenerateSync(__instance);
    return false;
  }
  public static void Cancel()
  {
    if (CTS != null)
    {
      EWS.Log.LogInfo("Cancelling previous world generation.");
      CTS.Cancel();
      CTS = null;
    }
  }
  public static bool Generating => CTS != null;
  static CancellationTokenSource? CTS = null;
  public static IEnumerator Coroutine(WorldGenerator wg)
  {
    Cancel();
    MapGeneration.Cancel();

    EWS.Log.LogInfo($"Started world generation.");
    var stopwatch = Stopwatch.StartNew();

    CancellationTokenSource cts = new();
    var ct = cts.Token;

    CTS = cts;
    wg.m_riverPoints.Clear();
    wg.m_cachedRiverGrid = new Vector2i(-999999, -999999);
    wg.m_cachedRiverPoints = null;
    wg.FindLakes();
    if (ct.IsCancellationRequested)
      yield break;
    yield return null;
    wg.m_rivers = wg.PlaceRivers();
    if (ct.IsCancellationRequested)
      yield break;
    yield return null;
    wg.m_streams = wg.PlaceStreams();
    if (ct.IsCancellationRequested)
      yield break;
    yield return null;
    foreach (var heightmap in UnityEngine.Object.FindObjectsOfType<Heightmap>())
    {
      heightmap.m_buildData = null;
      heightmap.Regenerate();
    }
    if (ct.IsCancellationRequested)
      yield break;
    yield return null;
    if (ClutterSystem.instance)
      ClutterSystem.instance.ClearAll();
    SetupMaterial.Refresh();
    WaterLayerFix.Refresh(EnvMan.instance);
    if (ct.IsCancellationRequested)
      yield break;
    yield return null;
    HasLoaded = true;
    EWS.Log.LogInfo($"Finished world generation ({stopwatch.Elapsed.TotalSeconds:F0} seconds).");
    stopwatch.Stop();
    cts.Dispose();

    if (CTS == cts)
      CTS = null;
    if (Minimap.instance && ZNet.instance && !ZNet.instance.IsDedicated())
      Minimap.instance.GenerateWorldMap();
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
    if (!WorldGeneration.HasLoaded || WorldGeneration.Generating) return false;
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

[HarmonyPatch(typeof(Game), nameof(Game.FindSpawnPoint))]
public class FindSpawnPoint
{
  static bool Prefix() => WorldGeneration.HasLoaded;
}
[HarmonyPatch(typeof(Game), nameof(Game.Logout))]
public class CancelOnLogout
{
  static void Prefix()
  {
    EWS.CancelRegenerate();
    WorldGeneration.HasLoaded = false;
  }
}
