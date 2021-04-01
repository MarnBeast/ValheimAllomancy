using BepInEx;
using HarmonyLib;
using System.Collections.Generic;
using ValheimMod.Util;

namespace ValheimMod
{
    [BepInPlugin("marnmods.AllomancyMod", "Marn's Valheim Allomancy Mod", "1.0.0")]
    [BepInProcess("valheim.exe")]
    public class AllomancyMod : BaseUnityPlugin
    {
        public const string AssetBundleName = "allomancy";
        private readonly Harmony harmony = new Harmony("marnmods.AllomancyMod");

        void Awake()
        {
            var assetBundle = AssetHelper.GetAssetBundleFromResources(AssetBundleName);
            Items.PewterItemData.Init(assetBundle);
            Items.MeadPewterItemData.Init(assetBundle);
            Items.MeadTinItemData.Init(assetBundle);
            harmony.PatchAll();
        }

        void OnDestroy()
        {
            harmony.UnpatchSelf();
        }

    }
}