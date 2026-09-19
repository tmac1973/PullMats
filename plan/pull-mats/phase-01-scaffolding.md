# Phase 01 — Repo, build scaffolding & loading plugin

**Depends on:** nothing · **Enables:** every later phase (a project that builds,
deploys to the dev profile, and loads in-game)

## Goal
Set up an empty but real mod. That means a git repo on GitHub, an SDK-style
net472 project that compiles against the local Valheim install, Jotunn and
AzuCraftyBoxes, a dedicated Gale test profile, and a post-build step that copies
the DLL into that profile. The plugin only logs that it loaded. This shows the
whole toolchain works on Linux/Gale before any feature code is written.

## Files touched
- `.gitignore`: ignore `bin/`, `obj/`, `dist/`, `*.user`, `PullMats.user.props`, `.idea/`, `.vs/`.
- `Directory.Build.props`: shared MSBuild properties (`ValheimDir`, `GaleProfileDir`, derived `ManagedDir`, `BepInExCoreDir`, `PluginsDir`). Imports `PullMats.user.props` if it exists.
- `PullMats.user.props.example`: sample override file with the two path properties.
- `PullMats.sln`: solution containing `src/PullMats/PullMats.csproj`.
- `src/PullMats/PullMats.csproj`: net472 plugin project and its references, plus the `DeployToGale` target.
- `src/PullMats/Plugin.cs`: `BaseUnityPlugin` with attributes, static `Log`, and the loaded log line.
- `README.md`: one-paragraph stub (filled out properly in phase 07).

## Steps
1. **Create the Gale dev profile (manual, in Gale):** make a new Valheim profile
   named `pullmats-dev`. Install only `ValheimModding-Jotunn` (2.30.1),
   `Azumatt-AzuCraftyBoxes` (1.8.22, from Hexium) and
   `Azumatt-Official_BepInEx_ConfigurationManager`. Launch it once so BepInEx
   creates `BepInEx/config` and `BepInEx/plugins`, then quit.
2. `git init` in `/home/tim/Projects/vh_pull_mats`, default branch `main`.
   Leave the old prototype at `~/vh_pull_mats` alone; nothing is copied from it.
3. Write `Directory.Build.props`:
   - `ValheimDir` defaults to `/games/SteamLibrary/steamapps/common/Valheim`.
   - `GaleProfileDir` defaults to
     `$(HOME)/.local/share/com.kesomannen.gale/valheim/profiles/pullmats-dev`.
   - Derived paths: `ManagedDir=$(ValheimDir)/valheim_Data/Managed`,
     `BepInExCoreDir=$(GaleProfileDir)/BepInEx/core`,
     `PluginsDir=$(GaleProfileDir)/BepInEx/plugins`.
   - `<Import Project="$(MSBuildThisFileDirectory)PullMats.user.props" Condition="Exists(...)" />`
     comes before the defaults, and each default is guarded with
     `Condition="'$(X)' == ''"` so the user file wins.
4. Write `src/PullMats/PullMats.csproj`:
   - `TargetFramework net472`, `LangVersion latest`, `Nullable enable`,
     `AssemblyName PullMats`, `RootNamespace PullMats`,
     `AppendTargetFrameworkToOutputPath false`, `GenerateAssemblyInfo false`.
   - NuGet: `Microsoft.NETFramework.ReferenceAssemblies` 1.0.3 (PrivateAssets all),
     `BepInEx.AssemblyPublicizer.MSBuild` 0.4.3 (PrivateAssets all),
     `JotunnLib` 2.30.1 (`ExcludeAssets="runtime"`, so Jotunn.dll is never copied).
   - File references, all `<Private>false</Private>`:
     - `$(BepInExCoreDir)/BepInEx.dll` and `$(BepInExCoreDir)/0Harmony.dll`
     - `$(ManagedDir)/assembly_valheim.dll` with `Publicize="true"`
     - `$(ManagedDir)/assembly_utils.dll`, `assembly_guiutils.dll`, `Unity.TextMeshPro.dll`,
       `UnityEngine.dll`, `UnityEngine.CoreModule.dll`, `UnityEngine.UI.dll`,
       `UnityEngine.InputLegacyModule.dll`, `UnityEngine.PhysicsModule.dll`
     - `$(PluginsDir)/Azumatt-AzuCraftyBoxes/AzuCraftyBoxes.dll`
   - `DeployToGale` target, `AfterTargets="Build"`, only when
     `Exists('$(PluginsDir)')`: copies `$(TargetPath)` (and the `.pdb`) to
     `$(PluginsDir)/Spronglehump-PullMats/`.
5. Write `src/PullMats/Plugin.cs`:
   - `public const string Guid = "Spronglehump.PullMats", Name = "PullMats", Version = "0.1.0";`
   - `[BepInPlugin(Guid, Name, Version)]`. The attribute and every log line use
     these constants, never string literals, so a version bump only means
     changing `Version`.
   - `[BepInDependency(Jotunn.Main.ModGuid)]`
   - `[BepInDependency("Azumatt.AzuCraftyBoxes")]`
   - `[NetworkCompatibility(CompatibilityLevel.NotEnforced, VersionStrictness.None)]`.
     This means PullMats never blocks joining, whether or not the server has it,
     and whatever the version.
   - `Awake()` stores `Logger` in a static `Log` and logs `$"{Name} {Version} loaded"`.
6. Create the public GitHub repo:
   `gh repo create tmac1973/PullMats --public --source . --remote origin`.
   Push after the first commit.

## Build gate
- `dotnet build -c Release` in the repo root finishes with 0 errors.
- `src/PullMats/bin/Release/PullMats.dll` exists and was copied to
  `.../profiles/pullmats-dev/BepInEx/plugins/Spronglehump-PullMats/PullMats.dll`.

## Test plan
- Launch Valheim through Gale with `pullmats-dev`. `BepInEx/LogOutput.log`
  contains `PullMats 0.1.0 loaded`, after the Jotunn and AzuCraftyBoxes load lines.
- Temporarily remove CraftyBoxes from the profile and launch. BepInEx should log
  that PullMats was skipped for a missing dependency. Then reinstall CraftyBoxes.
- `PullMats.user.props` override check: point `GaleProfileDir` at another
  profile and confirm the deploy target follows it. Then delete the file.

## Commit
`chore: scaffold PullMats plugin project with Gale deploy`

## Rollback
Delete the repo contents and the `Spronglehump-PullMats` folder in the dev
profile. Nothing outside the repo and that folder is touched. The Gale profile
itself can be deleted from Gale.
