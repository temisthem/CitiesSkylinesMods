using System.Collections.Generic;
using ColossalFramework;
using UnityEngine;

namespace SometimesPedestrianStreets
{
    public static class PrefabModifier
    {
        private struct OriginalLaneData
        {
            public VehicleInfo.VehicleCategoryPart1 Part1;
            public VehicleInfo.VehicleCategoryPart2 Part2;
        }

        public static bool IsApplied { get; private set; }

        // Stores original lane categories keyed by (prefab name, lane index)
        private static readonly Dictionary<string, OriginalLaneData[]> OriginalData =
            new Dictionary<string, OriginalLaneData[]>();

        public static void ApplyLaneModifications()
        {
            if (IsApplied)
                return;

            OriginalData.Clear();

            var count = PrefabCollection<NetInfo>.LoadedCount();
            var modified = 0;

            for (uint i = 0; i < count; i++)
            {
                var prefab = PrefabCollection<NetInfo>.GetLoaded(i);
                if (prefab == null || prefab.m_lanes == null)
                    continue;

                if (!prefab.IsPedestrianZoneRoad())
                    continue;

                var originals = new OriginalLaneData[prefab.m_lanes.Length];
                var prefabModified = false;

                for (var j = 0; j < prefab.m_lanes.Length; j++)
                {
                    var lane = prefab.m_lanes[j];

                    originals[j] = new OriginalLaneData
                    {
                        Part1 = lane.m_vehicleCategoryPart1,
                        Part2 = lane.m_vehicleCategoryPart2
                    };

                    // Only expand vehicle lanes that already allow car traffic
                    if ((lane.m_laneType & (NetInfo.LaneType.Vehicle | NetInfo.LaneType.TransportVehicle)) == NetInfo.LaneType.None)
                        continue;

                    if ((lane.m_vehicleType & VehicleInfo.VehicleType.Car) == VehicleInfo.VehicleType.None)
                        continue;

                    lane.m_vehicleCategoryPart1 |= ServiceVehicleCategories.Part1;
                    lane.m_vehicleCategoryPart2 |= ServiceVehicleCategories.Part2;
                    prefabModified = true;
                }

                if (prefabModified)
                {
                    OriginalData[prefab.name] = originals;
                    RecalculateVehicleCategories(prefab);
                    modified++;
                }
            }

            IsApplied = true;
            Debug.Log("[SometimesPedestrianStreets] Modified lane categories on " + modified + " pedestrian street prefabs.");
        }

        public static void RevertLaneModifications()
        {
            if (!IsApplied)
                return;

            var count = PrefabCollection<NetInfo>.LoadedCount();

            for (uint i = 0; i < count; i++)
            {
                var prefab = PrefabCollection<NetInfo>.GetLoaded(i);
                if (prefab == null || prefab.m_lanes == null)
                    continue;

                OriginalLaneData[] originals;
                if (!OriginalData.TryGetValue(prefab.name, out originals))
                    continue;

                for (var j = 0; j < prefab.m_lanes.Length && j < originals.Length; j++)
                {
                    prefab.m_lanes[j].m_vehicleCategoryPart1 = originals[j].Part1;
                    prefab.m_lanes[j].m_vehicleCategoryPart2 = originals[j].Part2;
                }

                RecalculateVehicleCategories(prefab);
            }

            OriginalData.Clear();
            IsApplied = false;
            Debug.Log("[SometimesPedestrianStreets] Reverted all lane category modifications.");
        }

        private static void RecalculateVehicleCategories(NetInfo prefab)
        {
            prefab.m_vehicleCategories = VehicleInfo.VehicleCategory.None;

            for (var i = 0; i < prefab.m_lanes.Length; i++)
            {
                var lane = prefab.m_lanes[i];

                if ((lane.m_laneType & (NetInfo.LaneType.Vehicle | NetInfo.LaneType.TransportVehicle)) == NetInfo.LaneType.None)
                    continue;

                if (lane.m_vehicleType == VehicleInfo.VehicleType.None)
                    continue;

                prefab.m_vehicleCategories |= lane.vehicleCategory;
            }
        }
    }
}
