using BepInEx;
using BepInEx.Logging;
using Jotunn.Utils;
using PullMats.Game;

namespace PullMats
{
    [BepInPlugin(Guid, Name, Version)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [BepInDependency("Azumatt.AzuCraftyBoxes")]
    [NetworkCompatibility(CompatibilityLevel.NotEnforced, VersionStrictness.None)]
    public class Plugin : BaseUnityPlugin
    {
        public const string Guid = "Spronglehump.PullMats";
        public const string Name = "PullMats";
        public const string Version = "0.1.0";

        internal static ManualLogSource Log = null!;

        private void Awake()
        {
            Log = Logger;
            PullMatsConfig.Bind(Config);
            PullInput.Register();
            PullController.Init();
            Log.LogInfo($"{Name} {Version} loaded");
        }

        private void Update()
        {
            PullInput.Tick();
        }
    }
}
