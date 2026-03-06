using HarmonyLib;

namespace PrecisionEngineering.Patches
{
    /// <summary>
    /// Patches NetAI.GetLengthSnap to allow toggling road length snapping on/off.
    /// When disabled, returns 0 (free placement). When enabled, returns the default 8-unit grid.
    /// </summary>
    [HarmonyPatch(typeof(NetAI), "GetLengthSnap")]
    internal static class NetAIPatches
    {
        static bool Prefix(ref float __result)
        {
            if (!SnapController.EnableLengthSnapping)
            {
                __result = 0f;
                return false;
            }

            __result = 8f;
            return false;
        }
    }
}
