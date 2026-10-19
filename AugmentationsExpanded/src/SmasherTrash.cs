using System;
using System.Collections.Generic;
using System.IO;
using MGSC;
using SimpleJSON;
using UnityEngine;

namespace AugmentationsExpanded
{
    public static class SmasherTrash
    {
        public const string BoxId        = "smasher_box";
        public const string NachtBoxId   = "nacht_box";
        public const string Scout22BoxId = "scout22_box";
        public const string AndrasBoxId  = "andras_box";
        public const string NemesisBoxId = "nemesis_box";

        private static readonly HashSet<string> _loaded = new HashSet<string>();

        private static readonly Dictionary<string, List<string>> _categories =
            new Dictionary<string, List<string>>();

        private static readonly Dictionary<string, string> _sets =
            new Dictionary<string, string>();

        /// <summary>
        /// Empties or restores the Categories of the crate of a set. Empty Categories keep it out
        /// of the generic faction loot; its reward row is handled separately.
        /// </summary>
        /// <param name="set">Set name, i.e. the subfolder name under Assets: "Smasher",
        /// "Nachtreiher", "Scout22", "Andras", "Nemesis", "OPI", "Setless" or "WeaponLimbs". An
        /// unknown name changes nothing.</param>
        /// <param name="enabled">True to restore the tags, false to empty them.</param>
        /// <remarks>
        /// Input: The parameters and the tags kept from the json of each crate.
        /// Output: Nothing is returned. Changes the Categories of the set's crate.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        public static void SetSpawnEnabled(string set, bool enabled)
        {
            foreach (var kv in _categories)
            {
                string owner;
                if (!_sets.TryGetValue(kv.Key, out owner) || owner != set) continue;

                var rec = Data.Items.GetSimpleRecord<ItemRecord>(kv.Key);
                if (rec == null) continue;
                rec.Categories = enabled ? new List<string>(kv.Value) : new List<string>();
            }
        }

        /// <summary>
        /// Sets the tech level of a crate, the one shown on its tooltip. The json value is only the
        /// default.
        /// </summary>
        /// <param name="id">Id of a crate loaded by the mod, e.g. "smasher_box". Unknown ids are
        /// ignored.</param>
        /// <param name="techLevel">Tech level, expected 1 to 10.</param>
        /// <remarks>
        /// Input: The parameters only.
        /// Output: Nothing is returned. Changes the crate's TechLevel.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        public static void SetTechLevel(string id, int techLevel)
        {
            if (!_loaded.Contains(id)) return;
            var rec = Data.Items.GetSimpleRecord<ItemRecord>(id);
            if (rec != null) rec.TechLevel = techLevel;
        }

        /// <summary>
        /// Loads every json under Assets/Trash (one subfolder per set) as a crate: a container item
        /// modeled on the vanilla robot parts container, which gives the set pieces listed in its
        /// Disassembly when taken apart.
        /// </summary>
        /// <param name="modContentPath">Folder of the mod, the one that contains Assets. Null or
        /// empty loads nothing.</param>
        /// <returns>The number of crates added; 0 when the path or the folder is missing.</returns>
        /// <remarks>
        /// Input: The parameter, the json files under Assets/Trash and their images.
        /// Output: The return value. Adds the records to the game items and remembers tags and set
        /// of each.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        public static int Register(string modContentPath)
        {
            if (string.IsNullOrEmpty(modContentPath)) return 0;

            string assets = Path.Combine(modContentPath, "Assets");
            string dir    = Path.Combine(assets, "Trash");
            if (!Directory.Exists(dir))
            {
                Debug.LogWarning("[AugmentationsExpanded] Trash folder missing: " + dir);
                return 0;
            }

            int added = 0;
            foreach (string file in Directory.GetFiles(dir, "*.json", SearchOption.AllDirectories))
            {
                try { if (LoadOne(file, assets, SmasherAugmentations.SetOf(dir, file))) added++; }
                catch (Exception e)
                {
                    Debug.LogError("[AugmentationsExpanded] trash " + Path.GetFileName(file) + ": " + e);
                }
            }
            Debug.Log("[AugmentationsExpanded] crates registered: " + added);
            return added;
        }

        /// <summary>
        /// Builds and registers one crate from its json, and remembers its Categories and set.
        /// </summary>
        /// <param name="file">Full path of a json with a Data object holding at least Id.</param>
        /// <param name="assetsRoot">Full path of the Assets folder.</param>
        /// <param name="set">Set of the crate, as returned by SmasherAugmentations.SetOf.</param>
        /// <returns>True when a new record was added; false without an Id, for an id already loaded
        /// or already in the game.</returns>
        /// <remarks>
        /// Input: The parameters, the json file and its images. The inventory width is the json
        /// one, or follows the inventory icon when it comes from an icon pack (see
        /// SmasherIcons.InventoryWidth).
        /// Output: The return value. Adds the record to the game items; throws on unreadable json,
        /// which the caller catches.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static bool LoadOne(string file, string assetsRoot, string set)
        {
            JSONNode d = JSON.Parse(File.ReadAllText(file))["Data"];
            string id = d["Id"].Value;
            if (string.IsNullOrEmpty(id) || _loaded.Contains(id)) return false;
            if (Data.Items.GetRecord(id) != null) { _loaded.Add(id); return false; }

            var rec = new TrashRecord
            {
                Categories         = SmasherJson.Strings(d["Categories"]),
                TechLevel          = d["TechLevel"].AsInt,
                Price              = d["Price"].AsFloat,
                Weight             = d["Weight"].AsFloat,
                InventoryWidthSize = Math.Max(1, d["InventoryWidthSize"].AsInt),
                CanPutInVest       = d["CanPutInVest"].AsBool,
                MaxStack           = (short)Math.Max(1, d["MaxStack"].AsInt),
                Disassembly        = SmasherJson.Quantities(d["Disassembly"])
            };
            SmasherSprites.SetProperty(rec, "Id", id);

            ItemClass ic;
            if (SmasherJson.TryEnum(d["ItemClass"].Value, out ic)) rec.ItemClass = ic;
            TrashSubtype st;
            if (SmasherJson.TryEnum(d["SubType"].Value, out st)) rec.SubType = st;

            Sprite packIcon;
            rec.ContentDescriptor = BuildDescriptor(id, assetsRoot, set, out packIcon);
            rec.InventoryWidthSize = SmasherIcons.InventoryWidth(id, packIcon, rec.InventoryWidthSize);

            Data.Items.AddRecord(id, rec);
            _loaded.Add(id);
            _categories[id] = new List<string>(rec.Categories ?? new List<string>());
            _sets[id] = set;
            Debug.Log("[AugmentationsExpanded] + crate " + id + "  TL" + rec.TechLevel
                    + "  " + rec.InventoryWidthSize + "x1  contents=" + rec.Disassembly.Count);
            return true;
        }

        /// <summary>
        /// Creates the descriptor of a crate with the sprites Images/Trash/{id}_inv.png, _floor.png
        /// and _shadow.png, taken from the icon pack chosen for the item icons of its set when the
        /// pack has them.
        /// </summary>
        /// <param name="id">Id of the crate, e.g. "nemesis_box".</param>
        /// <param name="assetsRoot">Full path of the Assets folder.</param>
        /// <param name="set">Set of the crate, as returned by SmasherAugmentations.SetOf.</param>
        /// <param name="packIcon">Set by the method: the inventory icon when it was loaded from an
        /// icon pack, otherwise null.</param>
        /// <returns>The descriptor. Never null; a missing image leaves that sprite null and is
        /// logged.</returns>
        /// <remarks>
        /// Input: The parameters, the chosen icon pack and the images.
        /// Output: The return value and packIcon.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static ItemContentDescriptor BuildDescriptor(string id, string assetsRoot, string set, out Sprite packIcon)
        {
            var desc = ScriptableObject.CreateInstance<ItemContentDescriptor>();
            desc.name = id + "_descriptor";
            UnityEngine.Object.DontDestroyOnLoad(desc);

            SmasherIcons.Section section = SmasherIcons.ForItem(id, set);
            string inv    = "Images/Trash/" + id + "_inv.png";
            string floor  = "Images/Trash/" + id + "_floor.png";
            string shadow = "Images/Trash/" + id + "_shadow.png";
            bool fromPack;
            Sprite icon = SmasherIcons.LoadItemSprite(assetsRoot, section, SmasherIcons.Kind.Items, inv, inv, out fromPack);
            packIcon = fromPack ? icon : null;
            SmasherSprites.SetField(desc, "_icon",      icon);
            SmasherSprites.SetField(desc, "_smallIcon",
                SmasherIcons.LoadItemSprite(assetsRoot, section, SmasherIcons.Kind.Items, floor, floor, out _));
            SmasherSprites.SetField(desc, "_shadow",
                SmasherIcons.LoadItemSprite(assetsRoot, section, SmasherIcons.Kind.Items, shadow, shadow, out _));
            return desc;
        }
    }
}
