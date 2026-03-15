using System;
using CitiesHarmony.API;
using ICities;
using PrecisionEngineering.Patches;

namespace PrecisionEngineering
{
    public class LoadingExtension : LoadingExtensionBase
    {
        public override void OnCreated(ILoading loading)
        {
            base.OnCreated(loading);
        }

        public override void OnLevelLoaded(LoadMode mode)
        {
            base.OnLevelLoaded(mode);

            try
            {
                Debug.Log("OnLevelLoaded");

                Manager.OnLevelLoaded();

                if (HarmonyHelper.IsHarmonyInstalled)
                {
                    Patcher.PatchAll();
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Error during OnLevelLoaded callback");
                Debug.LogError(e.ToString());
            }
        }

        public override void OnLevelUnloading()
        {
            try
            {
                Debug.Log("OnLevelUnloading");

                if (HarmonyHelper.IsHarmonyInstalled)
                {
                    Patcher.UnpatchAll();
                }

                Manager.OnLevelUnloaded();
            }
            catch (Exception e)
            {
                Debug.LogError("Error during OnLevelUnloading callback");
                Debug.LogError(e.ToString());
            }

            base.OnLevelUnloading();
        }
    }
}
