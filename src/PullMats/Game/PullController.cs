using System;
using PullMats.Core;
using UnityEngine;

namespace PullMats.Game
{
    internal static class PullController
    {
        private const float MinSecondsBetweenPulls = 0.2f;

        private static bool _busy;
        private static float _lastPullTime = float.NegativeInfinity;

        internal static void Init()
        {
            PullInput.PullRequested += OnPull;
        }

        private static void OnPull(Player player, Piece piece)
        {
            if (_busy || Time.time - _lastPullTime < MinSecondsBetweenPulls)
                return;

            _busy = true;
            _lastPullTime = Time.time;
            try
            {
                var (needs, prefabs) = RecipeReader.Read(piece);
                var (containers, stock) = SourceCollector.Collect(player, needs, prefabs);
                var request = new PullRequest(piece.m_name, PullMatsConfig.PullMode, needs, stock,
                    PlayerSnapshot.Take(player, needs));

                PullResult result = PullPlanner.Plan(request);
                if (result is PullResult.Success success)
                {
                    var (moved, shortfall) = PullExecutor.Execute(success, containers, prefabs, player);
                    Notifier.ShowSuccess(piece.m_name, needs, moved, shortfall);
                }
                else
                {
                    Notifier.ShowFailure(piece.m_name, result);
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogError($"Pull failed for {piece.m_name}: {e}");
                Notifier.ShowError();
            }
            finally
            {
                _busy = false;
            }
        }
    }
}
