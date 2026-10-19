using System;
using System.IO;
using MGSC;
using SimpleJSON;
using UnityEngine;

namespace AugmentationsExpanded
{
    public static class SmasherFiremodes
    {
        /// <summary>
        /// Loads every json under Assets/Firemodes as a firemode of the mod. In the game firemodes
        /// are presets shared by many weapons (disc_1, shotgun_2, punch_1, ...), and a weapon
        /// picks the presets it uses. When no preset has the values a limb weapon needs, the mod
        /// adds a new one to the same table, as the game does for the modes only one weapon uses.
        /// Must run before the weapons that list these firemodes. The folder is optional.
        /// </summary>
        /// <param name="modContentPath">Folder of the mod, the one that contains Assets. Null or
        /// empty loads nothing.</param>
        /// <returns>The number of firemodes added; 0 when the path or the folder is
        /// missing.</returns>
        /// <remarks>
        /// Input: The parameter, the json files under Assets/Firemodes and the vanilla firemodes
        /// they copy.
        /// Output: The return value. Adds the firemodes to the game's firemode table and writes
        /// lines in the log.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        public static int Register(string modContentPath)
        {
            if (string.IsNullOrEmpty(modContentPath)) return 0;

            string dir = Path.Combine(Path.Combine(modContentPath, "Assets"), "Firemodes");
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
                    Debug.LogError("[AugmentationsExpanded] firemode " + Path.GetFileName(file) + ": " + e);
                }
            }
            Debug.Log("[AugmentationsExpanded] firemodes registered: " + added);
            return added;
        }

        /// <summary>
        /// Creates one firemode as a copy of the vanilla firemode named in CloneFrom, then
        /// overrides the values present in the json: AmmoPerShot, WeaponCastsCount,
        /// RequiredAllAmmoToShot, Accuracy, DamageMult, ScatterAngle and DelayInSecsBetweenShots.
        /// The copy keeps the vanilla visual data, which holds the icon of the current weapon
        /// panel. The name shown in game is the localization key firemode.{id}.name.
        /// </summary>
        /// <param name="file">Full path of a json with a Data object holding at least Id and
        /// CloneFrom, the id of a vanilla firemode, e.g. "disc_1".</param>
        /// <returns>True when a new firemode was added; false without Id, for an id already in
        /// the game, or when CloneFrom is missing or unknown.</returns>
        /// <remarks>
        /// Input: The parameter, the json file and the vanilla firemode it copies.
        /// Output: The return value. Adds the firemode to the game's firemode table and writes a
        /// line in the log; throws on unreadable json, which the caller catches.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static bool LoadOne(string file)
        {
            JSONNode d = JSON.Parse(File.ReadAllText(file))["Data"];
            string id = d["Id"].Value;
            if (string.IsNullOrEmpty(id)) return false;

            if (Data.Firemodes.GetRecord(id) != null)
            {
                Debug.Log("[AugmentationsExpanded] firemode " + id + " already present, left as it is.");
                return false;
            }

            string source = d["CloneFrom"].Value;
            FireModeRecord src = string.IsNullOrEmpty(source) ? null : Data.Firemodes.GetRecord(source);
            if (src == null)
            {
                Debug.LogError("[AugmentationsExpanded] firemode " + id + ": CloneFrom '" + source + "' not found, skipped.");
                return false;
            }

            var rec = new FireModeRecord
            {
                AmmoPerShot             = Has(d, "AmmoPerShot")             ? d["AmmoPerShot"].AsInt               : src.AmmoPerShot,
                WeaponCastsCount        = Has(d, "WeaponCastsCount")        ? d["WeaponCastsCount"].AsInt          : src.WeaponCastsCount,
                RequiredAllAmmoToShot   = Has(d, "RequiredAllAmmoToShot")   ? d["RequiredAllAmmoToShot"].AsBool    : src.RequiredAllAmmoToShot,
                Accuracy                = Has(d, "Accuracy")                ? d["Accuracy"].AsFloat                : src.Accuracy,
                DamageMult              = Has(d, "DamageMult")              ? d["DamageMult"].AsFloat              : src.DamageMult,
                ScatterAngle            = Has(d, "ScatterAngle")            ? d["ScatterAngle"].AsFloat            : src.ScatterAngle,
                DelayInSecsBetweenShots = Has(d, "DelayInSecsBetweenShots") ? d["DelayInSecsBetweenShots"].AsFloat : src.DelayInSecsBetweenShots,
                ContentDescriptor       = src.ContentDescriptor
            };
            SmasherSprites.SetProperty(rec, "Id", id);

            Data.Firemodes.AddRecord(id, rec);
            Debug.Log("[AugmentationsExpanded] + firemode " + id + " (from " + source + ")"
                    + "  accuracy=" + rec.Accuracy + "  scatter=" + rec.ScatterAngle
                    + "  shots=" + rec.WeaponCastsCount + "  ammo/shot=" + rec.AmmoPerShot);
            return true;
        }

        /// <summary>
        /// Whether a key of the json is present and not null.
        /// </summary>
        /// <param name="d">The Data object of the firemode json.</param>
        /// <param name="key">The key, e.g. "Accuracy".</param>
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
