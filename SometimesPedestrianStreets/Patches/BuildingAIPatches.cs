using ColossalFramework;
using HarmonyLib;

namespace SometimesPedestrianStreets.Patches
{
    /// <summary>
    /// Bypasses the service point proxy system for buildings that are on
    /// pedestrian streets but NOT inside an actual pedestrian zone district.
    /// This forces the game to dispatch service vehicles directly to the
    /// building instead of routing through the service point intermediary.
    /// When overriding the result, clears stale service-point problem
    /// notifications that vanilla would otherwise never remove.
    /// </summary>
    [HarmonyPatch(typeof(BuildingAI), nameof(BuildingAI.GetUseServicePoint))]
    internal static class BuildingAI_GetUseServicePoint
    {
        [HarmonyPostfix]
        static void Postfix(ushort buildingAI, ref Building data, ref bool __result)
        {
            if (!__result)
                return;

            if (DistrictUtils.IsInPedestrianZone(data.m_position))
                return;

            __result = false;

            data.m_problems = Notification.RemoveProblems(data.m_problems,
                Notification.Problem2.NoCargoServicePoint
                | Notification.Problem2.NoGarbageServicePoint);
        }
    }
}
