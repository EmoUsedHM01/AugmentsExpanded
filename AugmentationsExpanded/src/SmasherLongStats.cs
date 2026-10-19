using System;
using System.Reflection;
using HarmonyLib;
using MGSC;
using UnityEngine;

namespace AugmentationsExpanded
{
    internal static class SmasherLongStats
    {
        private const string HarmonyId = "Augmentations_Expanded.LongStats";
        private const string TextKey   = "ae.tooltip.longstats";

        private static bool _patched;
        private static bool _hideNext;

        /// <summary>
        /// Applies, once per launch, the patches that shorten the item tooltip of the augmentations
        /// whose json has "ae_kill_long_stats": "On" in Data: their effect list (bonuses, penalties
        /// and resistances of all their wound slots) is replaced by one line. The rest of the
        /// tooltip and the tooltips of the wound slots on the clone stay as they are; every other
        /// item is left to the game. The list patch is applied first: alone it does nothing.
        /// </summary>
        /// <remarks>
        /// Input: No parameters. Reads the game's methods through reflection.
        /// Output: Nothing is returned. Patches ItemTooltipBuilder.InitAugmentation and
        /// TooltipFactory.AddEffectsBlock; when a method is missing or the patch fails, it is
        /// logged and the tooltips stay as in the game.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        internal static void Patch()
        {
            if (_patched) return;
            _patched = true;

            MethodBase init  = AccessTools.Method(typeof(ItemTooltipBuilder), "InitAugmentation");
            MethodBase block = AccessTools.Method(typeof(TooltipFactory), nameof(TooltipFactory.AddEffectsBlock));
            if (init == null || block == null)
            {
                Debug.LogError("[AugmentationsExpanded] long effect lists: a game method was not found, "
                             + "the tooltips stay as in the game.");
                return;
            }
            try
            {
                var harmony = new Harmony(HarmonyId);
                harmony.Patch(block, prefix: Own(nameof(BlockPrefix)));
                harmony.Patch(init, prefix: Own(nameof(InitPrefix)), finalizer: Own(nameof(InitFinalizer)));
                Debug.Log("[AugmentationsExpanded] long effect lists: patches applied.");
            }
            catch (Exception e)
            {
                Debug.LogError("[AugmentationsExpanded] long effect lists: patching failed, "
                             + "the tooltips stay as in the game: " + e);
            }
        }

        /// <summary>
        /// A patch method of this class, for Harmony.
        /// </summary>
        /// <param name="name">Method name.</param>
        /// <returns>The Harmony wrapper.</returns>
        /// <remarks>
        /// Input: The parameter only.
        /// Output: Only the return value.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static HarmonyMethod Own(string name)
        {
            return new HarmonyMethod(AccessTools.Method(typeof(SmasherLongStats), name));
        }

        /// <summary>
        /// Before the augmentation part of an item tooltip is built: remembers whether the effect
        /// list of this augmentation must be hidden.
        /// </summary>
        /// <param name="record">The augmentation record of the tooltip.</param>
        /// <remarks>
        /// Input: The parameter and the flags read from the mod's json.
        /// Output: Nothing is returned. Sets the flag read by BlockPrefix.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static void InitPrefix(AugmentationRecord record)
        {
            _hideNext = record != null && SmasherAugmentations.HidesEffectList(record.Id);
        }

        /// <summary>
        /// After the augmentation part of an item tooltip is built, also when it failed: clears
        /// the flag, so no other tooltip is touched.
        /// </summary>
        /// <param name="__exception">The exception thrown by the game method, if any.</param>
        /// <returns>The same exception, so the game handles it as before.</returns>
        /// <remarks>
        /// Input: The parameter.
        /// Output: The return value. Clears the flag.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static Exception InitFinalizer(Exception __exception)
        {
            _hideNext = false;
            return __exception;
        }

        /// <summary>
        /// Before the game adds an effect list to a tooltip: when the flag is set, adds the
        /// placeholder line instead and skips the list. In every other case the game draws the
        /// list as usual, also when adding the line fails.
        /// </summary>
        /// <param name="__instance">The tooltip factory.</param>
        /// <returns>False to skip the game's list, true to let the game draw it.</returns>
        /// <remarks>
        /// Input: The parameter, the flag and the text ae.tooltip.longstats.
        /// Output: The return value. Clears the flag; adds one line to the tooltip when it was set.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static bool BlockPrefix(TooltipFactory __instance)
        {
            if (!_hideNext) return true;
            _hideNext = false;
            try
            {
                __instance.AddPanelToTooltip().SetMultilineName(Localization.Get(TextKey))
                          .SetNameColor(Colors.AltGreen);
                return false;
            }
            catch (Exception e)
            {
                Debug.LogError("[AugmentationsExpanded] long effect list: " + e.Message);
                return true;
            }
        }
    }
}
