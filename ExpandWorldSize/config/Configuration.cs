using BepInEx.Configuration;
using HarmonyLib;
using Service;
using UnityEngine;

namespace ExpandWorldSize;

public partial class Configuration
{
#nullable disable

  public static ConfigEntry<bool> configRegenerateMap;
  public static bool RegenerateMap => configRegenerateMap.Value;
  public static ConfigEntry<string> configWorldRadius;
  public static float WorldRadius => ConfigWrapper.Floats[configWorldRadius];
  public static float StrechedWorldRadius => WorldRadius / WorldStretch;
  public static ConfigEntry<string> configWorldEdgeSize;
  public static float WorldEdgeSize => ConfigWrapper.Floats[configWorldEdgeSize];
  public static float WorldTotalRadius => WorldRadius + WorldEdgeSize;
  public static float StrechedWorldTotalRadius => WorldTotalRadius / WorldStretch;
  public static ConfigEntry<string> configMapSize;
  public static float MapSize => ConfigWrapper.Floats[configMapSize];
  public static ConfigEntry<string> configMapPixelSize;
  public static float MapPixelSize => ConfigWrapper.Floats[configMapPixelSize];

  public static ConfigEntry<string> configAltitudeMultiplier;
  public static float AltitudeMultiplier => ConfigWrapper.Floats[configAltitudeMultiplier];
  public static ConfigEntry<string> configAltitudeDelta;
  public static float AltitudeDelta => ConfigWrapper.Floats[configAltitudeDelta];

  public static ConfigEntry<string> configLocationsMultiplier;
  public static float LocationsMultiplier => ConfigWrapper.Floats[configLocationsMultiplier];

  public static ConfigEntry<string> configForestMultiplier;
  public static float ForestMultiplier => ConfigWrapper.Floats[configForestMultiplier];
  public static ConfigEntry<string> configWorldStretch;
  public static ConfigEntry<string> configBiomeStretch;
  public static float WorldStretch => ConfigWrapper.Floats[configWorldStretch];
  public static float BiomeStretch => ConfigWrapper.Floats[configBiomeStretch];


  public static ConfigEntry<string> configSeed;
  public static string Seed => configSeed.Value;

  public static ConfigEntry<string> configOffsetX;
  public static float? OffsetX => ConfigWrapper.NullFloats[configOffsetX];
  public static ConfigEntry<string> configOffsetY;
  public static float? OffsetY => ConfigWrapper.NullFloats[configOffsetY];
  public static ConfigEntry<string> configHeightSeed;
  public static float? HeightSeed => ConfigWrapper.NullFloats[configHeightSeed];
  public static ConfigEntry<string> configWaterDepthMultiplier;
  public static float WaterDepthMultiplier => ConfigWrapper.Floats[configWaterDepthMultiplier];

  public static ConfigEntry<string> configAshlandsWidthRestriction;
  public static double AshlandsWidthRestriction => RestrictAshlands ? ConfigWrapper.Floats[configAshlandsWidthRestriction] : double.PositiveInfinity;
  public static ConfigEntry<string> configAshlandsLengthRestriction;
  public static double AshlandsLengthRestriction => RestrictAshlands ? ConfigWrapper.Floats[configAshlandsLengthRestriction] : double.PositiveInfinity;

  public static ConfigEntry<bool> configRestrictAshlands;
  public static bool RestrictAshlands => configRestrictAshlands.Value;
  public static ConfigEntry<bool> configAshlandsGap;
  public static bool AshlandsGap => configAshlandsGap.Value;
  public static ConfigEntry<bool> configDeepNorthGap;
  public static bool DeepNorthGap => configDeepNorthGap.Value;
  public static ConfigEntry<bool> configFixWaterColor;
  public static bool FixWaterColor => configFixWaterColor.Value;
  public static ConfigEntry<bool> configRemoveAshlandsWater;
  public static bool RemoveAshlandsWater => configRemoveAshlandsWater.Value;
#nullable enable
  public static void Init(ConfigWrapper wrapper)
  {
    var section = "1. General";
    configRegenerateMap = wrapper.Bind(section, "Regenerate map", true, false, "If true, the world map is regenerated automatically on data changes.");
    configWorldRadius = wrapper.BindFloat(section, "World radius", 10000f, true, "Radius of the world in meters (excluding the edge).");
    configWorldEdgeSize = wrapper.BindFloat(section, "World edge size", 500f, true, "Size of the edge area in meters (added to the radius for the total size).");
    configMapSize = wrapper.BindFloat(section, "Minimap size", 1f, false, "Increases the minimap size, but also significantly increases the generation time.");
    configMapSize.SettingChanged += (e, s) =>
    {
      if (!Minimap.instance) return;
      var newValue = (int)(MinimapAwake.OriginalTextureSize * MapSize);
      if (newValue == Minimap.instance.m_textureSize) return;
      Minimap.instance.m_maxZoom = MinimapAwake.OriginalMinZoom * Mathf.Max(1f, MapSize);
      MapGeneration.UpdateTextureSize(Minimap.instance, newValue);
      wrapper.Regenerate();
    };
    configMapPixelSize = wrapper.BindFloat(section, "Minimap pixel size", 1f, false, "Decreases the minimap detail, but doesn't affect the generation time.");
    configMapPixelSize.SettingChanged += (e, s) =>
    {
      if (!Minimap.instance) return;
      var newValue = MinimapAwake.OriginalPixelSize * MapPixelSize;
      if (newValue == Minimap.instance.m_pixelSize) return;
      Minimap.instance.m_pixelSize = newValue;
      wrapper.Regenerate();
    };
    configWorldStretch = wrapper.BindFloat(section, "Stretch world", 1f, true, "Stretches the world to a bigger area.");
    configBiomeStretch = wrapper.BindFloat(section, "Stretch biomes", 1f, true, "Stretches the biomes to a bigger area.");

    configForestMultiplier = wrapper.BindFloat(section, "Forest multiplier", 1f, true, "Multiplies the amount of forest.");
    configAltitudeMultiplier = wrapper.BindFloat(section, "Altitude multiplier", 1f, true, "Multiplies the altitude.");
    configAltitudeDelta = wrapper.BindFloat(section, "Altitude delta", 0f, true, "Adds to the altitude.");
    configWaterDepthMultiplier = wrapper.BindFloat(section, "Water depth multiplier", 1f, true, "Multplies the water depth.");
    configLocationsMultiplier = wrapper.BindFloat(section, "Locations", 1f, true, "Multiplies the max amount of locations.");
    configOffsetX = wrapper.BindFloat(section, "Offset X", null, true);
    configOffsetY = wrapper.BindFloat(section, "Offset Y", null, true);
    configSeed = wrapper.Bind(section, "Seed", "", false);
    configSeed.SettingChanged += (s, e) =>
    {
      if (Seed == "") return;
      if (WorldGenerator.instance == null) return;
      var world = WorldGenerator.instance.m_world;
      if (world.m_menu) return;
      world.m_seedName = Seed;
      world.m_seed = Seed.GetStableHashCode();
      // Prevents default generate (better use the debounced).
      world.m_menu = true;
      WorldGenerator.Initialize(world);
      world.m_menu = false;
      WorldInfo.Generate();
    };
    configHeightSeed = wrapper.BindFloat(section, "Height variation seed", null, true);


    section = "2. Poles";
    configRestrictAshlands = wrapper.Bind(section, "Restrict Ashlands position", true, true, "If true, restricts Ashlands biome position.");
    configAshlandsWidthRestriction = wrapper.BindFloat(section, "Ashlands width restriction", 7500f, true, "How wide is the Ashlands biome (meters).");
    configAshlandsLengthRestriction = wrapper.BindFloat(section, "Ashlands length restriction", 1000f, true, "How long/deep is the Ashlands biome (meters).");
    configAshlandsGap = wrapper.Bind(section, "Ashlands gap", true, true, "If true, Ashlands biome has an Ocean gap above it.");
    configDeepNorthGap = wrapper.Bind(section, "Deep North gap", true, true, "If true, Deep North biome has an Ocean gap below it.");
    configRemoveAshlandsWater = wrapper.Bind(section, "Remove Ashlands water", false, false, "If true, the red water color is completely removed.");
    configRemoveAshlandsWater.SettingChanged += (s, e) => WaterColor.Regenerate();
    configFixWaterColor = wrapper.Bind(section, "Fix water color", true, false, "If true, fixes the water color to match the current biome.");
    configFixWaterColor.SettingChanged += (s, e) => WaterColor.Regenerate();
    WaterColor.Regenerate();
  }
}