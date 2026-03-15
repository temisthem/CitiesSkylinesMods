using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace SometimesPedestrianStreets.Patches
{
    public static class Patcher
    {
        private const string HarmonyId = "temisthem.sometimespedestrianstreets";
        public static bool IsPatched { get; private set; }

        public static void PatchAll()
        {
            if (IsPatched)
                return;

            Debug.Log("[SometimesPedestrianStreets] Applying Harmony patches...");

            try
            {
                var harmony = new Harmony(HarmonyId);
                harmony.PatchAll(Assembly.GetExecutingAssembly());
                IsPatched = true;
                Debug.Log("[SometimesPedestrianStreets] Harmony patches applied successfully.");
            }
            catch (Exception e)
            {
                Debug.LogError("[SometimesPedestrianStreets] Failed to apply Harmony patches: " + e);
            }
        }

        public static void UnpatchAll()
        {
            if (!IsPatched)
                return;

            Debug.Log("[SometimesPedestrianStreets] Removing Harmony patches...");

            var harmony = new Harmony(HarmonyId);
            harmony.UnpatchAll(HarmonyId);
            IsPatched = false;

            Debug.Log("[SometimesPedestrianStreets] Harmony patches removed.");
        }
    }
}
