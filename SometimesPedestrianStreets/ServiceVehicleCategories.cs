namespace SometimesPedestrianStreets
{
    /// <summary>
    /// Shared constants defining which vehicle categories are considered
    /// service vehicles that should be allowed on pedestrian streets.
    /// These are the categories used by the pedestrian zone service point system.
    /// </summary>
    internal static class ServiceVehicleCategories
    {
        public static readonly VehicleInfo.VehicleCategoryPart1 Part1 =
            VehicleInfo.VehicleCategoryPart1.CargoTruck;

        public static readonly VehicleInfo.VehicleCategoryPart2 Part2 =
            VehicleInfo.VehicleCategoryPart2.GarbageTruck |
            VehicleInfo.VehicleCategoryPart2.PostTruck |
            VehicleInfo.VehicleCategoryPart2.BankTruck;

        public static readonly VehicleInfo.VehicleCategory Combined =
            VehicleInfo.VehicleCategory.GarbageTruck |
            VehicleInfo.VehicleCategory.CargoTruck |
            VehicleInfo.VehicleCategory.PostTruck |
            VehicleInfo.VehicleCategory.BankTruck;
    }
}
