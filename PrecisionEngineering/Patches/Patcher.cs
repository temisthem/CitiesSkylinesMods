using System;
using System.Reflection;
using HarmonyLib;

namespace PrecisionEngineering.Patches
{
    public static class Patcher
    {
        private const string HarmonyId = "com.precisionengineering";
        private static bool _patched;

        public static void PatchAll()
        {
            if (_patched)
            {
                return;
            }

            Debug.Log("Applying Harmony patches...");

            try
            {
                var harmony = new Harmony(HarmonyId);
                harmony.PatchAll(Assembly.GetExecutingAssembly());
                _patched = true;
                Debug.Log("Harmony patches applied successfully.");
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to apply Harmony patches: " + e);
            }
        }

        public static void UnpatchAll()
        {
            if (!_patched)
            {
                return;
            }

            Debug.Log("Removing Harmony patches...");

            var harmony = new Harmony(HarmonyId);
            harmony.UnpatchAll(HarmonyId);
            _patched = false;

            Debug.Log("Harmony patches removed.");
        }
    }
}
