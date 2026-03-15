using System;
using ColossalFramework;
using HarmonyLib;
using UnityEngine;

namespace PrecisionEngineering.Patches
{
    /// <summary>
    /// There appears to bug in Unity3D and Windows where holding left-alt, then ctrl, then releasing left-alt will trigger
    /// Alt-GR being pressed and won't release
    /// until you press Alt-GR itself. (On Windows Alt-Ctrl = AltGR. I'm not sure why it gets stuck)
    /// This breaks camera movement etc, so we patch the input methods used by camera input
    /// to disable checking for AltGR when masking alt.
    /// Right-Alt is still checked so it shouldn't actually be a problem.
    /// </summary>
    internal static class AltKeyFix
    {
        private const int MASK_KEY = 268435455;
        private const int MASK_CONTROL = 1073741824;
        private const int MASK_SHIFT = 536870912;
        private const int MASK_ALT = 268435456;

        [HarmonyPatch(typeof(SavedInputKey), "IsPressed", new Type[] {})]
        internal static class IsPressedPatch
        {
            static bool Prefix(SavedInputKey __instance, ref bool __result)
            {
                int num = __instance.value;
                var keyCode = (KeyCode) (num & MASK_KEY);
                __result = keyCode != KeyCode.None && Input.GetKey(keyCode) &&
                           (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) ==
                           ((num & MASK_CONTROL) != 0) &&
                           (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) ==
                           ((num & MASK_SHIFT) != 0) &&
                           (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) ==
                           ((num & MASK_ALT) != 0);
                return false;
            }
        }

        [HarmonyPatch(typeof(SavedInputKey), "IsKeyUp")]
        internal static class IsKeyUpPatch
        {
            static bool Prefix(SavedInputKey __instance, ref bool __result)
            {
                int num = __instance.value;
                var keyCode = (KeyCode) (num & MASK_KEY);
                __result = keyCode != KeyCode.None && Input.GetKeyUp(keyCode) &&
                           (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)) ==
                           ((num & MASK_CONTROL) != 0) &&
                           (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) ==
                           ((num & MASK_SHIFT) != 0) &&
                           (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)) ==
                           ((num & MASK_ALT) != 0);
                return false;
            }
        }
    }
}
