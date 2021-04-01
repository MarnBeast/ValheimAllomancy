using ValheimMod.Util;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using ValheimLib;
using ValheimLib.ODB;

namespace ValheimMod.Items
{
    public static class PewterItemData
    {
        public static GameObject PewterPrefab;
        public static CustomItem CustomItem;
        public static CustomRecipe CustomRecipe;

        public const string PrefabPath = "Assets/CustomItems/Pewter.prefab";

        public const string TokenName = "$custom_item_pewter";
        public const string TokenValue = "Pewter";

        public const string TokenDescriptionName = "$custom_item_pewter_description";
        public const string TokenDescriptionValue = "Pewterarm Mistings consume this with Tasty Mead for increased strength, speed, and endurance.";

        public const string CraftingStationPrefabName = "forge";

        public const string TokenLanguage = "English";

        internal static void Init(AssetBundle assetBundle)
        {
            PewterPrefab = assetBundle.LoadAsset<GameObject>(PrefabPath);

            AddCustomRecipe();
            AddCustomItem();

            Language.AddToken(TokenName, TokenValue, TokenLanguage);
            Language.AddToken(TokenDescriptionName, TokenDescriptionValue, TokenLanguage);
        }

        /*
         * Private Methods
         */

        private static void AddCustomRecipe()
        {
            var recipe = ScriptableObject.CreateInstance<Recipe>();

            recipe.m_item = PewterPrefab.GetComponent<ItemDrop>();
            recipe.m_amount = 5;

            var neededResources = new List<Piece.Requirement>
            {
                MockRequirement.Create("Tin", 4),
                MockRequirement.Create("Copper", 1),
            };

            recipe.m_resources = neededResources.ToArray();
            recipe.m_craftingStation = Mock<CraftingStation>.Create(CraftingStationPrefabName);

            CustomRecipe = new CustomRecipe(recipe, fixReference : true, true);
            ObjectDBHelper.Add(CustomRecipe);
        }

        private static void AddCustomItem()
        {
            CustomItem = new CustomItem(PewterPrefab, fixReference : true);

            ObjectDBHelper.Add(CustomItem);
        }
    }
}
