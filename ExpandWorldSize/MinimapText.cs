using HarmonyLib;
namespace ExpandWorldSize;

[HarmonyPatch(typeof(Minimap), nameof(Minimap.Awake))]
public class Minimap_Alignment
{
  static void Postfix(Minimap __instance)
  {
    // Removes the need of padding.
    __instance.m_biomeNameSmall.alignment = TMPro.TextAlignmentOptions.TopRight;
    __instance.m_biomeNameLarge.alignment = TMPro.TextAlignmentOptions.TopRight;
  }
}

[HarmonyPatch(typeof(Minimap), nameof(Minimap.UpdateBiome))]
public class Minimap_ShowLoading
{
  private static void AddText(TMPro.TMP_Text input, string text)
  {
    if (input.text.Contains(text)) return;
    input.text += text;
  }
  private static void CleanUp(TMPro.TMP_Text input, string text)
  {
    if (text == "" || !input.text.Contains(text)) return;
    input.text = input.text.Replace(text, "");
  }
  private static string GetText() => "\nLoading..";
  private static string PreviousSmallText = "";
  private static string PreviousLargeText = "";
  static void Postfix(Minimap __instance)
  {
    var mode = __instance.m_mode;
    if (PreviousSmallText != "")
    {
      CleanUp(__instance.m_biomeNameSmall, PreviousSmallText);
      PreviousSmallText = "";
    }
    if (PreviousLargeText != "")
    {
      CleanUp(__instance.m_biomeNameLarge, PreviousLargeText);
      PreviousLargeText = "";
    }
    if (mode == Minimap.MapMode.Small && MapGeneration.Generating)
    {
      var text = GetText();
      AddText(__instance.m_biomeNameSmall, text);
      PreviousSmallText = text;
    }
    if (mode == Minimap.MapMode.Large && MapGeneration.Generating)
    {
      var text = GetText();
      AddText(__instance.m_biomeNameLarge, text);
      PreviousLargeText = text;
    }
  }
}
