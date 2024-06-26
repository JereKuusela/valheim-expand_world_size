using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace ExpandWorldSize;

// Patches here are not critical because called once per location entry (not on each attempt).
[HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.GenerateLocations), typeof(ZoneSystem.ZoneLocation))]
public class GenerateLocationsQuantity
{
  static void Prefix(ZoneSystem.ZoneLocation location, ref int __state)
  {
    __state = location.m_quantity;
    if (Configuration.LocationsMultiplier == 1f) return;
    if (location.m_prefabName == Game.instance.m_StartLocation) return;
    location.m_quantity = Mathf.RoundToInt(location.m_quantity * Configuration.LocationsMultiplier);
  }
  static void Postfix(ZoneSystem.ZoneLocation location, int __state)
  {
    location.m_quantity = __state;
  }
}
[HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.GenerateLocations), typeof(ZoneSystem.ZoneLocation))]
public class GenerateLocationsMin
{
  static void Prefix(ZoneSystem.ZoneLocation location, ref float __state)
  {
    __state = location.m_minDistance;
    location.m_minDistance *= Configuration.WorldRadius / 10000f;
  }
  static void Postfix(ZoneSystem.ZoneLocation location, float __state)
  {
    location.m_minDistance = __state;
  }
}
[HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.GenerateLocations), typeof(ZoneSystem.ZoneLocation))]
public class GenerateLocationsMax
{
  static void Prefix(ZoneSystem.ZoneLocation location, ref float __state)
  {
    __state = location.m_maxDistance;
    location.m_maxDistance *= Configuration.WorldRadius / 10000f;
  }
  static void Postfix(ZoneSystem.ZoneLocation location, float __state)
  {
    location.m_maxDistance = __state;
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
[HarmonyPatch(typeof(ZoneSystem), nameof(ZoneSystem.GenerateLocations), typeof(ZoneSystem.ZoneLocation))]
public class GenerateLocations
{
  static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
  {
    CodeMatcher matcher = new(instructions);
    matcher = Helper.Replace(matcher, 10000f, Configuration.WorldRadius);
    return matcher.InstructionEnumeration();
  }
}
