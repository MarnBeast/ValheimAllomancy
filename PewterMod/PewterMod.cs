using BepInEx;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace ValheimMod
{
    [BepInPlugin("marnmods.PewterMod", "Marn's Valheim Pewter Mod", "1.0.0")]
    [BepInProcess("valheim.exe")]
    public class PewterMod : BaseUnityPlugin
    {
        private readonly Harmony harmony = new Harmony("marnmods.PewterMod");

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