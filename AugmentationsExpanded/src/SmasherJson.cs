using System;
using System.Collections.Generic;
using MGSC;
using SimpleJSON;

namespace AugmentationsExpanded
{
    public static class SmasherJson
    {
        /// <summary>
        /// Reads a JSON array of strings, skipping empty entries.
        /// </summary>
        /// <param name="n">A JSON array node, e.g. ["Specops", "RealWare"]. Null or not an array is
        /// accepted.</param>
        /// <returns>The strings in order. Never null; empty when the node is missing or not an
        /// array.</returns>
        /// <remarks>
        /// Input: The parameters only.
        /// Output: Only the return value.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        public static List<string> Strings(JSONNode n)
        {
            var l = new List<string>();
            if (n == null || !n.IsArray) return l;
            for (int i = 0; i < n.Count; i++)
                if (!string.IsNullOrEmpty(n[i].Value)) l.Add(n[i].Value);
            return l;
        }

        /// <summary>
        /// Reads a JSON array of { ItemId, Count } pairs, as used by Disassembly and by recipe
        /// ingredients.
        /// </summary>
        /// <param name="n">A JSON array node, e.g. [{"ItemId": "spring", "Count": 1}]. Null or not
        /// an array is accepted; entries without ItemId are skipped.</param>
        /// <returns>The quantities in order. Never null; empty when the node is missing or not an
        /// array.</returns>
        /// <remarks>
        /// Input: The parameters only.
        /// Output: Only the return value.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        public static List<ItemQuantity> Quantities(JSONNode n)
        {
            var l = new List<ItemQuantity>();
            if (n == null || !n.IsArray) return l;
            for (int i = 0; i < n.Count; i++)
            {
                string id = n[i]["ItemId"].Value;
                if (string.IsNullOrEmpty(id)) continue;
                l.Add(new ItemQuantity(id, n[i]["Count"].AsInt));
            }
            return l;
        }

        /// <summary>
        /// Reads an on/off switch of the mod's own json keys, e.g. "ae_kill_long_stats": "On".
        /// </summary>
        /// <param name="n">The value node. Null or missing is accepted.</param>
        /// <returns>True for the string "On" (ignoring case) or the boolean true; false for
        /// anything else, also "Off" and a missing key.</returns>
        /// <remarks>
        /// Input: The parameters only.
        /// Output: Only the return value.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        public static bool IsOn(JSONNode n)
        {
            if (n == null) return false;
            return string.Equals(n.Value, "On", StringComparison.OrdinalIgnoreCase)
                || (n.IsBoolean && n.AsBool);
        }

        /// <summary>
        /// Reads an enum value by name, ignoring case.
        /// </summary>
        /// <param name="value">The name, e.g. "CyberneticAug". Null or empty is accepted.</param>
        /// <param name="result">Set by the method: the parsed value, or the default value of the
        /// enum on failure.</param>
        /// <returns>True when parsed; false when the name is empty or unknown.</returns>
        /// <remarks>
        /// Input: The parameters only.
        /// Output: The return value and result.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        public static bool TryEnum<T>(string value, out T result) where T : struct
        {
            result = default(T);
            if (string.IsNullOrEmpty(value)) return false;
            try { result = (T)Enum.Parse(typeof(T), value, true); return true; }
            catch { return false; }
        }
    }
}
