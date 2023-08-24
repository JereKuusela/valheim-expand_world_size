
using HarmonyLib;

namespace ExpandWorldSize;

[HarmonyPatch(typeof(WorldGenerator), nameof(WorldGenerator.GetForestFactor))]
public class Forest
{
  public static float Postfix(float result) => result /= WorldInfo.ForestMultiplier;
}
