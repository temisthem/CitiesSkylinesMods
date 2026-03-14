using ColossalFramework;
using HarmonyLib;
using UnityEngine;

namespace PedestrianStreetServices.Patches
{
    /// <summary>
    /// Forces service point routing for buildings in pedestrian zones that are
    /// on pedestrian streets with our expanded vehicle categories.
    ///
    /// Without this, PrefabModifier's lane expansion causes TransferManager to
    /// think service vehicles can directly access the building's road, skipping
    /// service point routing entirely. These prefixes restore vanilla routing
    /// for zoned buildings by setting the offer's park flag before the original
    /// vehicle category check runs.
    /// </summary>
    internal static class TransferManagerHelper
    {
        private static int _logCounter;

        /// <summary>
        /// Shared logic for both AddOutgoingOffer and AddIncomingOffer prefixes.
        /// Returns true if service point routing was forced; false if skipped.
        /// </summary>
        internal static bool TryForceServicePointRouting(
            TransferManager.TransferReason material,
            ref TransferManager.TransferOffer offer,
            bool isOutgoing)
        {
            if (offer.Building == 0)
                return false;

            var buffer = Singleton<BuildingManager>.instance.m_buildings.m_buffer;
            var building = offer.Building;
            var districtManager = Singleton<DistrictManager>.instance;
            var park = districtManager.GetPark(buffer[building].m_position);

            if (park == 0 || !districtManager.m_parks.m_buffer[park].IsPedestrianZone)
                return false;

            // If ForceServicePoint policy is active, vanilla code handles it correctly
            if ((districtManager.m_parks.m_buffer[park].m_parkPolicies
                 & DistrictPolicies.Park.ForceServicePoint) != DistrictPolicies.Park.None)
                return false;

            if (!buffer[building].Info.m_buildingAI.GetUseServicePoint(building, ref buffer[building]))
                return false;

            DistrictPark.PedestrianZoneTransferReason reason;
            if (!DistrictPark.TryGetPedestrianReason(material, out reason))
                return false;

            // Replicate vanilla's lazy access segment initialization
            var accessSegment = buffer[building].m_accessSegment;
            if (accessSegment == 0
                && (buffer[building].m_problems
                    & new Notification.ProblemStruct(
                        Notification.Problem1.RoadNotConnected,
                        Notification.Problem2.NotInPedestrianZone)).IsNone)
            {
                buffer[building].Info.m_buildingAI.CheckRoadAccess(
                    building, ref buffer[building]);
                accessSegment = buffer[building].m_accessSegment;
            }

            if (accessSegment == 0)
                return false;

            var segInfo = Singleton<NetManager>.instance.m_segments.m_buffer[accessSegment].Info;

            // Only intervene when the access segment is a pedestrian street —
            // vanilla logic is fine for regular roads in pedestrian zones
            if (!segInfo.IsPedestrianZoneRoad())
                return false;

            // Force service point routing
            offer.m_isLocalPark = park;

            if (isOutgoing)
                districtManager.m_parks.m_buffer[park].AddMaterialSuggestion(building, material);
            else
                districtManager.m_parks.m_buffer[park].AddMaterialRequest(building, material);

            _logCounter++;
            if (_logCounter <= 30 || _logCounter % 500 == 0)
            {
                var direction = isOutgoing ? "Outgoing" : "Incoming";
                Debug.Log("[PSS] TransferManager." + direction + ": forced service point routing for building "
                    + building + " material=" + material + " park=" + park
                    + " vehicleCat=" + reason.m_vehicleCategory
                    + " deliveryCat=" + reason.m_deliveryCategory);
            }

            return true;
        }
    }

    [HarmonyPatch(typeof(TransferManager), nameof(TransferManager.AddOutgoingOffer))]
    internal static class TransferManager_AddOutgoingOffer
    {
        [HarmonyPrefix]
        static void Prefix(TransferManager.TransferReason material, ref TransferManager.TransferOffer offer)
        {
            TransferManagerHelper.TryForceServicePointRouting(material, ref offer, isOutgoing: true);
        }
    }

    [HarmonyPatch(typeof(TransferManager), nameof(TransferManager.AddIncomingOffer))]
    internal static class TransferManager_AddIncomingOffer
    {
        [HarmonyPrefix]
        static void Prefix(TransferManager.TransferReason material, ref TransferManager.TransferOffer offer)
        {
            TransferManagerHelper.TryForceServicePointRouting(material, ref offer, isOutgoing: false);
        }
    }
}
