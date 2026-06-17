using BepInEx.Bootstrap;

namespace ExpandWorldSize;

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
		}
		return _present;
	}
}