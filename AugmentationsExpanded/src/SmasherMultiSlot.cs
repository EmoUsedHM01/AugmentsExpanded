using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MGSC;
using UnityEngine;
using Mod = AugmentationsExpanded.AugmentationsExpandedMod;

namespace AugmentationsExpanded
{
    internal static class SmasherMultiSlot
    {
        private const string HarmonyId = "Augmentations_Expanded.MultiSlot";
        private const string TagTextKey = "ae.tooltip.multislot";

        private static bool _patched;
        private static Mercenary _restoring;
        private static FieldInfo _builderFactory;

        /// <summary>
        /// Applies, once per launch, the patches for the augmentations tagged as Multi-WoundSlot
        /// (Data.ae_lose_augmentation_on_amputate in their json). After a mission the game
        /// destroys every augmentation with an amputated wound slot; while the general switch
        /// (ae.cfg.multislotloss.label) is off, the default, the amputated slots of a tagged
        /// augmentation are left out of that check, so they grow back with the augmentation still
        /// installed. The implants of the amputated slots stay lost; those of the other slots stay
        /// installed. The item tooltip of a tagged augmentation also gets the line "This item is
        /// tagged as Multi-WoundSlot". Each patch is applied on its own; the two restore patches do
        /// nothing without each other.
        /// </summary>
        /// <remarks>
        /// Input: No parameters. Reads the game's methods through reflection.
        /// Output: Nothing is returned. Patches AmputationSystem.GetAmputatedSlots,
        /// MercenarySystem.RestoreDefaultWoundSlotsIfAmputated and
        /// ItemTooltipBuilder.InitAugmentation; every patch that cannot be applied is logged and
        /// skipped.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        internal static void Patch()
        {
            if (_patched) return;
            _patched = true;

            _builderFactory = AccessTools.Field(typeof(ItemTooltipBuilder), "_factory");

            var harmony = new Harmony(HarmonyId);
            int ok = 0;
            ok += Apply(harmony, AccessTools.Method(typeof(AmputationSystem), nameof(AmputationSystem.GetAmputatedSlots)),
                        postfix: nameof(AmputatedSlotsPostfix));
            ok += Apply(harmony, AccessTools.Method(typeof(MercenarySystem), "RestoreDefaultWoundSlotsIfAmputated"),
                        prefix: nameof(RestorePrefix), finalizer: nameof(RestoreFinalizer));
            ok += Apply(harmony, AccessTools.Method(typeof(ItemTooltipBuilder), "InitAugmentation"),
                        postfix: nameof(TooltipPostfix));
            Debug.Log("[AugmentationsExpanded] multi-wound slot augmentations: " + ok + "/3 patches applied.");
        }

        /// <summary>
        /// Applies one patch.
        /// </summary>
        /// <param name="harmony">The Harmony instance of this class.</param>
        /// <param name="original">The game method to patch; null when it was not found.</param>
        /// <param name="prefix">Name of the prefix method of this class, or null.</param>
        /// <param name="postfix">Name of the postfix method of this class, or null.</param>
        /// <param name="finalizer">Name of the finalizer method of this class, or null.</param>
        /// <returns>1 when the patch was applied, 0 otherwise (logged).</returns>
        /// <remarks>
        /// Input: The parameters only.
        /// Output: The return value. Patches the method.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static int Apply(Harmony harmony, MethodBase original, string prefix = null,
                                 string postfix = null, string finalizer = null)
        {
            if (original == null)
            {
                Debug.LogError("[AugmentationsExpanded] multi-wound slot augmentations: a game method was "
                             + "not found, that part is disabled.");
                return 0;
            }
            try
            {
                harmony.Patch(original,
                              prefix:    Own(prefix),
                              postfix:   Own(postfix),
                              finalizer: Own(finalizer));
                return 1;
            }
            catch (Exception e)
            {
                Debug.LogError("[AugmentationsExpanded] multi-wound slot augmentations: patching "
                             + original.DeclaringType.Name + "." + original.Name + " failed: " + e);
                return 0;
            }
        }

        /// <summary>
        /// A patch method of this class, for Harmony.
        /// </summary>
        /// <param name="name">Method name, or null.</param>
        /// <returns>The Harmony wrapper, or null for a null name.</returns>
        /// <remarks>
        /// Input: The parameter only.
        /// Output: Only the return value.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static HarmonyMethod Own(string name)
        {
            return name == null ? null : new HarmonyMethod(AccessTools.Method(typeof(SmasherMultiSlot), name));
        }

        /// <summary>
        /// Before the game removes, after a mission, the augmentations with an amputated wound
        /// slot: remembers the clone, unless the general switch keeps the vanilla loss.
        /// </summary>
        /// <param name="mercenary">The clone back from the mission.</param>
        /// <remarks>
        /// Input: The parameter and the general switch.
        /// Output: Nothing is returned. Sets the clone read by AmputatedSlotsPostfix.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static void RestorePrefix(Mercenary mercenary)
        {
            _restoring = Mod._multiSlotLoss ? null : mercenary;
        }

        /// <summary>
        /// After the augmentations with an amputated wound slot are removed, also when it failed:
        /// forgets the clone, so the amputated slots are never filtered anywhere else.
        /// </summary>
        /// <param name="__exception">The exception thrown by the game method, if any.</param>
        /// <returns>The same exception, so the game handles it as before.</returns>
        /// <remarks>
        /// Input: The parameter.
        /// Output: The return value. Clears the remembered clone.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static Exception RestoreFinalizer(Exception __exception)
        {
            _restoring = null;
            return __exception;
        }

        /// <summary>
        /// After the game lists the amputated wound slots of a creature, only while the clone back
        /// from a mission is being restored: takes out the slots that belong to an augmentation
        /// tagged as Multi-WoundSlot, so the game does not destroy it. Every other call is left
        /// alone.
        /// </summary>
        /// <param name="effectsController">The effects of the creature the list is for.</param>
        /// <param name="__result">The list built by the game, changed in place.</param>
        /// <remarks>
        /// Input: The parameters, the remembered clone, its installed augmentations and the tags.
        /// Output: Nothing is returned. Removes entries from __result and writes one line in the
        /// log per slot that grows back.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static void AmputatedSlotsPostfix(EffectsController effectsController, List<string> __result)
        {
            Mercenary merc = _restoring;
            if (merc == null || __result == null || merc.CreatureData == null
                || effectsController != merc.CreatureData.EffectsController) return;
            try
            {
                for (int i = __result.Count - 1; i >= 0; i--)
                {
                    string augId;
                    if (!merc.CreatureData.AugmentationMap.TryGetValue(__result[i], out augId)
                        || !SmasherAugmentations.IsMultiSlot(augId)) continue;
                    Debug.Log("[AugmentationsExpanded] " + __result[i] + " of " + augId
                            + " grows back after the mission: the augmentation is kept.");
                    __result.RemoveAt(i);
                }
            }
            catch (Exception e) { Debug.LogError("[AugmentationsExpanded] multi-wound slot check: " + e.Message); }
        }

        /// <summary>
        /// After the augmentation part of an item tooltip is built: adds the line "This item is
        /// tagged as Multi-WoundSlot" for a tagged augmentation.
        /// </summary>
        /// <param name="__instance">The tooltip builder.</param>
        /// <param name="record">The augmentation record of the tooltip.</param>
        /// <remarks>
        /// Input: The parameters, the tags and the text ae.tooltip.multislot.
        /// Output: Nothing is returned. Adds one line to the tooltip.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static void TooltipPostfix(ItemTooltipBuilder __instance, AugmentationRecord record)
        {
            try
            {
                if (record == null || _builderFactory == null || !SmasherAugmentations.IsMultiSlot(record.Id)) return;
                var factory = _builderFactory.GetValue(__instance) as TooltipFactory;
                if (factory == null) return;
                factory.AddPanelToTooltip().SetMultilineName(Localization.Get(TagTextKey))
                       .SetNameColor(Colors.Yellow);
            }
            catch (Exception e) { Debug.LogError("[AugmentationsExpanded] multi-wound slot tooltip: " + e.Message); }
        }
    }
}
