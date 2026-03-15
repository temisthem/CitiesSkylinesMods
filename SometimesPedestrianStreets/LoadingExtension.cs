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

            if (HarmonyHelper.IsHarmonyInstalled)
            {
                Patches.Patcher.PatchAll();
            }
        }

        public override void OnLevelUnloading()
        {
            if (HarmonyHelper.IsHarmonyInstalled)
            {
                Patches.Patcher.UnpatchAll();
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
