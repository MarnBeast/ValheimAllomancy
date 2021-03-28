using BepInEx;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace ValheimMod
{
    [BepInPlugin("marnmods.AllomancyMod", "Marn's Valheim Allomancy Mod", "1.0.0")]
    [BepInProcess("valheim.exe")]
    public class AllomancyMod : BaseUnityPlugin
    {
        private readonly Harmony harmony = new Harmony("marnmods.AllomancyMod");

        void Awake()
        {
            Items.Pewter.PewterItemData.Init();
            harmony.PatchAll();
        }

        void OnDestroy()
        {
            harmony.UnpatchSelf();
        }
    }
}