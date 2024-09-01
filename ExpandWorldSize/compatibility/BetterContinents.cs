using System.Reflection;
using BepInEx.Bootstrap;
using HarmonyLib;
using Service;

namespace ExpandWorldSize;

public class BetterContinents
{
  public const string GUID = "BetterContinents";
  private static Assembly? Assembly;
  private static FieldInfo? SettingsField;
  private static FieldInfo? IsEnabledField;
  private static MethodInfo? SetSizeMethod;
  private static FieldInfo? WorldSizeField;
  private static FieldInfo? EdgeSizeField;
  public static void Run()
  {
    if (!Chainloader.PluginInfos.TryGetValue(GUID, out var info)) return;
    Assembly = info.Instance.GetType().Assembly;
    var type = Assembly.GetType("BetterContinents.BetterContinents");
    if (type == null) return;
    SettingsField = AccessTools.Field(type, "Settings");
    if (SettingsField == null) return;
    type = Assembly.GetType("BetterContinents.BetterContinents+BetterContinentsSettings");
    IsEnabledField = AccessTools.Field(type, "EnabledForThisWorld");
    if (IsEnabledField == null) return;
    type = Assembly.GetType("BetterContinents.BetterContinents");
    if (type == null) return;
    SetSizeMethod = AccessTools.Method(type, "SetSize");
    if (SetSizeMethod != null)
    {
      Log.Info("\"Better Continents\" detected. Applying compatibility.");
      return;
    }
    WorldSizeField = AccessTools.Field(type, "WorldSize");
    EdgeSizeField = AccessTools.Field(type, "EdgeSize");
    if (EdgeSizeField != null && SetSizeMethod != null)
      Log.Info("Older \"Better Continents\" detected. Applying compatibility.");
  }

  public static bool IsEnabled()
  {
    if (SettingsField == null) return false;
    if (IsEnabledField == null) return false;
    var settings = SettingsField.GetValue(null);
    if (settings == null) return false;
    return (bool)IsEnabledField.GetValue(settings);
  }
  public static void RefreshSize()
  {
    SetSizeMethod?.Invoke(null, [Configuration.WorldRadius, Configuration.WorldEdgeSize]);
    WorldSizeField?.SetValue(null, Configuration.WorldTotalRadius);
    EdgeSizeField?.SetValue(null, Configuration.WorldEdgeSize);
  }
}
