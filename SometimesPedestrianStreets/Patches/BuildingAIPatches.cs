using ColossalFramework;
using HarmonyLib;

namespace SometimesPedestrianStreets.Patches
{
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
