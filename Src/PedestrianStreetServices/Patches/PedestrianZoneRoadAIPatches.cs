using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace PedestrianStreetServices.Patches
{
    /// <summary>
    /// Expands the bollard entry/exit point configuration to recognize service
    /// vehicles in addition to emergency vehicles. Without this, bollards would
    /// have no enter/exit points for service vehicle lanes and the traffic light
    /// cycle would not apply to them.
    ///
    /// The original code masks lane vehicle categories with VehicleCategory.Emergency.
    /// We replace that constant with an expanded mask that includes service vehicles.
    /// </summary>
    [HarmonyPatch(typeof(PedestrianZoneRoadAI), nameof(PedestrianZoneRoadAI.UpdateBollards))]
    internal static class PedestrianZoneRoadAI_UpdateBollards
    {
        private static readonly long OriginalMask = (long)VehicleInfo.VehicleCategory.Emergency;

        private static readonly long ExpandedMask = (long)(
            VehicleInfo.VehicleCategory.Emergency | ServiceVehicleCategories.Combined);

        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldc_I8 && instruction.operand is long value && value == OriginalMask)
                {
                    yield return new CodeInstruction(OpCodes.Ldc_I8, ExpandedMask);
                }
                else
                {
                    yield return instruction;
                }
            }
        }
    }
}
