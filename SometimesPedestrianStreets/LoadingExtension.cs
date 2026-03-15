using System;
using CitiesHarmony.API;
using ICities;
using UnityEngine;

namespace SometimesPedestrianStreets
{
    public class LoadingExtension : LoadingExtensionBase
    {
        public override void OnLevelLoaded(LoadMode mode)
        {
            base.OnLevelLoaded(mode);

            try
            {
                PrefabModifier.ApplyLaneModifications();
            }
            catch (Exception e)
            {
                Debug.LogError("[SometimesPedestrianStreets] Failed to apply prefab modifications: " + e);
            }

            try
            {
                if (HarmonyHelper.IsHarmonyInstalled)
                {
                    Patches.Patcher.PatchAll();
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[SometimesPedestrianStreets] Failed to apply Harmony patches: " + e);
            }
        }

        public override void OnLevelUnloading()
        {
            try
            {
                if (HarmonyHelper.IsHarmonyInstalled)
                {
                    Patches.Patcher.UnpatchAll();
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[SometimesPedestrianStreets] Failed to remove Harmony patches: " + e);
            }

            try
            {
                PrefabModifier.RevertLaneModifications();
            }
            catch (Exception e)
            {
                Debug.LogError("[SometimesPedestrianStreets] Failed to revert prefab modifications: " + e);
            }

            base.OnLevelUnloading();
        }
    }
}
