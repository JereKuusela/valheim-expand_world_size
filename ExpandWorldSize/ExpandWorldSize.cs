using System;
using System.IO;
using BepInEx;
using HarmonyLib;
using Service;
using UnityEngine;

namespace ExpandWorldSize;

[BepInPlugin(GUID, NAME, VERSION)]
[BepInIncompatibility("expand_world")]
public class EWS : BaseUnityPlugin
{
  public const string GUID = "expand_world_size";
  public const string NAME = "Expand World Size";
  public const string VERSION = "1.29";
#nullable disable
  public static EWS Instance;
#nullable enable
  public static ServerSync.ConfigSync ConfigSync = new(GUID)
  {
    DisplayName = NAME,
    CurrentVersion = VERSION,
    ModRequired = true,
    IsLocked = true
  };
  public static string ConfigName = $"{GUID}.cfg";
  public static bool NeedsMigration = File.Exists(Path.Combine(Paths.ConfigPath, "expand_world.cfg")) && !File.Exists(Path.Combine(Paths.ConfigPath, ConfigName));
  public void Awake()
  {
    Log.Init(Logger);
    Instance = this;
    ConfigWrapper wrapper = new(Config, ConfigSync, InvokeRegenerate);
    Configuration.Init(wrapper);
    // Two patchers are needed until all patches are properly dynamic.
    Harmony harmony = new(GUID);
    Harmony dynamicHarmony = new(GUID + ".dynamic");
    Patcher.Init(harmony, dynamicHarmony);
    try
    {
      SetupWatcher();
    }
    catch (Exception e)
    {
      Log.Error(e.Message);
    }
  }
  public void Start()
  {
    Marketplace.Run();
    BetterContinents.Run();
    EWD.Run();
  }
  public void InvokeRegenerate()
  {
    if (Patcher.IsMenu) return;
    // Debounced for smooth config editing.
    CancelInvoke("Regenerate");
    Invoke("Regenerate", 1.0f);
  }
  public void LateUpdate()
  {
    if (Patcher.IsMenu) return;
    WaterColor.Transition(Time.deltaTime);
  }

  public void Regenerate() => WorldInfo.Generate();
#pragma warning disable IDE0051
  private void OnDestroy()
  {
    Config.Save();
  }
#pragma warning restore IDE0051

  private void SetupWatcher()
  {
    FileSystemWatcher watcher = new(Paths.ConfigPath, ConfigName);
    watcher.Changed += ReadConfigValues;
    watcher.Created += ReadConfigValues;
    watcher.Renamed += ReadConfigValues;
    watcher.IncludeSubdirectories = true;
    watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
    watcher.EnableRaisingEvents = true;
  }
  private void ReadConfigValues(object sender, FileSystemEventArgs e)
  {
    if (!File.Exists(Config.ConfigFilePath)) return;
    try
    {
      Log.Debug("ReadConfigValues called");
      Config.Reload();
    }
    catch
    {
      Log.Error($"There was an issue loading your {Config.ConfigFilePath}");
      Log.Error("Please check your config entries for spelling and format!");
    }
  }

}

[HarmonyPatch(typeof(ZRpc), nameof(ZRpc.SetLongTimeout))]
public class IncreaseTimeout
{
  static bool Prefix()
  {
    ZRpc.m_timeout = 300f;
    ZLog.Log(string.Format("ZRpc timeout set to {0}s ", ZRpc.m_timeout));
    return false;
  }
}
