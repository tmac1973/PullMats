using System;
using Jotunn.Configs;
using Jotunn.Managers;

namespace PullMats
{
    internal static class PullInput
    {
        private static ButtonConfig _button = null!;

        /// <summary>
        /// Name to query ZInput with. Only valid after Register(): Jotunn's AddButton appends "!&lt;mod guid&gt;"
        /// to the name it was given.
        /// </summary>
        internal static string ButtonName => _button.Name;

        /// <summary>Raised when the pull key is pressed with an allowed tool out and a piece selected.</summary>
        internal static event Action<Player, Piece>? PullRequested;

        internal static void Register()
        {
            _button = new ButtonConfig
            {
                Name = "PullMats_Pull",
                ShortcutConfig = PullMatsConfig.PullKey,
                GamepadConfig = PullMatsConfig.PullGamepad,
                Hint = "Pull mats",
                ActiveInGUI = false,
                ActiveInCustomGUI = false,
                BlockOtherInputs = false,
            };
            InputManager.Instance.AddButton(Plugin.Guid, _button);
        }

        internal static void Tick()
        {
            if (!PullMatsConfig.Enabled)
                return;

            // No local player on a dedicated server, so this stays inert there.
            Player player = Player.m_localPlayer;
            if (player == null)
                return;

            if (!ZInput.GetButtonDown(ButtonName))
                return;

            // The build menu itself (Hud piece selection) is allowed so you can pick and pull without closing it.
            if (Console.IsVisible() || (Chat.instance != null && Chat.instance.HasFocus()) || TextInput.IsVisible() ||
                Menu.IsVisible() || InventoryGui.IsVisible() || Minimap.IsOpen() || StoreGui.IsVisible())
                return;

            if (!player.InPlaceMode())
                return;

            if (!PullMatsConfig.IsAllowedTool(player.m_rightItem?.m_dropPrefab?.name))
                return;

            Piece piece = player.GetSelectedPiece();
            if (piece == null)
                return;

            PullRequested?.Invoke(player, piece);
        }
    }
}
