using ColossalFramework;
using UnityEngine;

namespace SometimesPedestrianStreets
{
    internal static class DistrictUtils
    {
        public static bool IsInPedestrianZone(Vector3 position)
        {
            var dm = Singleton<DistrictManager>.instance;
            var park = dm.GetPark(position);
            return park != 0 && dm.m_parks.m_buffer[park].IsPedestrianZone;
        }
    }
}
