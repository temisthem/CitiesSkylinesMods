using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using ColossalFramework;
using HarmonyLib;
using UnityEngine;

namespace PedestrianStreetServices.Patches
{
    /// <summary>
    /// Allows service vehicles to force-open bollards the same way emergency
    /// vehicles do. Without this, service vehicles would be stopped at Red
    /// bollards indefinitely.
    ///
    /// The original code checks (vehicleData.m_flags &amp; Vehicle.Flags.Emergency2) != 0
    /// to decide whether a vehicle can force a bollard open. We replace that check
    /// with a helper that also returns true for service vehicle categories.
    /// </summary>
    [HarmonyPatch]
    internal static class CarAI_CalculateSegmentPosition
    {
        static MethodBase TargetMethod()
        {
            return typeof(CarAI).GetMethod(
                "CalculateSegmentPosition",
                BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new Type[]
                {
                    typeof(ushort),                  // vehicleID
                    typeof(Vehicle).MakeByRefType(),  // ref vehicleData
                    typeof(PathUnit.Position),        // nextPosition
                    typeof(PathUnit.Position),        // position
                    typeof(uint),                     // laneID
                    typeof(byte),                     // offset
                    typeof(PathUnit.Position),        // prevPos
                    typeof(uint),                     // prevLaneID
                    typeof(byte),                     // prevOffset
                    typeof(int),                      // index
                    typeof(Vector3).MakeByRefType(),  // out pos
                    typeof(Vector3).MakeByRefType(),  // out dir
                    typeof(float).MakeByRefType()     // out maxSpeed
                },
                null);
        }

        /// <summary>
        /// Returns true if the vehicle should be allowed to force-open a bollard.
        /// Called from the transpiled IL in place of the original Emergency2 flag check.
        /// </summary>
        public static bool ShouldPassBollard(Vehicle.Flags flags, ushort vehicleID)
        {
            if ((flags & Vehicle.Flags.Emergency2) != 0)
                return true;

            var info = Singleton<VehicleManager>.instance.m_vehicles.m_buffer[vehicleID].Info;
            return info != null
                && (info.vehicleCategory & ServiceVehicleCategories.Combined) != VehicleInfo.VehicleCategory.None;
        }

        [HarmonyTranspiler]
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var shouldPassBollard = typeof(CarAI_CalculateSegmentPosition).GetMethod(
                nameof(ShouldPassBollard), BindingFlags.Public | BindingFlags.Static);

            var vehicleFlagsField = typeof(Vehicle).GetField(nameof(Vehicle.m_flags));

            var codes = new List<CodeInstruction>(instructions);
            var patched = false;

            for (var i = 0; i < codes.Count - 2; i++)
            {
                // Match the IL pattern:
                //   ldfld  Vehicle.m_flags
                //   ldc.i4 Vehicle.Flags.Emergency2
                //   and
                if (codes[i].opcode == OpCodes.Ldfld
                    && codes[i].operand is FieldInfo fi && fi == vehicleFlagsField
                    && IsEmergency2Constant(codes[i + 1])
                    && codes[i + 2].opcode == OpCodes.And)
                {
                    // Replace:  ldfld m_flags / ldc Emergency2 / and
                    // With:     ldfld m_flags / ldarg.1        / call ShouldPassBollard
                    // Stack:    [Vehicle.Flags, ushort] -> call -> [bool]
                    codes[i + 1] = new CodeInstruction(OpCodes.Ldarg_1); // vehicleID (arg 0=this, 1=vehicleID)
                    codes[i + 2] = new CodeInstruction(OpCodes.Call, shouldPassBollard);

                    patched = true;
                    break;
                }
            }

            if (!patched)
            {
                Debug.LogWarning(
                    "[PedestrianStreetServices] Could not find Emergency2 bollard check in CarAI.CalculateSegmentPosition. " +
                    "Service vehicles may not be able to force-open bollards.");
            }

            return codes;
        }

        private static bool IsEmergency2Constant(CodeInstruction instruction)
        {
            if (instruction.opcode != OpCodes.Ldc_I4
                && instruction.opcode != OpCodes.Ldc_I4_S
                && instruction.opcode != OpCodes.Ldc_I8)
                return false;

            var emergency2Value = (int)Vehicle.Flags.Emergency2;

            if (instruction.operand is int intVal)
                return intVal == emergency2Value;
            if (instruction.operand is sbyte sbyteVal)
                return sbyteVal == emergency2Value;
            if (instruction.operand is long longVal)
                return longVal == emergency2Value;

            return false;
        }
    }
}
