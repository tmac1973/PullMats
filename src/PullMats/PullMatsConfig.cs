using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx.Configuration;
using Jotunn.Managers;
using PullMats.Core;
using UnityEngine;

namespace PullMats
{
    internal static class PullMatsConfig
    {
        private static ConfigEntry<bool> _enabled = null!;
        private static ConfigEntry<PullMode> _pullMode = null!;
        private static ConfigEntry<string> _allowedTools = null!;
        private static ConfigEntry<MessageHud.MessageType> _successPosition = null!;
        private static ConfigEntry<MessageHud.MessageType> _failurePosition = null!;

        private static HashSet<string> _allowedToolSet = new(StringComparer.OrdinalIgnoreCase);

        internal static ConfigEntry<KeyboardShortcut> PullKey { get; private set; } = null!;
        internal static ConfigEntry<InputManager.GamepadButton> PullGamepad { get; private set; } = null!;

        internal static bool Enabled => _enabled.Value;
        internal static PullMode PullMode => _pullMode.Value;
        internal static MessageHud.MessageType SuccessPosition => _successPosition.Value;
        internal static MessageHud.MessageType FailurePosition => _failurePosition.Value;

        internal static bool IsAllowedTool(string? prefabName) =>
            prefabName != null && _allowedToolSet.Contains(prefabName);

        internal static void Bind(ConfigFile cfg)
        {
            // Admin-only entries are synced from the server by Jotunn when the server has PullMats.
            _enabled = cfg.Bind("1 - General", "Enabled", true,
                new ConfigDescription("Master switch for the pull key.", null, AdminOnly()));
            _pullMode = cfg.Bind("1 - General", "Pull mode", PullMode.FullSet,
                new ConfigDescription(
                    "FullSet: each press pulls one full recipe's worth, regardless of what you carry. " +
                    "TopUp: each press rounds every material up to the next whole set.",
                    null, AdminOnly()));
            _allowedTools = cfg.Bind("1 - General", "Allowed tools", "Hammer",
                new ConfigDescription(
                    "Comma-separated item prefab names that enable the pull key (e.g. Hammer). " +
                    "Add modded hammer prefab names to allow them.",
                    null, AdminOnly()));

            _successPosition = cfg.Bind("2 - Notifications", "Success position", MessageHud.MessageType.TopLeft,
                "Where the 'Pulled ...' message appears.");
            _failurePosition = cfg.Bind("2 - Notifications", "Failure position", MessageHud.MessageType.Center,
                "Where 'Can't pull ...' messages appear.");

            PullKey = cfg.Bind("3 - Input", "Pull key", new KeyboardShortcut(KeyCode.N),
                "Key that pulls materials for the selected build piece.");
            PullGamepad = cfg.Bind("3 - Input", "Pull gamepad button", InputManager.GamepadButton.None,
                "Gamepad button that pulls materials for the selected build piece. None = unbound.");

            ParseAllowedTools();
            _allowedTools.SettingChanged += (_, _) => ParseAllowedTools();
            SynchronizationManager.OnConfigurationSynchronized += (_, _) => ParseAllowedTools();
        }

        private static ConfigurationManagerAttributes AdminOnly() => new() { IsAdminOnly = true };

        private static void ParseAllowedTools()
        {
            _allowedToolSet = new HashSet<string>(
                _allowedTools.Value.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0),
                StringComparer.OrdinalIgnoreCase);
        }
    }
}
