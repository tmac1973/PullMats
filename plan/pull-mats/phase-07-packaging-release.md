# Phase 07 — Packaging & Hexium release

**Depends on:** 01 (csproj, `Plugin.cs`), 02 (tests in the build gate), 03 (config table for README), 04 (feature), 05 (hint), 06 (MP matrix passing) · **Enables:**
public 1.0.0 release

## Goal
Produce a Thunderstore-format zip that imports cleanly into Gale and meets
Hexium's packaging rules. Write the README and CHANGELOG, bump to 1.0.0, tag
the release on GitHub, and upload by hand to Hexium.

## Files touched
- `package/manifest.json`: Thunderstore/Hexium manifest.
- `package/icon.png`: 256×256 icon.
- `README.md`: full user documentation (also packaged).
- `CHANGELOG.md`: release notes (also packaged).
- `src/PullMats/PullMats.csproj`: `Package` target that builds `dist/Spronglehump-PullMats-<version>.zip`.
- `src/PullMats/Plugin.cs`: version const → `1.0.0`.

## Steps
1. `package/manifest.json`:
   ```json
   {
     "name": "PullMats",
     "version_number": "1.0.0",
     "website_url": "https://github.com/tmac1973/PullMats",
     "description": "Hammer out, piece selected, press N: pulls one full set of that piece's materials from nearby chests (via AzuCraftyBoxes). All-or-nothing, never over-encumbers.",
     "dependencies": [
       "ValheimModding-Jotunn-2.30.1",
       "Azumatt-AzuCraftyBoxes-1.8.22"
     ]
   }
   ```
   The description must stay ≤ 256 characters. BepInExPack is left out because
   Hexium assumes it and strips it.
2. `package/icon.png`: generate with ImageMagick:
   `magick -size 256x256 xc:'#2b2118' -gravity center -fill '#e8c170' -font DejaVu-Sans-Bold -pointsize 44 -annotate +0-20 'PULL' -pointsize 44 -annotate +0+32 'MATS' package/icon.png`.
   Check it's exactly 256×256 with `magick identify`.
3. `README.md` sections:
   - What it does (one paragraph + a screenshot of the key hint and a success message)
   - Requirements (Jotunn, AzuCraftyBoxes)
   - Usage (hammer → piece → N; repeat presses stack sets)
   - The all-or-nothing rules (missing / too heavy / no space)
   - Config table (every entry from phase 03, with defaults and admin-only flags)
   - Server install (optional: syncs the admin-only settings, and never blocks
     joining)
   - CraftyBoxes interplay (range, YAML exclusions, and which storage types count)
   - Known limitations (carried bags are ignored; build pieces only)
   - Source link
4. `CHANGELOG.md`: a `## 1.0.0` entry listing the features.
5. `Package` target in the csproj, run with `dotnet build -c Release -t:Package`
   (depends on `Build`):
   1. Stage `package/manifest.json`, `package/icon.png`, `README.md`,
      `CHANGELOG.md` and `PullMats.dll` into `obj/package/`, with the DLL at
      `obj/package/plugins/PullMats.dll`.
   2. Zip it with the `ZipDirectory` MSBuild task to
      `dist/Spronglehump-PullMats-$(Version).zip`.
   3. Fail the build if the manifest's `version_number` differs from the
      csproj `$(Version)`. Use `ReadLinesFromFile` on `package/manifest.json`,
      pull the value out with the MSBuild property function
      `$([System.Text.RegularExpressions.Regex]::Match(...,'"version_number":\s*"([^"]+)"').Groups[1].Value)`,
      then `<Error Condition="'$(ManifestVersion)' != '$(Version)'" />`.
      `Plugin.Version` in code must be set to the same value. The step 6 edit
      keeps the two in line.
6. Set `Plugin.Version` and the csproj `<Version>` to `1.0.0`.
7. Install the zip in Gale (Import → local mod) into a new empty profile. Gale
   should resolve Jotunn and CraftyBoxes, and the game should load
   `PullMats 1.0.0`.
8. Commit, `git tag v1.0.0`, push, then
   `gh release create v1.0.0 dist/Spronglehump-PullMats-1.0.0.zip --notes-file CHANGELOG.md`.
9. Upload `dist/Spronglehump-PullMats-1.0.0.zip` by hand through Hexium's
   submit page for Valheim, under team `Spronglehump`. Confirm the package page
   shows both dependencies, then install it from Hexium in Gale once as a final
   check.

## Build gate
- `dotnet test tests/PullMats.Tests` passes.
- `dotnet build -c Release -t:Package` produces `dist/Spronglehump-PullMats-1.0.0.zip`.
- `unzip -l` shows `manifest.json`, `icon.png`, `README.md`, `CHANGELOG.md` and
  `plugins/PullMats.dll` at the expected paths.

## Test plan
- Fresh Gale profile, local import of the zip → dependencies are pulled
  automatically and the game loads PullMats 1.0.0.
- In that profile, run a quick subset of the phase 04 scenarios (stocked Portal
  pull, Missing, Too heavy).
- After the Hexium upload, install from Hexium in another fresh profile and
  repeat the stocked Portal pull.

## Commit
`chore(release): package PullMats 1.0.0 for Hexium`

## Rollback
Delete the GitHub release/tag and hide or deprecate the Hexium version from its
package page. Packaging files are additive and can stay in the repo.
