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
    [HarmonyPatch(typeof(BuildingAI), nameof(BuildingAI.GetUseServicePoint))]
    internal static class BuildingAI_GetUseServicePoint
    {
        [HarmonyPostfix]
        static void Postfix(ushort buildingAI, ref Building data, ref bool __result)
        {
            if (!__result)
                return;

            var districtManager = Singleton<DistrictManager>.instance;
            var park = districtManager.GetPark(data.m_position);

            if (park != 0 && districtManager.m_parks.m_buffer[park].IsPedestrianZone)
                return;

            __result = false;
        }
    }
}
