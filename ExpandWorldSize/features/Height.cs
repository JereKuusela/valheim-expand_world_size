using HarmonyLib;

namespace ExpandWorldSize;


[HarmonyPatch(typeof(WorldGenerator), nameof(WorldGenerator.GetBaseHeight))]
public class BaseHeight
{
  public static float Postfix(float result) => World.BaseWaterLevel + (result - World.BaseWaterLevel) * World.AltitudeMultiplier + World.BaseAltitudeDelta;
}
[HarmonyPatch(typeof(WorldGenerator), nameof(WorldGenerator.GetBiomeHeight))]
public class BiomeHeight
{
  public static float Postfix(float result) => result > World.WaterLevel ? result : (result - World.WaterLevel) * World.WaterDepth + World.WaterLevel;
}
