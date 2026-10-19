using System;
using System.Collections.Generic;
using MGSC;
using UnityEngine;

namespace AugmentationsExpanded
{
    public static class SmasherMapDrop
    {
        private const string VanillaChip = "augment_chip";
        private const float Ratio = 0.10f;

        private static readonly HashSet<string> _applied = new HashSet<string>();

        /// <summary>
        /// Removes a set chip from every map container loot table.
        /// </summary>
        /// <param name="chipId">Id of the set chip, e.g. "nacht_chip".</param>
        /// <remarks>
        /// Input: The parameter and the game's map container loot tables.
        /// Output: Nothing is returned. Removes every row of that chip and writes a line in the
        /// log.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        public static void Remove(string chipId)
        {
            if (Data.ContainerItemDrop == null) return;
            int removed = 0;
            try
            {
                foreach (string container in new List<string>(Data.ContainerItemDrop.ContainerIds))
                    foreach (string biome in new List<string>(Data.ContainerItemDrop.GetDropBiomes(container)))
                    {
                        var rows = Data.ContainerItemDrop.GetDrop(container, biome);
                        if (rows == null) continue;
                        removed += rows.RemoveAll(r => r.Item2 == chipId);
                    }
            }
            catch (Exception e) { Debug.LogError("[AugmentationsExpanded] removing map drops: " + e); return; }
            _applied.Remove(chipId);
            Debug.Log("[AugmentationsExpanded] " + chipId + " removed from " + removed + " container tables.");
        }

        /// <summary>
        /// Adds a set chip to every map container loot table that contains the vanilla augmentation
        /// chip, at 10% of its weight (at least 1, since weights are whole numbers). The tables are
        /// read at run time, so containers added by other mods are included. The game still filters
        /// the rows by the raid's tech level, so the chip's tech level applies.
        /// </summary>
        /// <param name="chipId">Id of a registered set chip, e.g. "nacht_chip". Null or empty does
        /// nothing.</param>
        /// <remarks>
        /// Input: The parameter and the game's map container loot tables.
        /// Output: Nothing is returned. Adds one row per container and biome, once; a second call
        /// without Remove does nothing.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        public static void Apply(string chipId)
        {
            if (string.IsNullOrEmpty(chipId) || _applied.Contains(chipId)) return;
            if (Data.ContainerItemDrop == null) { Debug.LogWarning("[AugmentationsExpanded] ContainerItemDrop is null."); return; }

            int added = 0;
            try
            {
                var containers = new List<string>(Data.ContainerItemDrop.ContainerIds);
                foreach (string container in containers)
                {
                    var biomes = new List<string>(Data.ContainerItemDrop.GetDropBiomes(container));
                    foreach (string biome in biomes)
                    {
                        List<Tuple<float, string>> rows = Data.ContainerItemDrop.GetDrop(container, biome);
                        if (rows == null) continue;

                        float weight = 0f;
                        bool alreadyThere = false;
                        foreach (var r in rows)
                        {
                            if (r.Item2 == VanillaChip) weight = Mathf.Max(weight, r.Item1);
                            else if (r.Item2 == chipId) alreadyThere = true;
                        }
                        if (weight <= 0f || alreadyThere) continue;

                        int ours = Mathf.Max(1, Mathf.FloorToInt(weight * Ratio));
                        Data.ContainerItemDrop.Add(container, biome, chipId + " " + ours);
                        added++;
                        Debug.Log("[AugmentationsExpanded] " + chipId + " in " + container + "/" + biome
                                + " weight " + ours + " (augment_chip " + weight + ")");
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[AugmentationsExpanded] map drops: " + e);
                return;
            }

            _applied.Add(chipId);
            Debug.Log("[AugmentationsExpanded] " + chipId + " added to " + added
                    + " container tables (10% of the " + VanillaChip + " weight).");
        }
    }
}
