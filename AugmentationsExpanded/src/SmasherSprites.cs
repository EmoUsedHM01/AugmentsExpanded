using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace AugmentationsExpanded
{
    public static class SmasherSprites
    {
        private static readonly Dictionary<string, Sprite> _cache = new Dictionary<string, Sprite>();

        /// <summary>
        /// Loads a PNG as a sprite with point filtering, as fits pixel art, and caches it.
        /// </summary>
        /// <param name="root">Full path of the base folder, usually Assets.</param>
        /// <param name="relativePath">Path of the image below root with forward slashes, e.g.
        /// "Images/Augmentations/smasher_head_inv.png". Null or empty gives null.</param>
        /// <param name="pixelsPerUnit">Pixels per unit, 100 for item icons.</param>
        /// <param name="pivotX">Horizontal pivot from 0 (left) to 1 (right).</param>
        /// <param name="pivotY">Vertical pivot from 0 (bottom) to 1 (top).</param>
        /// <returns>The sprite, the cached one for a repeated request, or null when the file is
        /// missing or unreadable (the error is logged).</returns>
        /// <remarks>
        /// Input: The parameters and the image file.
        /// Output: The return value. The sprite and its texture are cached and kept between scenes.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        public static Sprite Load(string root, string relativePath, float pixelsPerUnit, float pivotX, float pivotY)
        {
            if (string.IsNullOrEmpty(relativePath)) return null;

            string path = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
            string key = path + "|" + pixelsPerUnit + "|" + pivotX + "|" + pivotY;
            Sprite cached;
            if (_cache.TryGetValue(key, out cached)) return cached;

            if (!File.Exists(path))
            {
                Debug.LogError("[AugmentationsExpanded] missing image: " + path);
                return null;
            }

            try
            {
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                if (!ImageConversion.LoadImage(tex, File.ReadAllBytes(path)))
                {
                    Debug.LogError("[AugmentationsExpanded] unreadable PNG: " + path);
                    return null;
                }
                tex.name = Path.GetFileNameWithoutExtension(path);
                tex.filterMode = FilterMode.Point;
                tex.wrapMode = TextureWrapMode.Clamp;
                tex.Apply(false, false);
                UnityEngine.Object.DontDestroyOnLoad(tex);

                var sprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height),
                                           new Vector2(pivotX, pivotY), pixelsPerUnit);
                sprite.name = tex.name;
                UnityEngine.Object.DontDestroyOnLoad(sprite);

                _cache[key] = sprite;
                return sprite;
            }
            catch (Exception e)
            {
                Debug.LogError("[AugmentationsExpanded] loading " + path + ": " + e);
                return null;
            }
        }

        /// <summary>
        /// Sets a private field, searching the base classes too. The sprites of a descriptor are
        /// read-only properties backed by such fields.
        /// </summary>
        /// <param name="target">The object to change. Null does nothing.</param>
        /// <param name="fieldName">Exact name of the field, e.g. "_icon".</param>
        /// <param name="value">The new value, of the field's type.</param>
        /// <remarks>
        /// Input: The parameters only.
        /// Output: Nothing is returned. Changes the field; logs an error when it does not exist.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        public static void SetField(object target, string fieldName, object value)
        {
            if (target == null) return;
            Type t = target.GetType();
            while (t != null)
            {
                FieldInfo f = t.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
                if (f != null) { f.SetValue(target, value); return; }
                t = t.BaseType;
            }
            Debug.LogError("[AugmentationsExpanded] field " + fieldName + " not found on " + target.GetType().Name);
        }

        /// <summary>
        /// Sets a property whose setter is not public, such as the Id of a record.
        /// </summary>
        /// <param name="target">The object to change. Null does nothing.</param>
        /// <param name="propertyName">Exact name of the property, e.g. "Id".</param>
        /// <param name="value">The new value, of the property's type.</param>
        /// <remarks>
        /// Input: The parameters only.
        /// Output: Nothing is returned. Changes the property; logs an error when it does not exist
        /// or has no setter.
        /// AI-generated documentation, written to help external users understand the mod.
        /// </remarks>
        public static void SetProperty(object target, string propertyName, object value)
        {
            if (target == null) return;
            Type t = target.GetType();
            while (t != null)
            {
                PropertyInfo p = t.GetProperty(propertyName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (p != null)
                {
                    MethodInfo setter = p.GetSetMethod(true);
                    if (setter != null) { setter.Invoke(target, new object[] { value }); return; }
                }
                t = t.BaseType;
            }
            Debug.LogError("[AugmentationsExpanded] property " + propertyName + " is not writable on " + target.GetType().Name);
        }
    }
}
