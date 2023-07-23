using System.Reflection;
using BepInEx.Bootstrap;
using HarmonyLib;

namespace ExpandWorldSize;

public class ExpandWorld
{
  public const string GUID = "expand_world";
  private static Assembly? ExpandWorldAssembly;
  private static MethodInfo? SetSize;
  public static void Run()
  {
    if (!Chainloader.PluginInfos.TryGetValue(GUID, out var info)) return;
    ExpandWorldAssembly = info.Instance.GetType().Assembly;
    var type = ExpandWorldAssembly.GetType("ExpandWorld.World");
    if (type == null) return;
    SetSize = AccessTools.Method(type, "Set");
    if (SetSize == null) return;
    EWS.Log.LogInfo("\"Expand World\" detected. Applying compatibility.");
  }

  public static void RefreshSize()
  {
    if (SetSize == null) return;
    SetSize.Invoke(null, new object[] { World.WaterLevel, Configuration.WorldRadius, Configuration.WorldTotalRadius, Configuration.WorldStretch, Configuration.BiomeStretch });
  }
}
