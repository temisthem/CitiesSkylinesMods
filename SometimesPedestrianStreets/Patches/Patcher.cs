using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace SometimesPedestrianStreets.Patches
{
    public static class Patcher
    {
        private const string HarmonyId = "temisthem.sometimespedestrianstreets";
        private static bool _patched;

        public static void PatchAll()
        {
            if (_patched)
                return;

            Debug.Log("[SometimesPedestrianStreets] Applying Harmony patches...");

            try
            {
                var harmony = new Harmony(HarmonyId);
                harmony.PatchAll(Assembly.GetExecutingAssembly());
                _patched = true;
                Debug.Log("[SometimesPedestrianStreets] Harmony patches applied successfully.");
            }
            catch (Exception e)
            {
                Debug.LogError("[SometimesPedestrianStreets] Failed to apply Harmony patches: " + e);
            }
        }

        public static void UnpatchAll()
        {
            if (!_patched)
                return;

            Debug.Log("[SometimesPedestrianStreets] Removing Harmony patches...");

            var harmony = new Harmony(HarmonyId);
            harmony.UnpatchAll(HarmonyId);
            _patched = false;

            Debug.Log("[SometimesPedestrianStreets] Harmony patches removed.");
        }
    }
}
