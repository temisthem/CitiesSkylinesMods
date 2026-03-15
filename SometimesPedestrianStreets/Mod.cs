using System;
using CitiesHarmony.API;
using ICities;
using UnityEngine;

namespace SometimesPedestrianStreets
{
    public class Mod : LoadingExtensionBase, IUserMod
    {
        public string Name => "Sometimes Pedestrian Streets";

        public string Description =>
            "Allows buildings to be placed on pedestrian streets without requiring a pedestrian zone, " +
            "and enables service vehicles (garbage, cargo, mail, etc.) to drive on pedestrian streets if there is no pedestrian zone.";

        public void OnEnabled()
        {
            HarmonyHelper.DoOnHarmonyReady(Patches.Patcher.PatchAll);
        }

        public void OnDisabled()
        {
            if (HarmonyHelper.IsHarmonyInstalled)
            {
                Patches.Patcher.UnpatchAll();
            }
        }

        public override void OnLevelLoaded(LoadMode mode)
        {
            base.OnLevelLoaded(mode);

            if (!Patches.Patcher.IsPatched)
            {
                Debug.LogWarning("[SometimesPedestrianStreets] Harmony patches not applied — skipping prefab modifications.");
                return;
            }

            try
            {
                PrefabModifier.ApplyLaneModifications();
            }
            catch (Exception e)
            {
                Debug.LogError("[SometimesPedestrianStreets] Failed to apply prefab modifications: " + e);
            }
        }

        public override void OnLevelUnloading()
        {
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
