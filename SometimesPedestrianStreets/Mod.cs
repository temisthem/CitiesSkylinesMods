using CitiesHarmony.API;
using ICities;

namespace SometimesPedestrianStreets
{
    public class Mod : IUserMod
    {
        public string Name => "Sometimes Pedestrian Streets";

        public string Description =>
            "Allows buildings to be placed on pedestrian streets without requiring a pedestrian zone, " +
            "and enables service vehicles (garbage, cargo, mail, etc.) to drive on pedestrian streets.";

        public void OnEnabled()
        {
            HarmonyHelper.EnsureHarmonyInstalled();
        }

        public void OnDisabled()
        {
            if (HarmonyHelper.IsHarmonyInstalled)
            {
                Patches.Patcher.UnpatchAll();
            }
        }
    }
}
