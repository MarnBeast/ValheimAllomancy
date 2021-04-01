using System.Collections.Generic;
using UnityEngine;
using ValheimLib;
using ValheimLib.ODB;

namespace ValheimMod.Items
{
    public static class MeadPewterItemData
    {
        public static GameObject MeadPewterPrefab;
        public static CustomItem CustomItem;
        public static CustomRecipe CustomRecipe;

        public const string PrefabPath = "Assets/CustomItems/MeadPewter.prefab";

        public const string TokenName = "$custom_item_mead_pewter";
        public const string TokenValue = "Pewter Mead";

        public const string TokenDescriptionName = "$custom_item_mead_pewter_description";
        public const string TokenDescriptionValue = "Pewterarm Mistings can consume this for increased strength and endurance.";

        public const string TokenStatusName = "$custom_se_mead_pewter";
        public const string TokenStatusValue = "Pewterarm";

        public const string TokenStatusTooltipName = "$custom_se_mead_pewter_tooltip";
        public const string TokenStatusTooltipValue = "Increased physical abilities, such as stamina, carry weight, jump, and attack damage.";

        public const string CraftingStationPrefabName = "forge";

        public const string TokenLanguage = "English";


        internal static void Init(AssetBundle assetBundle)
        {
            MeadPewterPrefab = assetBundle.LoadAsset<GameObject>(PrefabPath);

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

            recipe.m_item = MeadPewterPrefab.GetComponent<ItemDrop>();
            recipe.m_amount = 10;

            var neededResources = new List<Piece.Requirement>
            {
                MockRequirement.Create("Pewter", 1),
                MockRequirement.Create("MeadTasty", 2),
            };

            recipe.m_resources = neededResources.ToArray();
            recipe.m_craftingStation = Mock<CraftingStation>.Create(CraftingStationPrefabName);

            CustomRecipe = new CustomRecipe(recipe, fixReference : true, true);
            ObjectDBHelper.Add(CustomRecipe);
        }

        private static void AddCustomItem()
        {
            CustomItem = new CustomItem(MeadPewterPrefab, fixReference : true);

            ObjectDBHelper.Add(CustomItem);
        }
    }
}
