using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using MGSC;
using SimpleJSON;
using UnityEngine;

namespace AugmentationsExpanded
{
    public static class SmasherAmmo
    {
        /// <summary>
        /// Loads every json under Assets/Ammo as an ammo record of the mod. A melee weapon takes
        /// the damage type, crit chance and traits of each firemode from the implicit ammo listed
        /// in its OverrideAmmo, and in the game those ammo records are presets shared by many
        /// weapons (implicted_knife, implicted_combat_knife, ...). When no preset has the values a
        /// limb weapon needs, the mod adds a new one, as the game does. Must run before the
        /// weapons that use them. The folder is optional.
        /// </summary>
        /// <param name="modContentPath">Folder of the mod, the one that contains Assets. Null or
        /// empty loads nothing.</param>
        /// <returns>The number of ammo records added; 0 when the path or the folder is
        /// missing.</returns>
        /// <remarks>
        /// Input: The parameter, the json files under Assets/Ammo and the vanilla ammo they copy.
        /// Output: The return value. Adds the ammo to the game items and writes lines in the log.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        public static int Register(string modContentPath)
        {
            if (string.IsNullOrEmpty(modContentPath)) return 0;

            string dir = Path.Combine(Path.Combine(modContentPath, "Assets"), "Ammo");
            if (!Directory.Exists(dir)) return 0;

            int added = 0;
            foreach (string file in Directory.GetFiles(dir, "*.json", SearchOption.AllDirectories))
            {
                try
                {
                    if (LoadOne(file)) added++;
                }
                catch (Exception e)
                {
                    Debug.LogError("[AugmentationsExpanded] ammo " + Path.GetFileName(file) + ": " + e);
                }
            }
            Debug.Log("[AugmentationsExpanded] ammo registered: " + added);
            return added;
        }

        /// <summary>
        /// Creates one ammo record as a copy of the vanilla ammo named in CloneFrom, then
        /// overrides the values present in the json: DmgType, DmgCritChance, AccuracyMult,
        /// ScatterMult, DamageMult, RangeBonus and Traits. The copy keeps the vanilla visual
        /// data. The id should contain "implicted_", which is how the game recognizes the
        /// implicit ammo of melee weapons and limbs.
        /// </summary>
        /// <param name="file">Full path of a json with a Data object holding at least Id and
        /// CloneFrom, the id of a vanilla ammo, e.g. "implicted_combat_knife".</param>
        /// <returns>True when a new ammo record was added; false without Id, for an id already
        /// in the game, or when CloneFrom is missing or not an ammo.</returns>
        /// <remarks>
        /// Input: The parameter, the json file and the vanilla ammo it copies.
        /// Output: The return value. Adds the ammo to the game items and writes a line in the
        /// log; throws on unreadable json, which the caller catches.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static bool LoadOne(string file)
        {
            JSONNode d = JSON.Parse(File.ReadAllText(file))["Data"];
            string id = d["Id"].Value;
            if (string.IsNullOrEmpty(id)) return false;

            if (Data.Items.GetRecord(id) != null)
            {
                Debug.Log("[AugmentationsExpanded] ammo " + id + " already present, left as it is.");
                return false;
            }

            string source = d["CloneFrom"].Value;
            if (string.IsNullOrEmpty(source) || Data.Items.GetRecord(source) == null)
            {
                Debug.LogError("[AugmentationsExpanded] ammo " + id + ": CloneFrom '" + source + "' not found, skipped.");
                return false;
            }
            AmmoRecord src = Data.Items.GetSimpleRecord<AmmoRecord>(source);
            if (src == null)
            {
                Debug.LogError("[AugmentationsExpanded] ammo " + id + ": '" + source + "' is not an ammo, skipped.");
                return false;
            }

            AmmoRecord rec = Copy(src);
            SmasherSprites.SetProperty(rec, "Id", id);
            rec.Categories  = new List<string>();
            rec.Disassembly = new List<ItemQuantity>();
            rec.Traits      = d["Traits"].IsArray ? SmasherJson.Strings(d["Traits"])
                                                  : new List<string>(src.Traits ?? new List<string>());

            if (!string.IsNullOrEmpty(d["DmgType"].Value)) rec.DmgType = d["DmgType"].Value;
            if (Has(d, "DmgCritChance")) rec.DmgCritChance = d["DmgCritChance"].AsFloat;
            if (Has(d, "AccuracyMult"))  rec.AccuracyMult  = d["AccuracyMult"].AsFloat;
            if (Has(d, "ScatterMult"))   rec.ScatterMult   = d["ScatterMult"].AsFloat;
            if (Has(d, "DamageMult"))    rec.DamageMult    = d["DamageMult"].AsFloat;
            if (Has(d, "RangeBonus"))    rec.RangeBonus    = d["RangeBonus"].AsInt;

            Data.Items.AddRecord(id, rec);
            Debug.Log("[AugmentationsExpanded] + ammo " + id + " (from " + source + ")  " + rec.DmgType
                    + "  crit=" + rec.DmgCritChance + "  traits=[" + string.Join(" ", rec.Traits.ToArray()) + "]");
            return true;
        }

        /// <summary>
        /// Field by field copy of an ammo record. AmmoRecord has no Clone method, so the object
        /// is copied with MemberwiseClone; lists stay shared with the source until the caller
        /// replaces them.
        /// </summary>
        /// <param name="src">The vanilla ammo record.</param>
        /// <returns>A new AmmoRecord with the same values and the same visual data.</returns>
        /// <remarks>
        /// Input: The parameter only.
        /// Output: Only the return value; the source is not changed.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static AmmoRecord Copy(AmmoRecord src)
        {
            MethodInfo clone = typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic);
            return (AmmoRecord)clone.Invoke(src, null);
        }

        /// <summary>
        /// Whether a key of the json is present and not null.
        /// </summary>
        /// <param name="d">The Data object of the ammo json.</param>
        /// <param name="key">The key, e.g. "DmgCritChance".</param>
        /// <returns>True when the json sets that value; false when the copied value must be
        /// kept.</returns>
        /// <remarks>
        /// Input: The parameters only.
        /// Output: Only the return value.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static bool Has(JSONNode d, string key)
        {
            return d[key] != null && !d[key].IsNull;
        }
    }
}
