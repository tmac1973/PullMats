using System.Collections.Generic;
using System.Linq;
using PullMats.Core;

namespace PullMats.Game
{
    internal static class Notifier
    {
        internal static void ShowSuccess(string pieceToken, IReadOnlyList<MaterialNeed> needs,
            IReadOnlyDictionary<string, int> moved, bool shortfall)
        {
            IEnumerable<string> parts = needs
                .Where(n => moved.TryGetValue(n.Key, out int m) && m > 0)
                .Select(n => $"{n.DisplayName} ×{moved[n.Key]}");
            string text = $"Pulled {Localize(pieceToken)}: {string.Join(", ", parts)}";
            if (shortfall)
                text += " (a chest changed — pulled less than a full set)";
            Show(PullMatsConfig.SuccessPosition, text);
        }

        internal static void ShowFailure(string pieceToken, PullResult result)
        {
            string piece = Localize(pieceToken);
            string text = result switch
            {
                PullResult.Missing m => $"Can't pull {piece} — missing " +
                    string.Join(", ", m.Shortfalls.Select(s => $"{s.Need.DisplayName} {s.Available}/{s.Required}")),
                PullResult.TooHeavy h => $"Can't pull {piece} — too heavy ({h.ResultingWeight:0}/{h.MaxWeight:0})",
                PullResult.NoSpace s => $"Can't pull {piece} — inventory full (needs {s.SlotsNeeded - s.SlotsFree} more slot(s))",
                PullResult.NothingToPull => $"{piece} needs no materials",
                _ => $"Can't pull {piece}",
            };
            Show(PullMatsConfig.FailurePosition, text);
        }

        internal static void ShowError() => Show(PullMatsConfig.FailurePosition, "PullMats error — see log");

        private static string Localize(string token) => Localization.instance.Localize(token);

        private static void Show(MessageHud.MessageType type, string text)
        {
            if (MessageHud.instance != null)
                MessageHud.instance.ShowMessage(type, text);
        }
    }
}
