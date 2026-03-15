using System.Reflection;
using ColossalFramework;
using HarmonyLib;
using UnityEngine;

namespace PedestrianStreetServices.Patches
{
    /// <summary>
    /// Ensures vanilla service point routing still activates for buildings inside
    /// pedestrian zones despite our expanded m_vehicleCategories on pedestrian roads.
    ///
    /// TransferManager.AddOutgoingOffer/AddIncomingOffer checks the building's access
    /// segment's m_vehicleCategories to decide whether to route through a service point.
    /// Because PrefabModifier expands that field to include service vehicles, the vanilla
    /// check passes and service point routing is skipped for zoned buildings.
    ///
    /// Fix: temporarily strip our added categories before the vanilla method runs so
    /// the check sees original (Emergency-only) values, then restore them afterwards.
    /// This lets the vanilla service point logic activate naturally for zoned buildings
    /// while non-zoned buildings remain unaffected (GetUseServicePoint returns false).
    /// </summary>
    internal static class TransferManagerHelper
    {
        internal static NetInfo TryStripServiceCategories(
            ushort buildingID,
            TransferManager.TransferReason material,
            bool isOutgoing)
        {
            if (buildingID == 0)
                return null;

            var buffer = Singleton<BuildingManager>.instance.m_buildings.m_buffer;
            var accessSeg = buffer[buildingID].m_accessSegment;

            if (accessSeg == 0)
                return null;

            var dm = Singleton<DistrictManager>.instance;
            var park = dm.GetPark(buffer[buildingID].m_position);
            var inPedZone = park != 0 && dm.m_parks.m_buffer[park].IsPedestrianZone;

            var segInfo = Singleton<NetManager>.instance.m_segments.m_buffer[accessSeg].Info;
            if (!segInfo.IsPedestrianZoneRoad())
                return null;

            if (!inPedZone)
                return null;

            segInfo.m_vehicleCategories &= ~ServiceVehicleCategories.Combined;
            return segInfo;
        }

        internal static void RestoreServiceCategories(NetInfo segInfo)
        {
            if (segInfo != null)
                segInfo.m_vehicleCategories |= ServiceVehicleCategories.Combined;
        }
    }

    [HarmonyPatch(typeof(TransferManager), nameof(TransferManager.AddOutgoingOffer))]
    internal static class TransferManager_AddOutgoingOffer
    {
        [HarmonyPrefix]
        static void Prefix(
            TransferManager.TransferReason material,
            ref TransferManager.TransferOffer offer,
            out NetInfo __state)
        {
            __state = TransferManagerHelper.TryStripServiceCategories(
                offer.Building, material, isOutgoing: true);
        }

        [HarmonyFinalizer]
        static void Finalizer(NetInfo __state)
        {
            TransferManagerHelper.RestoreServiceCategories(__state);
        }
    }

    [HarmonyPatch(typeof(TransferManager), nameof(TransferManager.AddIncomingOffer))]
    internal static class TransferManager_AddIncomingOffer
    {
        [HarmonyPrefix]
        static void Prefix(
            TransferManager.TransferReason material,
            ref TransferManager.TransferOffer offer,
            out NetInfo __state)
        {
            if (offer.Park != 0 && offer.Building == 0)
            {
                DistrictPark.PedestrianZoneTransferReason reason;
                if (DistrictPark.TryGetPedestrianReason(material, out reason)
                    && reason.m_deliveryCategory == DistrictPark.DeliveryCategories.Cargo)
                {
                    offer.Active = true;
                }
            }

            __state = TransferManagerHelper.TryStripServiceCategories(
                offer.Building, material, isOutgoing: false);
        }

        [HarmonyFinalizer]
        static void Finalizer(NetInfo __state)
        {
            TransferManagerHelper.RestoreServiceCategories(__state);
        }
    }

    /// <summary>
    /// Prioritises pedestrian zone park offers in MatchOffers by moving them to the
    /// front of the incoming offer array. Without this, park offers (added last by
    /// AddParkInOffers) sit at the END of the array and are consistently outcompeted
    /// by the ~245 regular building offers that consume all scarce factory supply
    /// before the park offers are ever evaluated.
    /// </summary>
    [HarmonyPatch(typeof(TransferManager), "MatchOffers")]
    internal static class TransferManager_MatchOffers_ParkPriority
    {
        private static readonly FieldInfo IncomingOffersField =
            AccessTools.Field(typeof(TransferManager), "m_incomingOffers");
        private static readonly FieldInfo IncomingCountField =
            AccessTools.Field(typeof(TransferManager), "m_incomingCount");

        [HarmonyPrefix]
        static void Prefix(TransferManager __instance, TransferManager.TransferReason material)
        {
            DistrictPark.PedestrianZoneTransferReason reason;
            if (!DistrictPark.TryGetPedestrianReason(material, out reason)
                || reason.m_deliveryCategory != DistrictPark.DeliveryCategories.Cargo)
                return;

            var inOffers = (TransferManager.TransferOffer[])IncomingOffersField.GetValue(__instance);
            var inCounts = (ushort[])IncomingCountField.GetValue(__instance);

            for (int p = 0; p < 8; p++)
            {
                int idx = (int)material * 8 + p;
                int inCount = inCounts[idx];

                int insertPos = 0;
                for (int i = 0; i < inCount; i++)
                {
                    int offsetI = idx * 256 + i;
                    if (inOffers[offsetI].Park != 0)
                    {
                        if (i != insertPos)
                        {
                            int offsetInsert = idx * 256 + insertPos;
                            var temp = inOffers[offsetInsert];
                            inOffers[offsetInsert] = inOffers[offsetI];
                            inOffers[offsetI] = temp;
                        }
                        insertPos++;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Logs when a cargo truck targets a service point building.
    /// Helps diagnose whether cargo delivery failures to pedestrian zones
    /// are caused by this mod (pathfinding failures) or other issues.
    /// </summary>
    [HarmonyPatch(typeof(CargoTruckAI), nameof(CargoTruckAI.SetTarget))]
    internal static class CargoTruckAI_SetTarget_Diag
    {
        private static int _okCount;

        [HarmonyPostfix]
        static void Postfix(ushort vehicleID, ref Vehicle data, ushort targetBuilding)
        {
            if (targetBuilding == 0)
                return;

            var bldgBuf = Singleton<BuildingManager>.instance.m_buildings.m_buffer;
            if (!(bldgBuf[targetBuilding].Info?.m_buildingAI is ServicePointAI))
                return;

            bool pathOk = (data.m_flags & Vehicle.Flags.WaitingPath) != 0;
            if (pathOk)
            {
                if (_okCount < 20 || _okCount % 500 == 0)
                {
                    Debug.Log("[PSS] CargoTruck → ServicePoint OK: veh=" + vehicleID
                        + " target=" + targetBuilding
                        + " path=" + data.m_path);
                }
                _okCount++;
            }
            else
            {
                Debug.Log("[PSS] CargoTruck → ServicePoint FAILED: veh=" + vehicleID
                    + " target=" + targetBuilding
                    + " flags=" + data.m_flags
                    + " — pathfind failed, vehicle unspawned");
            }
        }
    }
}
