using System.Collections.Generic;
using System.Globalization;
using BepInEx.Configuration;
using ExpandWorldSize;
using ServerSync;

namespace Service;

public class ConfigWrapper(ConfigFile configFile, ConfigSync configSync)
{

  private readonly ConfigFile ConfigFile = configFile;
  private readonly ConfigSync ConfigSync = configSync;
  public static Dictionary<ConfigEntry<string>, float> Floats = [];
  public static Dictionary<ConfigEntry<string>, float?> NullFloats = [];
  public static Dictionary<ConfigEntry<string>, int> Ints = [];
  public static readonly List<ConfigEntryBase> Entries = [];

  public ConfigEntry<string> BindFloat(string group, string name, float value, Regen regenerate, string description = "", bool synchronizedSetting = true)
  {
    var entry = Bind(group, name, value.ToString(CultureInfo.InvariantCulture), Regen.None, description, synchronizedSetting);
    entry.SettingChanged += (s, e) => Floats[entry] = TryParseFloat(entry) ?? value;
    Floats[entry] = TryParseFloat(entry) ?? value;
    // After the cached value so the regeneration sees the new value.
    AddRegenerate(entry, regenerate);
    return entry;
  }
  public ConfigEntry<string> BindFloat(string group, string name, float? value, Regen regenerate, string description = "", bool synchronizedSetting = true)
  {
    var entry = Bind(group, name, value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : "", Regen.None, description, synchronizedSetting);
    entry.SettingChanged += (s, e) => NullFloats[entry] = TryParseFloat(entry);
    NullFloats[entry] = TryParseFloat(entry);
    AddRegenerate(entry, regenerate);
    return entry;
  }

  public ConfigEntry<T> Bind<T>(string group, string name, T value, Regen regenerate, ConfigDescription description, bool synchronizedSetting = true)
  {
    var configEntry = ConfigFile.Bind(group, name, value, description);
    Entries.Add(configEntry);
    AddRegenerate(configEntry, regenerate);
    var syncedConfigEntry = ConfigSync.AddConfigEntry(configEntry);
    syncedConfigEntry.SynchronizedConfig = synchronizedSetting;
    return configEntry;
  }
  public ConfigEntry<T> Bind<T>(string group, string name, T value, Regen regenerate, string description = "", bool synchronizedSetting = true) => Bind(group, name, value, regenerate, new ConfigDescription(description), synchronizedSetting);
  private static void AddRegenerate<T>(ConfigEntry<T> entry, Regen regenerate)
  {
    if (regenerate != Regen.None) entry.SettingChanged += (e, s) => Regenerator.Request(regenerate);
  }

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
