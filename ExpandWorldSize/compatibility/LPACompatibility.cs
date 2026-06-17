// v0.0.1
using BepInEx.Bootstrap;
using Service;

namespace ExpandWorldSize;

/**
 * Detects Location Placement Accelerator (LPA). When LPA is loaded, EWS
 * defers minimap generation to LPA's parallel implementation, which uses
 * Environment.ProcessorCount - 2 workers vs EWS's single thread.
 *
 * LPA reads m_textureSize and m_pixelSize directly from the Minimap instance at generation time. 
 * EWS's MinimapAwake.Postfix has already mutated those instance fields from Configuration.MapSize
 * and Configuration.MapPixelSize before any generation runs, so the EWS pixel-size and texture-size settings are honored regardless of who
 * performs the gen. All good here.
 *
 * Priority when both BC and LPA are present: BC wins because that is how I had designed LPA initially. BC takes full ownership 
 * of the world (including a custom pixel-size flow tied to its in-game preset switching), and LPA itself defers to BC. EWS therefore checks BC
 * first and LPA second. 
 *
 * Detection is lazy on first call to IsEnabled() so this file is drop-in without requiring any change to ExpandWorldSize.cs.
 */
public class LPACompatibility
{
    public const string GUID = "nickpappas.locationplacementaccelerator";
    private static bool _checked;
    private static bool _present;

    public static bool IsEnabled()
    {
        if (!_checked)
        {
            _checked = true;
            _present = Chainloader.PluginInfos.ContainsKey(GUID);
            if (_present)
                Log.Info("\"Location Placement Accelerator\" detected. Deferring minimap generation to LPA.");
        }
        return _present;
    }
}