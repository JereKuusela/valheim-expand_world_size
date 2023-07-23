

namespace ExpandWorldSize;

public class World
{

  public static float WaterLevel = 30;

  public static void Set(float waterLevel)
  {
    if (waterLevel == WaterLevel) return;
    WaterLevel = waterLevel;
    Generate.World();
  }
}