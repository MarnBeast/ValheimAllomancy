using BepInEx;
using HarmonyLib;
using UnityEngine;

namespace ValheimMod
{
    [BepInPlugin("marnmods.JumpMod", "Marn's Valheim Jump Mod", "1.0.0")]
    [BepInProcess("valheim.exe")]
    public class JumpMod : BaseUnityPlugin
    {
        private readonly Harmony harmony = new Harmony("marnmods.JumpMod");

        void Awake()
        {
            harmony.PatchAll();
        }

        [HarmonyPatch(typeof(Character), nameof(Character.Jump))]
        class Jump_Patch
        {
            static void Prefix(ref float ___m_jumpForce)
            {
                Debug.Log($"Jump force: {___m_jumpForce}");
                ___m_jumpForce = 15;
                Debug.Log($"Modified jump force: {___m_jumpForce}");
            }
        }
    }
}