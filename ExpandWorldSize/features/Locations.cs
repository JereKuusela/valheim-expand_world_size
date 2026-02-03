using System.Collections.Generic;
using System.Diagnostics;
using HarmonyLib;
using UnityEngine;

namespace ExpandWorldSize;

[HarmonyPatch(typeof(LoadingIndicator), nameof(LoadingIndicator.SetShowProgress))] 
public class ModifyLocations
{
  private static readonly Dictionary<ZoneSystem.ZoneLocation, int> OriginalQuantities = [];
  private static readonly Dictionary<ZoneSystem.ZoneLocation, float> OriginalMin = [];
  private static readonly Dictionary<ZoneSystem.ZoneLocation, float> OriginalMax = [];

    static void Prefix(bool show) // rename 'visible' to 'show' to match the game's new parameter name
    {
    if (!show) return;
    if (Configuration.LocationsMultiplier != 1f)
    {
      foreach (var location in ZoneSystem.instance.m_locations)
      {
        if (location.m_prefabName == Game.instance.m_StartLocation) continue;
        OriginalQuantities[location] = location.m_quantity;
        location.m_quantity = Mathf.RoundToInt(location.m_quantity * Configuration.LocationsMultiplier);
      }
    }
    if (Configuration.WorldRadius != 10000f)
    {
      foreach (var location in ZoneSystem.instance.m_locations)
      {
        OriginalMin[location] = location.m_minDistance;
        OriginalMax[location] = location.m_maxDistance;
        location.m_minDistance *= Configuration.WorldRadius / 10000f;
        location.m_maxDistance *= Configuration.WorldRadius / 10000f;
      }
    }
  }
  static void Postfix(bool show)//again... rename 'visible' to 'show' to match the game's new parameter name
    {
    if (show) return;
    foreach (var location in ZoneSystem.instance.m_locations)
    {
      if (OriginalQuantities.TryGetValue(location, out var quantity))
        location.m_quantity = quantity;
      if (OriginalMin.TryGetValue(location, out var min))
        location.m_minDistance = min;
      if (OriginalMax.TryGetValue(location, out var max))
        location.m_maxDistance = max;
    }
    OriginalQuantities.Clear();
    OriginalMin.Clear();
    OriginalMax.Clear();
  }
}

[HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.GetRandomZone))]
public class GetRandomZone
{
  static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
  {
    CodeMatcher matcher = new(instructions);
    matcher = Helper.Replace(matcher, 10000f, Configuration.WorldRadius);
    return matcher.InstructionEnumeration();
  }
}
[HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.GenerateLocationsTimeSliced), typeof(ZoneSystem.ZoneLocation), typeof(Stopwatch), typeof(ZPackage))]
[HarmonyPatch(MethodType.Enumerator)]
public class GenerateLocations
{
  [HarmonyTranspiler]
  static IEnumerable<CodeInstruction> TranspileMoveNext(IEnumerable<CodeInstruction> instructions)
  {
    CodeMatcher matcher = new(instructions);
    matcher = Helper.Replace(matcher, 10000f, Configuration.WorldRadius);
    return matcher.InstructionEnumeration();
  }
}
