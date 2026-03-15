using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using ColossalFramework;
using HarmonyLib;
using UnityEngine;

namespace SometimesPedestrianStreets.Patches
{
    /// <summary>
    /// Expands the bollard entry/exit point configuration to recognize service
    /// vehicles in addition to emergency vehicles, but only for nodes that are
    /// NOT inside a pedestrian zone district. Nodes inside a zone retain vanilla
    /// behavior so that the service point system handles deliveries.
    ///
    /// The original code masks lane vehicle categories with VehicleCategory.Emergency.
    /// We replace that constant load with a call to a helper that returns an expanded
    /// mask when outside a zone, or the original Emergency mask when inside one.
    /// </summary>
    [HarmonyPatch(typeof(PedestrianZoneRoadAI), nameof(PedestrianZoneRoadAI.UpdateBollards))]
    internal static class PedestrianZoneRoadAI_UpdateBollards
    {
        private static readonly long OriginalMask = (long)VehicleInfo.VehicleCategory.Emergency;

        /// <summary>
        /// Returns the appropriate bollard vehicle category mask for the given node.
        /// Called from transpiled IL in place of the original Emergency constant.
        /// </summary>
        public static long GetBollardMask(ushort nodeID)
        {
            var position = Singleton<NetManager>.instance.m_nodes.m_buffer[nodeID].m_position;

            if (DistrictUtils.IsInPedestrianZone(position))
                return (long)VehicleInfo.VehicleCategory.Emergency;

            return (long)(VehicleInfo.VehicleCategory.Emergency | ServiceVehicleCategories.Combined);
        }

        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var getBollardMask = typeof(PedestrianZoneRoadAI_UpdateBollards).GetMethod(
                nameof(GetBollardMask), BindingFlags.Public | BindingFlags.Static);

            var patched = false;

            foreach (var instruction in instructions)
            {
                if (!patched && instruction.opcode == OpCodes.Ldc_I8
                    && instruction.operand is long value && value == OriginalMask)
                {
                    // Replace: ldc.i8 Emergency
                    // With:    ldarg.0 (nodeID) / call GetBollardMask
                    // UpdateBollards signature: static void UpdateBollards(ushort nodeID, ref NetNode nodeData)
                    // So arg 0 = nodeID
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, getBollardMask);
                    patched = true;
                }
                else
                {
                    yield return instruction;
                }
            }

            if (!patched)
            {
                Debug.LogWarning(
                    "[SometimesPedestrianStreets] Could not find Emergency mask in PedestrianZoneRoadAI.UpdateBollards.");
            }
        }
    }
}
