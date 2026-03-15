using HarmonyLib;

namespace SometimesPedestrianStreets.Patches
{
    /// <summary>
    /// Removes the "must be in a pedestrian zone" placement restriction
    /// from service point buildings (cafes, shops, etc.) so they can be
    /// placed on pedestrian streets without a pedestrian zone district.
    /// </summary>
    [HarmonyPatch(typeof(ServicePointAI), nameof(ServicePointAI.CheckBuildPosition))]
    internal static class ServicePointAI_CheckBuildPosition
    {
        [HarmonyPostfix]
        static void Postfix(ref ToolBase.ToolErrors __result)
        {
            __result &= ~(ToolBase.ToolErrors.PedestrianZoneRequired | ToolBase.ToolErrors.MainPedestrianZoneRequired);
        }
    }
}
