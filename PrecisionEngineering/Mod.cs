using CitiesHarmony.API;
using ICities;

namespace PrecisionEngineering
{
    public class Mod : IUserMod
    {
        public string Name => "Precision Engineering (Harmony)";

        public string Description => "Build with precision. Hold CTRL to enable angle snapping, SHIFT to show more information, ALT to snap to guide-lines.";

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

        public void OnSettingsUI(UIHelperBase helper)
        {
            var group = helper.AddGroup("UI");
            group.AddDropdown("Font Size", new [] {"Normal", "Large", "X-Large"}, ModSettings.FontSize,
                OnFontSizeChanged);

            group.AddDropdown("Measurement Unit", new [] {"Metric", "Imperial"}, (int)ModSettings.Unit,
                OnMeasurementUnitChanged);

            group.AddDropdown("Road Length Position", new [] {"Middle", "End"}, (int)ModSettings.RoadLengthPosition,
                OnRoadLengthPositionChanged);
        }

        private void OnMeasurementUnitChanged(int sel)
        {
            ModSettings.Unit = (ModSettings.Units) sel;
        }

        private void OnFontSizeChanged(int val)
        {
            ModSettings.FontSize = val;
        }

        private void OnRoadLengthPositionChanged(int val)
        {
            ModSettings.RoadLengthPosition = (ModSettings.RoadLengthPositions)val;
        }
    }
}
