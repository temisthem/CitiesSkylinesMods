using ColossalFramework;
using UnityEngine;

namespace SometimesPedestrianStreets
{
    internal static class DistrictUtils
    {
        public static bool IsInPedestrianZone(Vector3 position)
        {
            var district = Singleton<DistrictManager>.instance;
            var park = district.GetPark(position);
            return park != 0 && district.m_parks.m_buffer[park].IsPedestrianZone;
        }
    }
}
