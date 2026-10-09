using System;
using System.Collections.Generic;
using KSP.Localization;
using UnityEngine;

/* RealChute was made by Christophe Savard (stupid_chris). You are free to copy, fork, and modify RealChute as you see
 * fit. However, redistribution is only permitted for unmodified versions of RealChute, and under attribution clause.
 * If you want to distribute a modified version of RealChute, be it code, textures, configs, or any other asset and
 * piece of work, you must get my explicit permission on the matter through a private channel, and must also distribute
 * it through the attribution clause, and must make it clear to anyone using your modification of my work that they
 * must report any problem related to this usage to you, and not to me. This clause expires if I happen to be
 * inactive (no connection) for a period of 90 days on the official KSP forums. In that case, the license reverts
 * back to CC-BY-NC-SA 4.0 INTL.*/

namespace RealChute
{
    /// <summary>
    /// Handles all localization of the RealChute user interface.
    /// Retrieves the translated strings from GameData/RealChute/Localization/(en-us|zh-cn).cfg.
    /// </summary>
    public static class Localization
    {
        #region Constants
        /// <summary>
        /// Prefix of all the RealChute localization tags
        /// </summary>
        private const string prefix = "#RealChute_";
        #endregion

        #region Fields
        /// <summary>
        /// Caches the strings already looked up to avoid a dictionary lookup on every OnGUI call
        /// </summary>
        private static readonly Dictionary<string, string> cache = new Dictionary<string, string>();

        /// <summary>
        /// Reusable builder for the indexed replacement to avoid allocations in the GUI loops
        /// </summary>
        private static readonly List<string> replacements = new List<string>();
        #endregion

        #region Methods
        /// <summary>
        /// Returns the localized string associated with the given tag.
        /// If the tag cannot be found (or the Localizer is not ready yet), the tag itself is returned so the
        /// untranslated key is easy to spot instead of throwing from inside a GUI callback.
        /// </summary>
        /// <param name="tag">Localization tag, without the leading # (see Localization/en-us.cfg)</param>
        public static string Get(string tag)
        {
            if (cache.TryGetValue(tag, out string value)) { return value; }

            string key = prefix + tag;
            try
            {
                if (!Localizer.TryGetStringByTag(key, out value) || string.IsNullOrEmpty(value)) { value = key; }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[RealChute]: Could not look up the \"{key}\" localization tag: {e.Message}");
                value = key;
            }

            cache[tag] = value;
            return value;
        }

        /// <summary>
        /// Returns the localized string associated with the given tag, with all the &lt;&lt;n&gt;&gt; indexed
        /// placeholders replaced by the given values, in order.
        /// </summary>
        /// <param name="tag">Localization tag, without the leading #</param>
        /// <param name="values">Values to insert in place of the placeholders</param>
        public static string Get(string tag, params object[] values)
        {
            string text = Get(tag);
            if (values == null || values.Length == 0) { return text; }

            replacements.Clear();
            for (int i = 0; i < values.Length; i++)
            {
                replacements.Add(values[i] == null ? string.Empty : values[i].ToString());
            }
            return Localizer.Format(text, replacements.ToArray());
        }
        #endregion

        #region Checking
        /// <summary>
        /// Clears the lookup cache. Mostly useful for the localization checking tool.
        /// </summary>
        public static void ClearCache() => cache.Clear();
        #endregion
    }
}
