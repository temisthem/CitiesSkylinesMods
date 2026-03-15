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
        private static int _logCount;

        internal static NetInfo TryStripServiceCategories(
            ushort buildingID,
            TransferManager.TransferReason material,
            bool isOutgoing)
        {
            if (buildingID == 0)
                return null;

            var buffer = Singleton<BuildingManager>.instance.m_buildings.m_buffer;
            var accessSeg = buffer[buildingID].m_accessSegment;

            var dm = Singleton<DistrictManager>.instance;
            var park = dm.GetPark(buffer[buildingID].m_position);
            var inPedZone = park != 0 && dm.m_parks.m_buffer[park].IsPedestrianZone;

            if (_logCount < 50 || _logCount % 2000 == 0)
            {
                var dir = isOutgoing ? "Out" : "In";
                var segName = accessSeg != 0
                    ? Singleton<NetManager>.instance.m_segments.m_buffer[accessSeg].Info.name
                    : "none";
                var isPedRoad = accessSeg != 0 &&
                    Singleton<NetManager>.instance.m_segments.m_buffer[accessSeg].Info.IsPedestrianZoneRoad();
                var useServicePoint = buffer[buildingID].Info.m_buildingAI.GetUseServicePoint(
                    buildingID, ref buffer[buildingID]);
                var vcats = accessSeg != 0
                    ? Singleton<NetManager>.instance.m_segments.m_buffer[accessSeg].Info.m_vehicleCategories
                    : VehicleInfo.VehicleCategory.None;

                Debug.Log("[PSS] Transfer " + dir + ": bldg=" + buildingID
                    + " material=" + material
                    + " park=" + park + " inPedZone=" + inPedZone
                    + " accessSeg=" + accessSeg + " road=" + segName
                    + " isPedRoad=" + isPedRoad
                    + " useServicePoint=" + useServicePoint
                    + " vehicleCats=" + vcats);
            }
            _logCount++;

            if (accessSeg == 0)
                return null;

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
        private static int _parkOfferLog;

        [HarmonyPrefix]
        static void Prefix(
            TransferManager.TransferReason material,
            ref TransferManager.TransferOffer offer,
            out NetInfo __state)
        {
            if (offer.Park != 0 && offer.Building == 0
                && (_parkOfferLog < 100 || _parkOfferLog % 500 == 0))
            {
                Debug.Log("[PSS] ParkOffer Out: material=" + material
                    + " park=" + offer.Park
                    + " localPark=" + offer.m_isLocalPark
                    + " active=" + offer.Active);
                _parkOfferLog++;
            }

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
        private static int _parkOfferLog;

        [HarmonyPrefix]
        static void Prefix(
            TransferManager.TransferReason material,
            ref TransferManager.TransferOffer offer,
            out NetInfo __state)
        {
            // Park-level incoming offers (from AddParkInOffers) for cargo materials
            // are created as passive (Active=false). Making Active=true doesn't affect
            // MatchOffers (no Active check) but ensures correct StartTransfer dispatch.
            if (offer.Park != 0 && offer.Building == 0)
            {
                DistrictPark.PedestrianZoneTransferReason reason;
                if (DistrictPark.TryGetPedestrianReason(material, out reason)
                    && reason.m_deliveryCategory == DistrictPark.DeliveryCategories.Cargo)
                {
                    offer.Active = true;
                }

                if (_parkOfferLog < 100 || _parkOfferLog % 500 == 0)
                {
                    Debug.Log("[PSS] ParkOffer In: material=" + material
                        + " park=" + offer.Park
                        + " localPark=" + offer.m_isLocalPark
                        + " active=" + offer.Active
                        + " priority=" + offer.Priority
                        + " amount=" + offer.Amount);
                }
                _parkOfferLog++;
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
    /// Diagnostic: logs when AddMaterialRequest is called on a pedestrian zone park.
    /// </summary>
    [HarmonyPatch(typeof(DistrictPark), nameof(DistrictPark.AddMaterialRequest))]
    internal static class DistrictPark_AddMaterialRequest_Diag
    {
        private static int _logCount;

        [HarmonyPostfix]
        static void Postfix(ushort buildingID, TransferManager.TransferReason material)
        {
            if (_logCount < 100 || _logCount % 500 == 0)
            {
                Debug.Log("[PSS] AddMaterialRequest: building=" + buildingID
                    + " material=" + material);
            }
            _logCount++;
        }
    }

    /// <summary>
    /// Diagnostic: logs when AddParkInOffers runs.
    /// </summary>
    [HarmonyPatch(typeof(DistrictPark), "AddParkInOffers",
        new[] { typeof(byte) })]
    internal static class DistrictPark_AddParkInOffers_Diag
    {
        private static int _logCount;

        [HarmonyPrefix]
        static void Prefix(byte parkID)
        {
            if (_logCount < 30 || _logCount % 200 == 0)
            {
                var park = Singleton<DistrictManager>.instance.m_parks.m_buffer[parkID];
                Debug.Log("[PSS] AddParkInOffers: park=" + parkID
                    + " cargoSP=" + park.m_cargoServicePointExist
                    + " garbageSP=" + park.m_garbageServicePointExist
                    + " gateCount=" + park.m_finalGateCount
                    + " isPedZone=" + park.IsPedestrianZone);
            }
            _logCount++;
        }
    }

    /// <summary>
    /// Diagnostic: logs StartTransfer, with a separate uncapped counter for
    /// park-related Goods transfers so they are never missed.
    /// </summary>
    [HarmonyPatch(typeof(TransferManager), "StartTransfer")]
    internal static class TransferManager_StartTransfer_Diag
    {
        private static int _parkLogCount;
        private static int _goodsLogCount;
        private static int _goodsParkLogCount;

        [HarmonyPrefix]
        static void Prefix(
            TransferManager.TransferReason material,
            TransferManager.TransferOffer offerOut,
            TransferManager.TransferOffer offerIn)
        {
            if (material == TransferManager.TransferReason.Goods
                || material == TransferManager.TransferReason.LuxuryProducts)
            {
                // ALWAYS log park-related Goods transfers (separate counter, high cap)
                if (offerIn.Park != 0 || offerOut.Park != 0
                    || offerIn.m_isLocalPark != 0 || offerOut.m_isLocalPark != 0)
                {
                    if (_goodsParkLogCount < 500)
                    {
                        // Check if TryGetRandomServicePoint will succeed
                        string spInfo = "";
                        if (offerIn.Park != 0)
                        {
                            ushort spBldg;
                            var dm = Singleton<DistrictManager>.instance;
                            bool resolved = dm.m_parks.m_buffer[offerIn.Park]
                                .TryGetRandomServicePoint(material, out spBldg);
                            spInfo = " spResolved=" + resolved + " spBldg=" + spBldg;
                        }
                        if (offerOut.Park != 0)
                        {
                            ushort spBldg;
                            var dm = Singleton<DistrictManager>.instance;
                            bool resolved = dm.m_parks.m_buffer[offerOut.Park]
                                .TryGetRandomServicePoint(material, out spBldg);
                            spInfo += " spOutResolved=" + resolved + " spOutBldg=" + spBldg;
                        }

                        // Log factory building AI type and service point access road
                        var bldgBuf = Singleton<BuildingManager>.instance.m_buildings.m_buffer;
                        string factoryAI = offerOut.Building != 0
                            ? bldgBuf[offerOut.Building].Info.m_buildingAI.GetType().Name
                            : "none";
                        string spRoad = "";
                        if (spInfo.Contains("spBldg=") && offerIn.Park != 0)
                        {
                            ushort spCheck;
                            Singleton<DistrictManager>.instance.m_parks.m_buffer[offerIn.Park]
                                .TryGetRandomServicePoint(material, out spCheck);
                            if (spCheck != 0)
                            {
                                var spSeg = bldgBuf[spCheck].m_accessSegment;
                                spRoad = " spSeg=" + spSeg;
                                if (spSeg != 0)
                                {
                                    var spSegInfo = Singleton<NetManager>.instance
                                        .m_segments.m_buffer[spSeg].Info;
                                    spRoad += " spRoad=" + spSegInfo.name
                                        + " spIsPed=" + spSegInfo.IsPedestrianZoneRoad();
                                }
                            }
                        }

                        Debug.Log("[PSS] StartTransfer PARK " + material + ":"
                            + " outBldg=" + offerOut.Building + " outPark=" + offerOut.Park
                            + " inBldg=" + offerIn.Building + " inPark=" + offerIn.Park
                            + " outLP=" + offerOut.m_isLocalPark
                            + " inLP=" + offerIn.m_isLocalPark
                            + " factoryAI=" + factoryAI
                            + spInfo + spRoad);
                    }
                    _goodsParkLogCount++;
                }
                else if (_goodsLogCount < 50 || _goodsLogCount % 500 == 0)
                {
                    Debug.Log("[PSS] StartTransfer " + material + ":"
                        + " outBldg=" + offerOut.Building + " outPark=" + offerOut.Park
                        + " inBldg=" + offerIn.Building + " inPark=" + offerIn.Park
                        + " outLP=" + offerOut.m_isLocalPark
                        + " inLP=" + offerIn.m_isLocalPark);
                }
                _goodsLogCount++;
            }
            else if (offerIn.Park != 0 || offerOut.Park != 0)
            {
                if (_parkLogCount < 100 || _parkLogCount % 500 == 0)
                {
                    Debug.Log("[PSS] StartTransfer: material=" + material
                        + " outBldg=" + offerOut.Building + " outPark=" + offerOut.Park
                        + " inBldg=" + offerIn.Building + " inPark=" + offerIn.Park
                        + " outLP=" + offerOut.m_isLocalPark
                        + " inLP=" + offerIn.m_isLocalPark);
                }
                _parkLogCount++;
            }
        }
    }

    /// <summary>
    /// Prioritises pedestrian zone park offers in MatchOffers by moving them to the
    /// front of the incoming offer array. Without this, park offers (added last by
    /// AddParkInOffers) sit at the END of the array and are consistently outcompeted
    /// by the ~245 regular building offers that consume all scarce factory supply
    /// before the park offers are ever evaluated.
    ///
    /// Also logs pool state for diagnostics.
    /// </summary>
    [HarmonyPatch(typeof(TransferManager), "MatchOffers")]
    internal static class TransferManager_MatchOffers_ParkPriority
    {
        private static int _logCount;

        private static readonly FieldInfo IncomingOffersField =
            AccessTools.Field(typeof(TransferManager), "m_incomingOffers");
        private static readonly FieldInfo OutgoingOffersField =
            AccessTools.Field(typeof(TransferManager), "m_outgoingOffers");
        private static readonly FieldInfo IncomingCountField =
            AccessTools.Field(typeof(TransferManager), "m_incomingCount");
        private static readonly FieldInfo OutgoingCountField =
            AccessTools.Field(typeof(TransferManager), "m_outgoingCount");

        [HarmonyPrefix]
        static void Prefix(TransferManager __instance, TransferManager.TransferReason material)
        {
            // Only act on pedestrian-zone cargo materials
            DistrictPark.PedestrianZoneTransferReason reason;
            if (!DistrictPark.TryGetPedestrianReason(material, out reason)
                || reason.m_deliveryCategory != DistrictPark.DeliveryCategories.Cargo)
                return;

            var inOffers = (TransferManager.TransferOffer[])IncomingOffersField.GetValue(__instance);
            var outOffers = (TransferManager.TransferOffer[])OutgoingOffersField.GetValue(__instance);
            var inCounts = (ushort[])IncomingCountField.GetValue(__instance);
            var outCounts = (ushort[])OutgoingCountField.GetValue(__instance);

            int totalIn = 0, parkIn = 0, localParkIn = 0;
            int totalOut = 0, localParkOut = 0;
            string parkInDetail = "";

            for (int p = 0; p < 8; p++)
            {
                int idx = (int)material * 8 + p;
                int inCount = inCounts[idx];
                int outCount = outCounts[idx];
                totalIn += inCount;
                totalOut += outCount;

                // Move park offers (Park != 0) to the front of this priority bucket
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

                // Diagnostic counting (after reorder so indices are stable)
                for (int i = 0; i < inCount; i++)
                {
                    var offer = inOffers[idx * 256 + i];
                    if (offer.Park != 0)
                    {
                        parkIn++;
                        parkInDetail += " [P" + offer.Park + " pri=" + p
                            + " amt=" + offer.Amount + " lp=" + offer.m_isLocalPark
                            + " act=" + offer.Active + "]";
                    }
                    if (offer.m_isLocalPark != 0)
                        localParkIn++;
                }

                for (int i = 0; i < outCount; i++)
                {
                    if (outOffers[idx * 256 + i].m_isLocalPark != 0)
                        localParkOut++;
                }
            }

            // Diagnostic log
            if (_logCount < 30 || _logCount % 100 == 0)
            {
                Debug.Log("[PSS] MatchOffers " + material + ": totalIn=" + totalIn
                    + " parkIn=" + parkIn + " localParkIn=" + localParkIn
                    + " totalOut=" + totalOut + " localParkOut=" + localParkOut
                    + parkInDetail);
            }
            _logCount++;
        }
    }

    /// <summary>
    /// Diagnostic: logs when a cargo truck's target is set to a service point building,
    /// confirming that vehicles are actually being created and targeted correctly.
    /// Also logs if StartPathFind fails (vehicle gets Unspawned).
    /// </summary>
    [HarmonyPatch(typeof(CargoTruckAI), nameof(CargoTruckAI.SetTarget))]
    internal static class CargoTruckAI_SetTarget_Diag
    {
        private static int _logCount;

        [HarmonyPrefix]
        static void Prefix(ushort vehicleID, ref Vehicle data, ushort targetBuilding)
        {
            if (targetBuilding == 0 || _logCount > 200)
                return;

            var bldgBuf = Singleton<BuildingManager>.instance.m_buildings.m_buffer;
            var ai = bldgBuf[targetBuilding].Info?.m_buildingAI;
            if (ai is ServicePointAI)
            {
                var src = data.m_sourceBuilding;
                var srcAI = src != 0 ? bldgBuf[src].Info?.m_buildingAI.GetType().Name : "none";
                var flags = data.m_flags;
                var material = (TransferManager.TransferReason)data.m_transferType;
                Debug.Log("[PSS] CargoTruck.SetTarget: veh=" + vehicleID
                    + " target=" + targetBuilding + " (ServicePoint)"
                    + " src=" + src + " srcAI=" + srcAI
                    + " material=" + material
                    + " flags=" + flags);
                _logCount++;
            }
        }

        [HarmonyPostfix]
        static void Postfix(ushort vehicleID, ref Vehicle data, ushort targetBuilding)
        {
            if (targetBuilding == 0)
                return;

            var bldgBuf = Singleton<BuildingManager>.instance.m_buildings.m_buffer;
            var ai = bldgBuf[targetBuilding].Info?.m_buildingAI;
            if (ai is ServicePointAI)
            {
                // Check if vehicle was unspawned (StartPathFind failed)
                if ((data.m_flags & Vehicle.Flags.Spawned) == 0
                    && (data.m_flags & Vehicle.Flags.WaitingPath) == 0)
                {
                    Debug.Log("[PSS] CargoTruck.SetTarget FAILED: veh=" + vehicleID
                        + " target=" + targetBuilding
                        + " flags=" + data.m_flags
                        + " path=" + data.m_path
                        + " — vehicle was NOT spawned (pathfind likely failed)");
                }
                else
                {
                    Debug.Log("[PSS] CargoTruck.SetTarget OK: veh=" + vehicleID
                        + " target=" + targetBuilding
                        + " flags=" + data.m_flags
                        + " path=" + data.m_path);
                }
            }
        }
    }
}
