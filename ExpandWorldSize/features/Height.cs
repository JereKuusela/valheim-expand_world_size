using HarmonyLib;

namespace ExpandWorldSize;


[HarmonyPatch(typeof(WorldGenerator), nameof(WorldGenerator.GetBaseHeight))]
public class BaseHeight
{
  public static float Postfix(float result) => WorldInfo.BaseWaterLevel + (result - WorldInfo.BaseWaterLevel) * WorldInfo.AltitudeMultiplier + WorldInfo.BaseAltitudeDelta;
}
[HarmonyPatch(typeof(WorldGenerator), nameof(WorldGenerator.GetBiomeHeight))]
public class BiomeHeight
{
  public static float Postfix(float result) => result > WorldInfo.WaterLevel ? result : (result - WorldInfo.WaterLevel) * WorldInfo.WaterDepth + WorldInfo.WaterLevel;
}
