using System;
using System.IO;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Service;
using UnityEngine;

namespace ExpandWorldSize;
[BepInPlugin(GUID, NAME, VERSION)]
public class EWS : BaseUnityPlugin
{
  public const string GUID = "expand_world_size";
  public const string NAME = "Expand World Size";
  public const string VERSION = "1.0";
#nullable disable
  public static ManualLogSource Log;
#nullable enable
  public static ServerSync.ConfigSync ConfigSync = new(GUID)
  {
    DisplayName = NAME,
    CurrentVersion = VERSION,
    ModRequired = true,
    IsLocked = true
  };
  public static string ConfigName = $"{GUID}.cfg";
  public void Awake()
  {
    Log = Logger;
    ConfigWrapper wrapper = new(Config, ConfigSync, InvokeRegenerate);
    Configuration.Init(wrapper);
    Harmony harmony = new(GUID);
    harmony.PatchAll();
    try
    {
      SetupWatcher();
    }
    catch (Exception e)
    {
      Log.LogError(e);
    }
    CancelRegenerate = () =>
    {
      Generate.Cancel();
      CancelInvoke("Regenerate");
    };
  }
  public void Start()
  {
    Marketplace.Run();
    BetterContinents.Run();
    ExpandWorld.Run();
  }
  public void InvokeRegenerate()
  {
    // Debounced for smooth config editing.
    CancelInvoke("Regenerate");
    Invoke("Regenerate", 1.0f);
  }
  public static Action CancelRegenerate = () => { };
  public void Regenerate() => Generate.World();
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
      Log.LogDebug("ReadConfigValues called");
      Config.Reload();
    }
    catch
    {
      Log.LogError($"There was an issue loading your {Config.ConfigFilePath}");
      Log.LogError("Please check your config entries for spelling and format!");
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
