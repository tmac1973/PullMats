using System.Linq;
using HarmonyLib;
using Jotunn.Managers;
using TMPro;
using UnityEngine;

namespace PullMats.UI
{
    /// <summary>
    /// Adds a "Pull mats" row to the vanilla build-mode key hints by cloning an existing row, leaving
    /// the vanilla (and other mods') rows untouched.
    /// </summary>
    [HarmonyPatch]
    internal static class KeyHintRow
    {
        private const string HintText = "Pull mats";

        private static GameObject? _kbRow;
        private static TMP_Text? _kbKey;
        private static string? _kbKeyText;

        private static GameObject? _gpRow;
        private static TMP_Text? _gpText;
        private static string? _gpKeyText;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(KeyHints), "Start")]
        private static void Create(KeyHints __instance)
        {
            UIInputHint? hint = __instance.m_buildHints != null ? __instance.m_buildHints.GetComponent<UIInputHint>() : null;
            if (hint == null)
            {
                Plugin.Log.LogWarning("Build key hints not found; 'Pull mats' hint disabled.");
                return;
            }

            if (hint.m_mouseKeyboardHint != null)
                CreateKeyboardRow(hint.m_mouseKeyboardHint.transform);
            if (hint.m_gamepadHint != null)
                CreateGamepadRow(hint.m_gamepadHint.transform);
            if (_kbRow == null)
                Plugin.Log.LogWarning("Could not create keyboard 'Pull mats' hint row.");
        }

        /// <summary>
        /// Keyboard rows look like: Row { Text [label], key_bkg { Key [key] } }. Clone the first row with
        /// exactly one key cap (e.g. "Remove").
        /// </summary>
        private static void CreateKeyboardRow(Transform root)
        {
            Transform? template = root.Cast<Transform>().FirstOrDefault(row =>
                row.Find("Text")?.GetComponent<TMP_Text>() != null &&
                row.Find("key_bkg/Key")?.GetComponent<TMP_Text>() != null &&
                row.Find("key_bkg (1)") == null);
            if (template == null)
                return;

            GameObject clone = Clone(template, root, "PullMats_Hint");
            clone.transform.Find("Text").GetComponent<TMP_Text>().text = HintText;
            _kbKey = clone.transform.Find("key_bkg/Key").GetComponent<TMP_Text>();
            _kbRow = clone;
        }

        /// <summary>Gamepad rows are one text: "Label &lt;mspace=0.6em&gt; KEY&lt;/mspace&gt;".</summary>
        private static void CreateGamepadRow(Transform root)
        {
            Transform? template = root.Cast<Transform>().FirstOrDefault(row =>
                row.GetComponent<TMP_Text>()?.text.Contains("<mspace") == true);
            if (template == null)
                return;

            GameObject clone = Clone(template, root, "PullMats_HintGP");
            _gpText = clone.GetComponent<TMP_Text>();
            _gpRow = clone;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(KeyHints), "UpdateHints")]
        private static void Update()
        {
            if (_kbRow == null && _gpRow == null)
                return;

            Player player = Player.m_localPlayer;
            bool show = PullMatsConfig.Enabled && player != null && player.InPlaceMode() &&
                        PullMatsConfig.IsAllowedTool(player.m_rightItem?.m_dropPrefab?.name);

            if (_kbRow != null)
            {
                _kbRow.SetActive(show);
                if (show && _kbKey != null)
                    SetIfChanged(_kbKey, ref _kbKeyText, PullMatsConfig.PullKey.Value.ToString());
            }

            if (_gpRow != null)
            {
                bool gpShow = show && PullMatsConfig.PullGamepad.Value != InputManager.GamepadButton.None;
                _gpRow.SetActive(gpShow);
                if (gpShow && _gpText != null)
                    SetIfChanged(_gpText, ref _gpKeyText,
                        $"{HintText} <mspace=0.6em> {ZInput.instance.GetBoundKeyString("Joy!" + PullInput.ButtonName, true)}</mspace>");
            }
        }

        private static GameObject Clone(Transform template, Transform root, string name)
        {
            GameObject clone = Object.Instantiate(template.gameObject, root);
            clone.name = name;
            clone.transform.SetAsLastSibling();

            // Localize components would overwrite our text with the original token.
            foreach (Localize localize in clone.GetComponentsInChildren<Localize>(true))
                Object.DestroyImmediate(localize);

            clone.SetActive(false);
            return clone;
        }

        private static void SetIfChanged(TMP_Text label, ref string? cache, string value)
        {
            if (cache == value)
                return;
            cache = value;
            label.text = value;
        }
    }
}
