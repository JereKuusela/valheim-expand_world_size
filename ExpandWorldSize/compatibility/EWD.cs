using System.Reflection;
using BepInEx.Bootstrap;
using HarmonyLib;
using Service;

namespace ExpandWorldSize;

public class EWD
{
  public const string GUID = "expand_world_data";
  private static Assembly? Assembly;
  private static MethodInfo? SetSize;
  public static void Run()
  {
    if (!Chainloader.PluginInfos.TryGetValue(GUID, out var info)) return;
    Assembly = info.Instance.GetType().Assembly;
    var type = Assembly.GetType("ExpandWorldData.WorldInfo");
    if (type == null) return;
    SetSize = AccessTools.Method(type, "Set");
    if (SetSize == null) return;
    Log.Info("\"Expand World Data\" detected. Applying compatibility.");
  }

  public static void RefreshSize()
  {
    if (SetSize == null) return;
    SetSize.Invoke(null, [Configuration.WorldRadius, Configuration.WorldTotalRadius, Configuration.WorldStretch, Configuration.BiomeStretch]);
  }
}
