using ColossalFramework;
using HarmonyLib;

namespace PedestrianStreetServices.Patches
{
    /// <summary>
    /// Bypasses the service point proxy system for buildings that are on
    /// pedestrian streets but NOT inside an actual pedestrian zone district.
    /// This forces the game to dispatch service vehicles directly to the
    /// building instead of routing through the service point intermediary.
    /// </summary>
    [HarmonyPatch(typeof(CommonBuildingAI), nameof(CommonBuildingAI.GetUseServicePoint))]
    internal static class CommonBuildingAI_GetUseServicePoint
    {
        [HarmonyPostfix]
        static void Postfix(ushort buildingID, ref Building data, ref bool __result)
        {
            if (!__result)
                return;

            var districtManager = Singleton<DistrictManager>.instance;
            var park = districtManager.GetPark(data.m_position);

            // If the building is inside a pedestrian zone district, leave it alone.
            // The service point system will handle it as intended.
            if (park != 0 && districtManager.m_parks.m_buffer[park].IsPedestrianZone)
                return;

            // Building is NOT in a pedestrian zone — bypass the service point proxy
            // so that service vehicles are dispatched directly.
            __result = false;
        }
    }
}
