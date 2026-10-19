using System;
using System.Collections.Generic;
using MGSC;

namespace AugmentationsExpanded
{
    internal static class SmasherUhmg
    {
        internal const string AugId          = "ae_uhmg";
        internal const string WeaponId       = "ae_nature_uhmg";
        internal const string ArmSlotId      = "AE_UHMGArm";
        internal const string ShoulderSlotId = "AE_UHMGShoulder";
        internal const string ChestSlotId    = "AE_UHMGChest";
        internal const string ConfigKey      = "uhmg_enabled";
        internal const string LabelKey       = "ae.cfg.uhmg.toggle.label";
        internal const string TipKey         = "ae.cfg.uhmg.toggle.tip";

        /// <summary>
        /// Factions that give the UHMG as a reward, each with the tech level of its row in the
        /// faction equipment table.
        /// </summary>
        internal static readonly KeyValuePair<string, int>[] RewardFactions =
        {
            new KeyValuePair<string, int>("PlanetBridge", 6),
            new KeyValuePair<string, int>("Tianming",     6),
            new KeyValuePair<string, int>("RealWare",     7),
        };

        internal static bool Enabled = true;

        /// <summary>
        /// Applies the Mod Config Menu switch of the UHMG to its spawn on enemies.
        /// </summary>
        /// <remarks>
        /// Input: No parameters. Reads the switch.
        /// Output: Nothing is returned. Changes the Categories of the UHMG augmentation.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        internal static void ApplySpawn()
        {
            SmasherAugmentations.SetItemSpawnEnabled(AugId, Enabled);
        }

        /// <summary>
        /// Gives the UHMG a reward row in the equipment table of each faction of RewardFactions,
        /// at that faction's tech level. Unlike the gun arms the factions are not read from the
        /// json Categories, because each faction has its own tech level. Removes the rows when the
        /// switch is off.
        /// </summary>
        /// <remarks>
        /// Input: No parameters. Reads the switch.
        /// Output: Nothing is returned. Changes the faction reward tables.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        internal static void ApplyRewards()
        {
            SmasherChipDrop.ApplyAugmentation(AugId, RewardFactions, !Enabled);
        }

        /// <summary>
        /// Adds the UHMG recipe to the unlock list of a chip.
        /// </summary>
        /// <param name="chipId">Id of an existing chip, the vanilla "augment_chip".</param>
        /// <remarks>
        /// Input: The parameters only.
        /// Output: Nothing is returned. Changes the chip's unlock list.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        internal static void AddToChip(string chipId)
        {
            SmasherAugmentations.AddToChip(chipId, AugId);
        }

        /// <summary>
        /// Reads the UHMG switch from the values saved by the Mod Config Menu.
        /// </summary>
        /// <param name="config">The panel values keyed by config key, e.g. "uhmg_enabled": true.
        /// A missing key keeps the current value.</param>
        /// <remarks>
        /// Input: The parameters only.
        /// Output: Nothing is returned. Changes the switch.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        internal static void ReadConfig(Dictionary<string, object> config)
        {
            object v;
            if (config.TryGetValue(ConfigKey, out v) && v != null)
                Enabled = Convert.ToBoolean(v);
        }

        /// <summary>
        /// Reads one config.json entry when it is the UHMG switch.
        /// </summary>
        /// <param name="key">Config key, e.g. "uhmg_enabled".</param>
        /// <param name="value">Value as text without quotes: anything but "false" means
        /// on.</param>
        /// <returns>True when the key is the UHMG switch and was read; false for any other
        /// key.</returns>
        /// <remarks>
        /// Input: The parameters only.
        /// Output: The return value. Changes the switch.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        internal static bool TryLoad(string key, string value)
        {
            if (key != ConfigKey) return false;
            Enabled = !value.Equals("false", StringComparison.OrdinalIgnoreCase);
            return true;
        }

        /// <summary>
        /// The UHMG entry of config.json, added by SaveSettings before the closing brace.
        /// </summary>
        /// <returns>Text that continues the JSON object: a comma, a new line and the entry
        /// "uhmg_enabled": true or false.</returns>
        /// <remarks>
        /// Input: No parameters. Reads the switch.
        /// Output: Only the return value.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        internal static string ConfigJsonTail()
        {
            return ",\n  \"" + ConfigKey + "\": " + (Enabled ? "true" : "false");
        }

        /// <summary>
        /// Short description of the UHMG switch for the log and the save message.
        /// </summary>
        /// <returns>"on" or "off".</returns>
        /// <remarks>
        /// Input: No parameters. Reads the switch.
        /// Output: Only the return value.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        internal static string Summary()
        {
            return Enabled ? "on" : "off";
        }
    }
}
