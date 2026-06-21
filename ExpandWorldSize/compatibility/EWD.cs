using System.Reflection;
using BepInEx.Bootstrap;
using HarmonyLib;
using Service;

namespace ExpandWorldSize;

public class EWD
{
  public const string GUID = "expand_world_data";
  public static bool IsPresent = false;
  private static Assembly? Assembly;
  private static MethodInfo? SetSize;
  private static MethodInfo? MethodGetMinimapHeight;
  public static void Run()
  {
    if (!Chainloader.PluginInfos.TryGetValue(GUID, out var info)) return;
    Assembly = info.Instance.GetType().Assembly;
    AccessSetSize(Assembly);
    AccessGetMinimapHeight(Assembly);
    Log.Info("\"Expand World Data\" detected. Applying compatibility.");
    IsPresent = true;
  }
  private static void AccessSetSize(Assembly assembly)
  {
    var type = assembly.GetType("ExpandWorldData.WorldInfo");
    if (type == null) return;
    SetSize = AccessTools.Method(type, "Set");
  }
  private static void AccessGetMinimapHeight(Assembly assembly)
  {
    var type = assembly.GetType("ExpandWorldData.Api");
    if (type == null) return;
    MethodGetMinimapHeight = AccessTools.Method(type, "GetMinimapHeight");
  }

  public static void RefreshSize()
  {
    if (SetSize == null) return;
    SetSize.Invoke(null, [Configuration.WorldRadius, Configuration.WorldTotalRadius, Configuration.WorldStretch, Configuration.BiomeStretch]);
  }
  public static float GetMinimapHeight(float height, Heightmap.Biome biome)
  {
    if (MethodGetMinimapHeight == null) return height;
    return (float)MethodGetMinimapHeight.Invoke(null, [height, biome]);
  }
}
