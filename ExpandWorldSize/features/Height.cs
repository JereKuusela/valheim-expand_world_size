namespace ExpandWorldSize;

// Postfixes that scale the generated terrain, applied by Patcher.
public class BaseHeight
{
  public static float Postfix(float result) => WorldInfo.BaseWaterLevel + (result - WorldInfo.BaseWaterLevel) * WorldInfo.AltitudeMultiplier + WorldInfo.BaseAltitudeDelta;
}
public class BiomeHeight
{
  public static float Postfix(float result) => result > WorldInfo.WaterLevel ? result : (result - WorldInfo.WaterLevel) * WorldInfo.WaterDepth + WorldInfo.WaterLevel;
}
public class Forest
{
  public static float Postfix(float result) => result / WorldInfo.ForestMultiplier;
}
