using System.Collections.Generic;
using System.IO;
using System.Reflection;
using MGSC;
using UnityEngine;

namespace AugmentationsExpanded
{
    public static class SmasherWoundSlotIcons
    {
        /// <summary>
        /// The vanilla human wound slot with the same type as a modded one ("Human" followed by the
        /// type), whose position on the body screen is reused.
        /// </summary>
        /// <param name="record">A wound slot record. Null is accepted.</param>
        /// <returns>The id of the human slot, e.g. "HumanArm" for SlotType "Arm", or null when the
        /// record or its SlotType is missing.</returns>
        /// <remarks>
        /// Input: The parameters only.
        /// Output: Only the return value.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static string VanillaSourceFor(WoundSlotRecord record)
        {
            if (record == null || string.IsNullOrEmpty(record.SlotType)) return null;
            return "Human" + record.SlotType;
        }

        private static readonly string[] States = { "normal", "wounded", "fixated", "amputated" };

        private static readonly HashSet<string> _registered = new HashSet<string>();

        /// <summary>
        /// Creates the body screen data of every wound slot of the mod: four images (normal,
        /// wounded, fixated, amputated) from Images/WoundSlots/{id}_{state}.png and the position of
        /// the human slot of the same type. The images follow the icon pack chosen for the wound
        /// slots of each set or item. The game fills this data only from its own assets, and the
        /// body and health screens would crash on a slot without it.
        /// </summary>
        /// <param name="modContentPath">Folder of the mod, the one that contains Assets. Null or
        /// empty loads nothing.</param>
        /// <remarks>
        /// Input: The parameter, the wound slot ids registered by SmasherRecords and their images.
        /// Output: Nothing is returned. Sets the body screen data of each wound slot record; a slot
        /// with a missing image is skipped and logged.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        public static void Register(string modContentPath)
        {
            if (string.IsNullOrEmpty(modContentPath))
            {
                Debug.LogError("[AugmentationsExpanded] empty ModContentPath: wound slots with no images");
                return;
            }

            string assets = Path.Combine(modContentPath, "Assets");
            string dir = Path.Combine(Path.Combine(assets, "Images"), "WoundSlots");
            if (!Directory.Exists(dir))
            {
                Debug.LogError("[AugmentationsExpanded] wound slot image folder not found: " + dir);
                return;
            }

            int ok = 0, total = 0;
            foreach (string slotId in SmasherRecords.Ids)
            {
                total++;
                if (_registered.Contains(slotId)) { ok++; continue; }

                WoundSlotRecord record = Data.WoundSlots.GetRecord(slotId);
                if (record == null)
                {
                    Debug.LogWarning("[AugmentationsExpanded] wound slot not loaded yet: " + slotId);
                    continue;
                }
                if (record.ContentDescriptor != null)
                {
                    _registered.Add(slotId);
                    ok++;
                    continue;
                }

                string source = VanillaSourceFor(record);
                if (source == null)
                {
                    Debug.LogError("[AugmentationsExpanded] " + slotId + " has no SlotType.");
                    continue;
                }

                SmasherIcons.Section section = SmasherIcons.ForSlot(slotId);
                Sprite[] sprites = new Sprite[States.Length];
                bool complete = true;
                for (int i = 0; i < States.Length; i++)
                {
                    sprites[i] = LoadSprite(assets, section, slotId, States[i]);
                    if (sprites[i] == null) complete = false;
                }
                if (!complete)
                {
                    Debug.LogError("[AugmentationsExpanded] incomplete images for " + slotId);
                    continue;
                }

                WoundSlotDescriptor descriptor = ScriptableObject.CreateInstance<WoundSlotDescriptor>();
                descriptor.name = slotId + "_descriptor";
                Object.DontDestroyOnLoad(descriptor);

                SetField(descriptor, "_slotPosition", SlotPositionOf(source));
                SetField(descriptor, "_normalIcon", sprites[0]);
                SetField(descriptor, "_woundedIcon", sprites[1]);
                SetField(descriptor, "_fixatedIcon", sprites[2]);
                SetField(descriptor, "_amputatedIcon", sprites[3]);

                record.ContentDescriptor = descriptor;
                _registered.Add(slotId);
                ok++;
            }
            Debug.Log("[AugmentationsExpanded] wound slot descriptors registered: " + ok + "/" + total);
        }

        /// <summary>
        /// Position on the body screen of a vanilla wound slot.
        /// </summary>
        /// <param name="vanillaSlotId">Id of a vanilla wound slot, e.g. "HumanArm".</param>
        /// <returns>The position, or (0, 0) when it cannot be read (a warning is logged).</returns>
        /// <remarks>
        /// Input: The parameter and the vanilla wound slot data.
        /// Output: Only the return value.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static Vector2 SlotPositionOf(string vanillaSlotId)
        {
            WoundSlotRecord rec = Data.WoundSlots.GetRecord(vanillaSlotId);
            WoundSlotDescriptor desc = rec == null ? null : rec.ContentDescriptor as WoundSlotDescriptor;
            if (desc != null) return desc.SlotPosition;
            Debug.LogWarning("[AugmentationsExpanded] position not readable from " + vanillaSlotId + ", using (0,0)");
            return Vector2.zero;
        }

        /// <summary>
        /// Loads one wound slot image with the vanilla parameters: centered pivot, 1 pixel per
        /// unit, point filtering, no mipmaps. The image comes from the icon pack chosen for the
        /// wound slots of the slot's set or item when the pack has it.
        /// </summary>
        /// <param name="assets">Full path of the Assets folder.</param>
        /// <param name="section">Section that owns the slot, from SmasherIcons.ForSlot; null
        /// loads the default image.</param>
        /// <param name="slotId">Id of the wound slot, e.g. "SmasherHead".</param>
        /// <param name="state">"normal", "wounded", "fixated" or "amputated".</param>
        /// <returns>The sprite, or null when the file is missing or unreadable (the error is
        /// logged).</returns>
        /// <remarks>
        /// Input: The parameters, the chosen icon pack and the image file
        /// Images/WoundSlots/{slotId}_{state}.png.
        /// Output: The return value. The sprite and its texture are kept between scenes.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static Sprite LoadSprite(string assets, SmasherIcons.Section section, string slotId, string state)
        {
            string root, rel;
            string wanted = "Images/WoundSlots/" + slotId + "_" + state + ".png";
            SmasherIcons.Pick(assets, section, SmasherIcons.Kind.WoundSlots, wanted, wanted, out root, out rel);
            string path = Path.Combine(root, rel.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(path))
            {
                Debug.LogError("[AugmentationsExpanded] missing " + path);
                return null;
            }

            byte[] bytes = File.ReadAllBytes(path);
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!ImageConversion.LoadImage(tex, bytes))
            {
                Debug.LogError("[AugmentationsExpanded] unreadable PNG: " + path);
                return null;
            }
            tex.name = slotId + "_" + state;
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.Apply(false, false);
            Object.DontDestroyOnLoad(tex);

            Sprite sprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height),
                                          new Vector2(0.5f, 0.5f), 1f);
            sprite.name = tex.name;
            Object.DontDestroyOnLoad(sprite);
            return sprite;
        }

        /// <summary>
        /// Sets a private field of the wound slot body screen data.
        /// </summary>
        /// <param name="target">A WoundSlotDescriptor.</param>
        /// <param name="fieldName">Exact name of the field, e.g. "_normalIcon".</param>
        /// <param name="value">The new value, of the field's type.</param>
        /// <remarks>
        /// Input: The parameters only.
        /// Output: Nothing is returned. Changes the field; logs an error when it does not exist.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo f = typeof(WoundSlotDescriptor).GetField(
                fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (f == null)
            {
                Debug.LogError("[AugmentationsExpanded] field " + fieldName + " missing on WoundSlotDescriptor");
                return;
            }
            f.SetValue(target, value);
        }
    }
}
