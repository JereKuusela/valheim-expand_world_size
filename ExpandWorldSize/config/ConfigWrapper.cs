using System;
using System.Collections.Generic;
using System.Globalization;
using BepInEx.Configuration;
using ServerSync;

namespace Service;

public class ConfigWrapper
{

  private readonly ConfigFile ConfigFile;
  private readonly ConfigSync ConfigSync;
  public readonly Action Regenerate;
  public ConfigWrapper(ConfigFile configFile, ConfigSync configSync, Action regenerate)
  {
    ConfigFile = configFile;
    ConfigSync = configSync;
    Regenerate = regenerate;
  }
  public static Dictionary<ConfigEntry<string>, float?> Floats = new();
  public static Dictionary<ConfigEntry<string>, int?> Ints = new();

  public ConfigEntry<string> BindFloat(string group, string name, float value, bool regenerate, string description = "", bool synchronizedSetting = true)
  {
    var entry = Bind(group, name, value.ToString(CultureInfo.InvariantCulture), regenerate, description, synchronizedSetting);
    entry.SettingChanged += (s, e) => Floats[entry] = TryParseFloat(entry);
    Floats[entry] = TryParseFloat(entry);
    return entry;
  }
  public ConfigEntry<string> BindFloat(string group, string name, float? value, bool regenerate, string description = "", bool synchronizedSetting = true)
  {
    var entry = Bind(group, name, value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : "", regenerate, description, synchronizedSetting);
    entry.SettingChanged += (s, e) => Floats[entry] = TryParseFloat(entry);
    Floats[entry] = TryParseFloat(entry);
    return entry;
  }

  public ConfigEntry<T> Bind<T>(string group, string name, T value, bool regenerate, ConfigDescription description, bool synchronizedSetting = true)
  {
    var configEntry = ConfigFile.Bind(group, name, value, description);
    if (regenerate)
      configEntry.SettingChanged += Regen;
    var syncedConfigEntry = ConfigSync.AddConfigEntry(configEntry);
    syncedConfigEntry.SynchronizedConfig = synchronizedSetting;
    return configEntry;
  }
  public ConfigEntry<T> Bind<T>(string group, string name, T value, bool regenerate, string description = "", bool synchronizedSetting = true) => Bind(group, name, value, regenerate, new ConfigDescription(description), synchronizedSetting);
  private void Regen(object e, EventArgs s) => Regenerate();

  private static float? TryParseFloat(string value)
  {
    if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result)) return result;
    return null;
  }
  private static float? TryParseFloat(ConfigEntry<string> setting)
  {
    if (float.TryParse(setting.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result)) return result;
    return TryParseFloat((string)setting.DefaultValue);
  }
}
