using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace PedestrianStreetServices.Patches
{
    public static class Patcher
    {
        private const string HarmonyId = "com.pedestrianstreetservices";
        private static bool _patched;

        public static void PatchAll()
        {
            if (_patched)
                return;

            Debug.Log("[PedestrianStreetServices] Applying Harmony patches...");

            try
            {
                var harmony = new Harmony(HarmonyId);
                harmony.PatchAll(Assembly.GetExecutingAssembly());
                _patched = true;
                Debug.Log("[PedestrianStreetServices] Harmony patches applied successfully.");
            }
            catch (Exception e)
            {
                Debug.LogError("[PedestrianStreetServices] Failed to apply Harmony patches: " + e);
            }
        }

        public static void UnpatchAll()
        {
            if (!_patched)
                return;

            Debug.Log("[PedestrianStreetServices] Removing Harmony patches...");

            var harmony = new Harmony(HarmonyId);
            harmony.UnpatchAll(HarmonyId);
            _patched = false;

            Debug.Log("[PedestrianStreetServices] Harmony patches removed.");
        }
    }
}
