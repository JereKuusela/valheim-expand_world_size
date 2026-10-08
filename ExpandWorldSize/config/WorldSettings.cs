using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using BepInEx.Configuration;
using Common;
using HarmonyLib;
using Service;

namespace ExpandWorldSize;

// Settings are stored in the world file as hidden global keys ("ews_<setting> <value>").
// They are removed from the world when it's loaded and only added to the written file, so vanilla never sees them.
public static class WorldSettings
{
  private const string Prefix = "ews_";
  private const string GridWidthKey = Prefix + "gridwidth";
  // Worlds without an entry are older than this feature and use whatever is in the config.
  private static readonly ConditionalWeakTable<World, Dictionary<string, string>> Stored = new();
  // Saving happens on a background thread.
  [ThreadStatic]
  private static List<string>? SaveKeys;

  private static string KeyOf(ConfigEntryBase entry) => Prefix + entry.Definition.Key.ToLowerInvariant().Replace(' ', '_');
  private static string DefaultOf(ConfigEntryBase entry) => TomlTypeConverter.ConvertToString(entry.DefaultValue, entry.SettingType);

  private static Dictionary<string, string> Snapshot()
  {
    Dictionary<string, string> values = [];
    foreach (var entry in ConfigWrapper.Entries)
    {
      var value = entry.GetSerializedValue();
      if (value != DefaultOf(entry)) values[KeyOf(entry)] = value;
    }
    return values;
  }

  public static void Extract(World world)
  {
    var keys = world.m_startingGlobalKeys;
    Dictionary<string, string>? values = null;
    for (var i = keys.Count - 1; i >= 0; i--)
    {
      var key = keys[i];
      if (!key.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase)) continue;
      values ??= [];
      var space = key.IndexOf(' ');
      if (space < 0) values[key.ToLowerInvariant()] = "";
      else values[key[..space].ToLowerInvariant()] = key[(space + 1)..];
      keys.RemoveAt(i);
    }
    if (values != null) Stored.AddOrUpdate(world, values);
  }

  public static void Apply(World world)
  {
    if (!Stored.TryGetValue(world, out var values)) return;
    foreach (var entry in ConfigWrapper.Entries)
    {
      var value = values.TryGetValue(KeyOf(entry), out var stored) ? stored : DefaultOf(entry);
      if (entry.GetSerializedValue() != value) entry.SetSerializedValue(value);
    }
    // The world load applies patches and generates with the loaded values.
    Refresh.Clear();
  }

  // Grid width of the chunks that are on the disk. Unlisted means the vanilla layout.
  public static int SavedGridWidth(World world)
  {
    if (Stored.TryGetValue(world, out var values) && values.TryGetValue(GridWidthKey, out var value)
      && int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var width))
      return width;
    return ZoneGrid.VanillaWidth;
  }

  public static void PrepareSave(World world)
  {
    var active = ZNet.GetWorldIfIsHost() == world;
    if (active || !Stored.TryGetValue(world, out var values))
    {
      values = Snapshot();
      // Menu saves don't know the layout of the chunks on the disk.
      if (active) values[GridWidthKey] = ZoneGrid.Width.ToString(CultureInfo.InvariantCulture);
      Stored.AddOrUpdate(world, values);
    }
    SaveKeys = [.. world.m_startingGlobalKeys];
    foreach (var kvp in values)
      SaveKeys.Add($"{kvp.Key} {kvp.Value}");
  }

  public static void EndSave() => SaveKeys = null;
  public static List<string> GetSaveKeys(World world) => SaveKeys ?? world.m_startingGlobalKeys;
}

[HarmonyPatch(typeof(World), nameof(World.LoadWorld))]
public class LoadWorldSettings
{
  static void Postfix(World? __result)
  {
    if (__result != null) WorldSettings.Extract(__result);
  }
}

[HarmonyPatch]
public class SaveWorldSettings
{
  static MethodBase TargetMethod() => AccessTools.Method(typeof(World), nameof(World.SaveWorldFWLData), [typeof(DateTime), typeof(FileWriter).MakeByRefType()]);
  static void Prefix(World __instance) => WorldSettings.PrepareSave(__instance);
  static void Finalizer() => WorldSettings.EndSave();

  // bw.Write(this.m_startingGlobalKeys.Count) and the loop reading the list.
  static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
  {
    var field = AccessTools.Field(typeof(World), nameof(World.m_startingGlobalKeys));
    var method = AccessTools.Method(typeof(WorldSettings), nameof(WorldSettings.GetSaveKeys));
    return new CodeMatcher(instructions)
      .MatchStartForward(new CodeMatch(OpCodes.Ldfld, field))
      .Repeat(matcher => matcher.Set(OpCodes.Call, method))
      .InstructionEnumeration();
  }
}
