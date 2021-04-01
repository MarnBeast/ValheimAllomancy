using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using ValheimLib;
using ValheimLib.ODB;

namespace ValheimMod.Items
{
    [HarmonyPatch]
    public static class MeadTinItemData
    {
        public static GameObject MeadTinPrefab;
        public static CustomItem CustomItem;
        public static CustomRecipe CustomRecipe;

        public const string PrefabPath = "Assets/CustomItems/MeadTin.prefab";

        public const string TokenName = "$custom_item_mead_tin";
        public const string TokenValue = "Tin Mead";

        public const string TokenDescriptionName = "$custom_item_mead_tin_description";
        public const string TokenDescriptionValue = "Tineye Mistings can consume this for enhanced perception.";

        public const string TokenStatusName = "$custom_se_mead_tin";
        public const string TokenStatusValue = "Tineye";

        public const string TokenStatusTooltipName = "$custom_se_mead_tin_tooltip";
        public const string TokenStatusTooltipValue = "Enhanced sensory perception lets you easily see and hear everything around you.";

        public const string CraftingStationPrefabName = "forge";

        public const string TokenLanguage = "English";



        
        [HarmonyPrefix]
        [HarmonyPatch(typeof(EnemyHud), "UpdateHuds")]
        private static void TineyeHuds(ref EnemyHud __instance, Player player, float dt)
        {
            if (!(bool)player || !(bool)__instance)
            {
                return;
            }
            if (player.GetSEMan().HaveStatusEffect("Tineye"))
            {
                if (__instance.m_hoverShowDuration != float.MaxValue)
                {
                    __instance.m_hoverShowDuration = float.MaxValue;
                    Debug.Log($"Modified Hover Show Duration: {__instance.m_hoverShowDuration}");
                }
            }
            else if (__instance.m_hoverShowDuration == float.MaxValue)
            {
                __instance.m_hoverShowDuration = 60f;
                Debug.Log($"Hover Show Duration: {__instance.m_hoverShowDuration}");
            }
        }

        internal static void Init(AssetBundle assetBundle)
        {
            MeadTinPrefab = assetBundle.LoadAsset<GameObject>(PrefabPath);

            AddCustomRecipe();
            AddCustomItem();

            Language.AddToken(TokenName, TokenValue, TokenLanguage);
            Language.AddToken(TokenDescriptionName, TokenDescriptionValue, TokenLanguage);
            Language.AddToken(TokenStatusName, TokenStatusValue, TokenLanguage);
            Language.AddToken(TokenStatusTooltipName, TokenStatusTooltipValue, TokenLanguage);
        }

        /*
         * Private Methods
         */

        private static void AddCustomRecipe()
        {
            var recipe = ScriptableObject.CreateInstance<Recipe>();

            recipe.m_item = MeadTinPrefab.GetComponent<ItemDrop>();
            recipe.m_amount = 10;

            var neededResources = new List<Piece.Requirement>
            {
                MockRequirement.Create("Tin", 1),
                MockRequirement.Create("MeadTasty", 2),
            };

            recipe.m_resources = neededResources.ToArray();
            recipe.m_craftingStation = Mock<CraftingStation>.Create(CraftingStationPrefabName);

            CustomRecipe = new CustomRecipe(recipe, fixReference : true, true);
            ObjectDBHelper.Add(CustomRecipe);
        }

        private static void AddCustomItem()
        {
            CustomItem = new CustomItem(MeadTinPrefab, fixReference : true);

            ObjectDBHelper.Add(CustomItem);
        }
    }
}
