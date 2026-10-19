using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using MGSC;
using SimpleJSON;
using UnityEngine;

namespace AugmentationsExpanded
{
    public static class SmasherContent
    {
        private static readonly string[] Langs =
        {
            "EnglishUS", "Russian", "German", "French", "Spanish", "Polish",
            "Turkish", "BrazilianPortugal", "Korean", "Japanese", "ChineseSimp"
        };

        /// <summary>
        /// Loads every json under Assets/Datadisks as a datadisk record: the set chips, with their
        /// sprites and their Disassembly, which is what lets them be taken apart into vanilla
        /// chips. Ids that already exist are skipped.
        /// </summary>
        /// <param name="modContentPath">Folder of the mod, the one that contains Assets. Must not
        /// be null: the method would throw, and the caller catches it.</param>
        /// <returns>The number of datadisks added; 0 when the folder is missing.</returns>
        /// <remarks>
        /// Input: The parameter, the json files under Assets/Datadisks and their images. The
        /// inventory width is the json one, or follows the inventory icon when it comes from an
        /// icon pack (see SmasherIcons.InventoryWidth).
        /// Output: The return value. Adds the records to the game items and writes lines in the
        /// log.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        public static int RegisterDatadisks(string modContentPath)
        {
            string assets = Path.Combine(modContentPath, "Assets");
            string dir = Path.Combine(assets, "Datadisks");
            if (!Directory.Exists(dir)) { Debug.LogWarning("[AugmentationsExpanded] Datadisks folder missing: " + dir); return 0; }

            int added = 0;
            foreach (string file in Directory.GetFiles(dir, "*.json", SearchOption.AllDirectories))
            {
                try
                {
                    JSONNode d = JSON.Parse(File.ReadAllText(file))["Data"];
                    string id = d["Id"].Value;
                    if (string.IsNullOrEmpty(id)) continue;
                    if (Data.Items.GetRecord(id) != null) continue;

                    var rec = new DatadiskRecord
                    {
                        Categories         = SmasherJson.Strings(d["Categories"]),
                        TechLevel          = d["TechLevel"].AsInt,
                        Price              = d["Price"].AsFloat,
                        Weight             = d["Weight"].AsFloat,
                        InventoryWidthSize = Math.Max(1, d["InventoryWidthSize"].AsInt),
                        CanPutInVest       = d["CanPutInVest"].AsBool,
                        UnlockIds          = SmasherJson.Strings(d["UnlockIds"]),
                        Disassembly        = SmasherJson.Quantities(d["Disassembly"])
                    };
                    SmasherSprites.SetProperty(rec, "Id", id);

                    ItemClass ic;
                    if (SmasherJson.TryEnum(d["ItemClass"].Value, out ic)) rec.ItemClass = ic;
                    DatadiskUnlockType ut;
                    if (SmasherJson.TryEnum(d["UnlockType"].Value, out ut)) rec.UnlockType = ut;

                    Sprite packIcon;
                    rec.ContentDescriptor = BuildDescriptor(id, assets,
                        Path.Combine(Path.Combine(assets, "Descriptors"), "Datadisks"),
                        SmasherAugmentations.SetOf(dir, file), out packIcon);
                    rec.InventoryWidthSize = SmasherIcons.InventoryWidth(id, packIcon, rec.InventoryWidthSize);

                    Data.Items.AddRecord(id, rec);
                    added++;
                    Debug.Log("[AugmentationsExpanded] + datadisk " + id + "  TL" + rec.TechLevel
                            + "  unlocks=" + rec.UnlockIds.Count);
                }
                catch (Exception e)
                {
                    Debug.LogError("[AugmentationsExpanded] datadisk " + Path.GetFileName(file) + ": " + e);
                }
            }
            Debug.Log("[AugmentationsExpanded] datadisks registered: " + added);
            return added;
        }

        /// <summary>
        /// Creates the descriptor of a datadisk with the inventory, floor and shadow sprites. The
        /// images are Images/Datadisks/{id}_inv.png, _floor.png and _shadow.png, unless an
        /// {id}_descriptor.json under Descriptors/Datadisks names other paths. When an icon pack is
        /// chosen for the chip of its set, the pack's files are used instead (see
        /// SmasherIcons.Pick).
        /// </summary>
        /// <param name="id">Id of the datadisk, e.g. "nacht_chip".</param>
        /// <param name="assetsRoot">Full path of the Assets folder.</param>
        /// <param name="descDir">Full path of the Descriptors/Datadisks folder.</param>
        /// <param name="set">Set folder of the datadisk, e.g. "Nachtreiher".</param>
        /// <param name="packIcon">Set by the method: the inventory icon when it was loaded from an
        /// icon pack, otherwise null.</param>
        /// <returns>The descriptor. Never null; a missing image leaves that sprite null and is
        /// logged.</returns>
        /// <remarks>
        /// Input: The parameters, the optional descriptor json, the chosen icon pack and the
        /// images.
        /// Output: The return value and packIcon.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static DatadiskDescriptor BuildDescriptor(string id, string assetsRoot, string descDir, string set,
                                                          out Sprite packIcon)
        {
            string icon   = "Images/Datadisks/" + id + "_inv.png";
            string small  = "Images/Datadisks/" + id + "_floor.png";
            string shadow = "Images/Datadisks/" + id + "_shadow.png";

            string descFile = FindFile(descDir, id + "_descriptor.json");
            if (descFile != null)
            {
                try
                {
                    JSONNode img = JSON.Parse(File.ReadAllText(descFile))["Data"]["ImageProperties"];
                    if (!string.IsNullOrEmpty(img["IconSpriteIdOrPath"].Value))         icon   = img["IconSpriteIdOrPath"].Value;
                    if (!string.IsNullOrEmpty(img["SmallIconSpriteIdOrPath"].Value))    small  = img["SmallIconSpriteIdOrPath"].Value;
                    if (!string.IsNullOrEmpty(img["ShadowOnFloorSpriteIdOrPath"].Value)) shadow = img["ShadowOnFloorSpriteIdOrPath"].Value;
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[AugmentationsExpanded] descriptor for " + id + " is unreadable: " + e.Message);
                }
            }

            var desc = ScriptableObject.CreateInstance<DatadiskDescriptor>();
            desc.name = id + "_descriptor";
            UnityEngine.Object.DontDestroyOnLoad(desc);
            SmasherIcons.Section section = SmasherIcons.ForItem(id, set);
            bool fromPack;
            Sprite inventoryIcon = SmasherIcons.LoadItemSprite(assetsRoot, section, SmasherIcons.Kind.Chip,
                "Images/Datadisks/" + id + "_inv.png", icon, out fromPack);
            packIcon = fromPack ? inventoryIcon : null;
            SmasherSprites.SetField(desc, "_icon",      inventoryIcon);
            SmasherSprites.SetField(desc, "_smallIcon", SmasherIcons.LoadItemSprite(assetsRoot, section, SmasherIcons.Kind.Chip,
                "Images/Datadisks/" + id + "_floor.png", small, out _));
            SmasherSprites.SetField(desc, "_shadow",    SmasherIcons.LoadItemSprite(assetsRoot, section, SmasherIcons.Kind.Chip,
                "Images/Datadisks/" + id + "_shadow.png", shadow, out _));
            return desc;
        }

        /// <summary>
        /// First file with the given name anywhere under a folder. A missing folder is not an
        /// error.
        /// </summary>
        /// <param name="root">Full path of the folder to search. Null, empty or missing gives
        /// null.</param>
        /// <param name="fileName">File name without folders, e.g. "x_descriptor.json".</param>
        /// <returns>The full path of the first match, or null.</returns>
        /// <remarks>
        /// Input: The parameters and the files on disk.
        /// Output: Only the return value.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static string FindFile(string root, string fileName)
        {
            if (string.IsNullOrEmpty(root) || !Directory.Exists(root)) return null;
            string[] hits = Directory.GetFiles(root, fileName, SearchOption.AllDirectories);
            return hits.Length > 0 ? hits[0] : null;
        }

        /// <summary>
        /// Loads every json under Assets/Crafting Recipes as a production recipe. A recipe is
        /// skipped when its output or one of its ingredients does not exist, which the game would
        /// only report at production time. An existing recipe with the same output is replaced,
        /// because the game always uses the first recipe it finds for an output. Must run after
        /// every output item is registered.
        /// </summary>
        /// <param name="modContentPath">Folder of the mod, the one that contains Assets. Must not
        /// be null: the method would throw, and the caller catches it.</param>
        /// <returns>The number of recipes added; 0 when the folder is missing.</returns>
        /// <remarks>
        /// Input: The parameter, the json files under Assets/Crafting Recipes and the items in the
        /// game data.
        /// Output: The return value. Changes the game's recipe list, hands every added recipe to
        /// SmasherCrafting for the hand and foot ingredient switch and writes lines in the log.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        public static int RegisterRecipes(string modContentPath)
        {
            string dir = Path.Combine(Path.Combine(modContentPath, "Assets"), "Crafting Recipes");
            if (!Directory.Exists(dir)) { Debug.LogWarning("[AugmentationsExpanded] Crafting Recipes folder missing: " + dir); return 0; }

            int added = 0, skipped = 0;
            foreach (string file in Directory.GetFiles(dir, "*.json", SearchOption.AllDirectories))
            {
                try
                {
                    JSONNode d = JSON.Parse(File.ReadAllText(file))["Data"];
                    string output = d["OutputItem"].Value;
                    if (string.IsNullOrEmpty(output)) continue;

                    if (Data.Items.GetRecord(output) == null)
                    {
                        Debug.LogWarning("[AugmentationsExpanded] recipe " + Path.GetFileName(file)
                                       + ": OutputItem " + output + " does not exist, skipped.");
                        skipped++; continue;
                    }

                    var required = new List<ItemQuantity>();
                    bool bad = false;
                    JSONNode items = d["RequiredItems"];
                    for (int i = 0; items != null && items.IsArray && i < items.Count; i++)
                    {
                        string iid = items[i]["ItemId"].Value;
                        if (string.IsNullOrEmpty(iid)) continue;
                        if (Data.Items.GetRecord(iid) == null)
                        {
                            Debug.LogWarning("[AugmentationsExpanded] recipe for " + output
                                           + ": ingredient " + iid + " does not exist, skipped.");
                            bad = true; break;
                        }
                        required.Add(new ItemQuantity(iid, items[i]["Count"].AsInt));
                    }
                    if (bad) { skipped++; continue; }

                    var rec = new ItemProduceReceipt
                    {
                        OutputItem         = output,
                        RequiredItems      = required,
                        ProduceTimeInHours = d["ProduceTimeInHours"].AsFloat,
                        ModifyStartCost    = d["ModifyStartCost"].AsInt,
                        ModifyStep         = d["ModifyStep"].AsFloat,
                        ModifyItemsGrades  = null,
                        ModifyLevelLimit   = d["ModifyLevelLimit"].AsInt
                    };
                    SmasherSprites.SetProperty(rec, "Id", d["Id"].Value ?? string.Empty);

                    ItemProduceReceipt old = Data.ProduceReceipts.Find(r => r.OutputItem == output);
                    if (old != null) Data.ProduceReceipts.Remove(old);

                    Data.ProduceReceipts.Add(rec);
                    SmasherCrafting.Track(rec);
                    added++;
                }
                catch (Exception e)
                {
                    Debug.LogError("[AugmentationsExpanded] recipe " + Path.GetFileName(file) + ": " + e);
                }
            }
            Debug.Log("[AugmentationsExpanded] recipes registered: " + added
                    + (skipped > 0 ? ", skipped: " + skipped : ""));
            return added;
        }

        /// <summary>
        /// Writes every key of every json under Assets/Localization into the game's text tables,
        /// for every language. An empty translation falls back to English, so no language shows a
        /// raw key.
        /// </summary>
        /// <param name="modContentPath">Folder of the mod, the one that contains Assets. Must not
        /// be null: the method would throw, and the caller catches it.</param>
        /// <returns>The number of keys written; 0 when the folder or the game's text table is
        /// missing.</returns>
        /// <remarks>
        /// Input: The parameter and the json files under Assets/Localization.
        /// Output: The return value. Changes the game's text tables for every language.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        public static int RegisterLocalization(string modContentPath)
        {
            string dir = Path.Combine(Path.Combine(modContentPath, "Assets"), "Localization");
            if (!Directory.Exists(dir)) { Debug.LogWarning("[AugmentationsExpanded] Localization folder missing: " + dir); return 0; }

            var db = LocalizationDb();
            if (db == null) return 0;

            int added = 0;
            foreach (string file in Directory.GetFiles(dir, "*.json", SearchOption.AllDirectories))
            {
                try
                {
                    JSONNode keys = JSON.Parse(File.ReadAllText(file))["Data"]["Keys"];
                    foreach (string key in keys.Keys)
                    {
                        string fallback = keys[key]["EnglishUS"].Value;
                        foreach (string lang in Langs)
                        {
                            string text = keys[key][lang].Value;
                            if (string.IsNullOrEmpty(text)) text = fallback;
                            if (string.IsNullOrEmpty(text)) continue;
                            Write(db, lang, key, text);
                        }
                        added++;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("[AugmentationsExpanded] localization " + Path.GetFileName(file) + ": " + e);
                }
            }
            Debug.Log("[AugmentationsExpanded] localization keys registered: " + added);
            return added;
        }

        /// <summary>
        /// The game's private text table (language, then key, then text), read through reflection.
        /// Changes written into it are visible immediately.
        /// </summary>
        /// <returns>The live table, or null when it cannot be read (the error is logged).</returns>
        /// <remarks>
        /// Input: No parameters. Reads the game's localization object.
        /// Output: Only the return value.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static System.Collections.IDictionary LocalizationDb()
        {
            try
            {
                var loc = Singleton<Localization>.Instance;
                if (loc == null) { Debug.LogError("[AugmentationsExpanded] Localization instance is null."); return null; }
                FieldInfo f = typeof(Localization).GetField("db", BindingFlags.Instance | BindingFlags.NonPublic);
                if (f == null) { Debug.LogError("[AugmentationsExpanded] Localization.db not found."); return null; }
                return f.GetValue(loc) as System.Collections.IDictionary;
            }
            catch (Exception e)
            {
                Debug.LogError("[AugmentationsExpanded] reading Localization.db: " + e);
                return null;
            }
        }

        /// <summary>
        /// Sets the text of one key in one language, replacing any previous value.
        /// </summary>
        /// <param name="db">The table returned by LocalizationDb.</param>
        /// <param name="lang">Language name: EnglishUS, Russian, German, French, Spanish, Polish,
        /// Turkish, BrazilianPortugal, Korean, Japanese or ChineseSimp. Unknown names are
        /// ignored.</param>
        /// <param name="key">Localization key, e.g. "item.smasher_head.name".</param>
        /// <param name="text">The text to show.</param>
        /// <remarks>
        /// Input: The parameters only.
        /// Output: Nothing is returned. Changes the text of the key in that language.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static void Write(System.Collections.IDictionary db, string lang, string key, string text)
        {
            object langKey;
            try { langKey = Enum.Parse(typeof(Localization.Lang), lang); }
            catch { return; }
            var dict = db[langKey] as Dictionary<string, string>;
            if (dict == null) return;
            dict[key] = text;
        }
    }
}
