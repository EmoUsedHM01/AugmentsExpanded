using System;
using System.Collections.Generic;
using MGSC;
using UnityEngine;

namespace AugmentationsExpanded
{
    public static class SmasherCrafting
    {
        public const string SmasherChipOutput = "smasher_chip";
        public const string NachtChipOutput   = "nacht_chip";
        public const string Scout22ChipOutput = "scout22_chip";
        public const string NemesisChipOutput = "nemesis_chip";
        public const string OpiChipOutput     = "opi_chip";

        private static readonly List<ItemProduceReceipt> _modRecipes = new List<ItemProduceReceipt>();

        private static readonly Dictionary<ItemProduceReceipt, KeyValuePair<int, ItemQuantity>> _removedParts =
            new Dictionary<ItemProduceReceipt, KeyValuePair<int, ItemQuantity>>();

        /// <summary>
        /// Remembers a recipe registered by the mod, so the hand and foot ingredient switch only
        /// ever touches the mod's own recipes.
        /// </summary>
        /// <param name="recipe">A recipe just added to the game. Null is ignored.</param>
        /// <remarks>
        /// Input: The parameter only.
        /// Output: Nothing is returned. Adds the recipe to the list, once.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        public static void Track(ItemProduceReceipt recipe)
        {
            if (recipe != null && !_modRecipes.Contains(recipe)) _modRecipes.Add(recipe);
        }

        /// <summary>
        /// Puts the hand and the foot back into, or takes them out of, the recipes of the mod's
        /// sets: the arm (shoulder and arm slots) normally needs the hand of the same set (arm slot
        /// only), and the leg (thigh and feet slots) needs the foot of the same set (feet slot
        /// only), like the vanilla limbs. Removed ingredients are remembered and put back in their
        /// old position, so the switch can be flipped any number of times.
        /// </summary>
        /// <param name="required">True: the arm and leg recipes need the hand and the foot (the
        /// json recipes). False: the hand and the foot are removed from them.</param>
        /// <remarks>
        /// Input: The parameter, the mod's recipes and the item and wound slot records.
        /// Output: Nothing is returned. Changes the ingredients of the arm and leg recipes and
        /// writes a line in the log.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        public static void SetHandFootRequired(bool required)
        {
            int changed = 0;
            foreach (ItemProduceReceipt r in _modRecipes)
            {
                if (r == null || r.RequiredItems == null) continue;
                KeyValuePair<int, ItemQuantity> removed;
                bool isRemoved = _removedParts.TryGetValue(r, out removed);
                if (required && isRemoved)
                {
                    r.RequiredItems.Insert(Mathf.Clamp(removed.Key, 0, r.RequiredItems.Count), removed.Value);
                    _removedParts.Remove(r);
                    changed++;
                }
                else if (!required && !isRemoved)
                {
                    int at = HandOrFootIndex(r);
                    if (at < 0) continue;
                    _removedParts[r] = new KeyValuePair<int, ItemQuantity>(at, r.RequiredItems[at]);
                    r.RequiredItems.RemoveAt(at);
                    changed++;
                }
            }
            Debug.Log("[AugmentationsExpanded] hand and foot required in arm and leg recipes: "
                    + (required ? "on" : "off") + " (" + changed + " recipes changed, "
                    + _removedParts.Count + " without hand or foot).");
        }

        /// <summary>
        /// Position of the hand or the foot among the ingredients of an arm or leg recipe: an
        /// augmentation with a single wound slot of type Arm or Feet that is also one of the slots
        /// of the recipe's output.
        /// </summary>
        /// <param name="r">A recipe of the mod.</param>
        /// <returns>The index in RequiredItems, or -1 when the output is not a multi-slot
        /// augmentation or no ingredient is its hand or foot.</returns>
        /// <remarks>
        /// Input: The parameter and the item and wound slot records.
        /// Output: Only the return value.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static int HandOrFootIndex(ItemProduceReceipt r)
        {
            AugmentationRecord output = Augmentation(r.OutputItem);
            if (output == null || output.WoundSlotIds == null || output.WoundSlotIds.Count < 2) return -1;
            for (int i = 0; i < r.RequiredItems.Count; i++)
            {
                AugmentationRecord part = Augmentation(r.RequiredItems[i].ItemId);
                if (part == null || part.WoundSlotIds == null || part.WoundSlotIds.Count != 1) continue;
                string slot = part.WoundSlotIds[0];
                if (!output.WoundSlotIds.Contains(slot)) continue;
                WoundSlotRecord ws = Data.WoundSlots.GetRecord(slot);
                if (ws != null && (ws.SlotType == "Arm" || ws.SlotType == "Feet")) return i;
            }
            return -1;
        }

        /// <summary>
        /// The augmentation record of an item, without the exception GetSimpleRecord throws on
        /// an unknown id.
        /// </summary>
        /// <param name="id">Item id. Null or empty is accepted.</param>
        /// <returns>The record, or null when the id is unknown or not an augmentation.</returns>
        /// <remarks>
        /// Input: The parameter and the items in the game data.
        /// Output: Only the return value.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static AugmentationRecord Augmentation(string id)
        {
            if (string.IsNullOrEmpty(id) || Data.Items.GetRecord(id) == null) return null;
            var composite = Data.Items.GetRecord(id) as CompositeItemRecord;
            return composite == null ? null : composite.GetRecord<AugmentationRecord>();
        }

        /// <summary>
        /// Makes a chip conversion recipe (vanilla augmentation chip into set chip) available or
        /// not, both for new campaigns and for the campaign in progress. Recipes have no tech
        /// level, so these are unlocked from the start and controlled by the mod's switches
        /// instead.
        /// </summary>
        /// <param name="output">Id of the set chip the recipe produces: "smasher_chip",
        /// "nacht_chip", "scout22_chip", "nemesis_chip" or "opi_chip".</param>
        /// <param name="enabled">True to make the recipe available, false to hide it.</param>
        /// <remarks>
        /// Input: The parameters only.
        /// Output: Nothing is returned. Changes the starting recipes and the unlocked recipes of
        /// the campaign in progress.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        public static void SetEnabled(string output, bool enabled)
        {
            if (enabled) { AddToStartReceipts(output); UnlockInCurrentGame(output); }
            else         { RemoveFromStartReceipts(output); LockInCurrentGame(output); }
        }

        /// <summary>
        /// Removes a recipe from those unlocked when a new campaign starts.
        /// </summary>
        /// <param name="output">Id of the item the recipe produces, e.g. "smasher_chip".</param>
        /// <remarks>
        /// Input: The parameter and the game's starting recipes.
        /// Output: Nothing is returned. Changes the starting recipes; logs when something was
        /// removed.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        public static void RemoveFromStartReceipts(string output)
        {
            if (Data.Global == null || Data.Global.StartReceipts == null) return;
            if (Data.Global.StartReceipts.Remove(output))
                Debug.Log("[AugmentationsExpanded] " + output + " removed from the starting recipes.");
        }

        /// <summary>
        /// Removes a recipe from the unlocked production items of the running campaign, so it
        /// disappears from production without a restart.
        /// </summary>
        /// <param name="output">Id of the item the recipe produces, e.g. "smasher_chip".</param>
        /// <remarks>
        /// Input: The parameter and the campaign in progress, if any.
        /// Output: Nothing is returned. Changes the unlocked recipes of the campaign; does nothing
        /// outside a campaign.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        public static void LockInCurrentGame(string output)
        {
            try
            {
                var mode = SingletonMonoBehaviour<SpaceGameMode>.Instance;
                if (mode == null) return;
                MagnumCargo cargo = mode.Get<MagnumCargo>();
                if (cargo == null || cargo.UnlockedProductionItems == null) return;
                if (cargo.UnlockedProductionItems.Remove(output))
                    Debug.Log("[AugmentationsExpanded] " + output + " locked again in the current game.");
            }
            catch (Exception e)
            {
                Debug.LogError("[AugmentationsExpanded] locking the conversion recipe: " + e);
            }
        }

        /// <summary>
        /// Adds a recipe to those unlocked when a new campaign starts. The game reads that list
        /// only when a campaign is created.
        /// </summary>
        /// <param name="output">Id of the item the recipe produces, e.g. "smasher_chip".</param>
        /// <remarks>
        /// Input: The parameter and the game's starting recipes.
        /// Output: Nothing is returned. Changes the starting recipes; logs when something was
        /// added.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        public static void AddToStartReceipts(string output)
        {
            if (Data.Global == null || Data.Global.StartReceipts == null)
            {
                Debug.LogWarning("[AugmentationsExpanded] StartReceipts not available.");
                return;
            }
            if (Data.Global.StartReceipts.Contains(output)) return;

            Data.Global.StartReceipts.Add(output);
            Debug.Log("[AugmentationsExpanded] " + output + " added to the starting recipes.");
        }

        /// <summary>
        /// Adds a recipe to the unlocked production items of the running campaign, for saves
        /// started before the mod was installed.
        /// </summary>
        /// <param name="output">Id of the item the recipe produces, e.g. "smasher_chip".</param>
        /// <remarks>
        /// Input: The parameter and the campaign in progress, if any.
        /// Output: Nothing is returned. Changes the unlocked recipes of the campaign; does nothing
        /// outside a campaign.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        public static void UnlockInCurrentGame(string output)
        {
            try
            {
                var mode = SingletonMonoBehaviour<SpaceGameMode>.Instance;
                if (mode == null) return;

                MagnumCargo cargo = mode.Get<MagnumCargo>();
                if (cargo == null || cargo.UnlockedProductionItems == null) return;
                if (cargo.UnlockedProductionItems.Contains(output)) return;

                cargo.UnlockedProductionItems.Add(output);
                Debug.Log("[AugmentationsExpanded] " + output + " unlocked in the current game.");
            }
            catch (Exception e)
            {
                Debug.LogError("[AugmentationsExpanded] unlocking the conversion recipe: " + e);
            }
        }
    }
}
