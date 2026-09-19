# Phase 06 — Multiplayer & dedicated-server verification

**Depends on:** 01 (dev profile, csproj), 02 (tests in build gate), 03
(admin-only config sync), 04 (pull feature incl. ownership claim), 05 (hint) ·
**Enables:** 07 (release only after this matrix passes)

## Goal
Show that PullMats behaves correctly as a listen-server host and as a client of
a local Valheim Dedicated Server. That covers: no item duplication or loss,
chests other players have open and other players' wards are respected,
admin-only settings sync when the server has the mod, and joining works whether
or not either side has the mod or the versions match. Fix any failing scenario.
The deliverable is a repeatable test checklist in the repo.

## Files touched
- `docs/test-checklist.md`: the matrix below, with a results table (date, build, mode, pass/fail).
- `scripts/run-dedicated-server.sh`: syncs BepInEx + mods from a Gale profile into the dedicated server install and starts it.
- Code under `src/PullMats/` only if a scenario fails. Each fix gets its own
  `fix(mp): …` commit that references the checklist row, and the whole matrix is
  rerun afterwards.

## Steps
1. Install **Valheim Dedicated Server** (Steam → Library → Tools, app 896660)
   into `/games/SteamLibrary/steamapps/common/Valheim dedicated server`.
2. **Gale profile for the "no PullMats" client:** in Gale, clone `pullmats-dev`
   to `pullmats-nomod` and disable `Spronglehump-PullMats` in it. The phase 01
   deploy target doesn't write there, because `GaleProfileDir` points at
   `pullmats-dev`.
3. Write `scripts/run-dedicated-server.sh` (bash, `set -euo pipefail`):
   - Arguments:
     - `--no-pullmats` excludes `Spronglehump-PullMats` from the sync.
     - `--pullmats-dll <path>` makes the server use that DLL instead of the
       profile's copy.
   - `SERVER_DIR=/games/SteamLibrary/steamapps/common/Valheim dedicated server`
   - `PROFILE=$HOME/.local/share/com.kesomannen.gale/valheim/profiles/pullmats-dev`
   - `rsync -a --delete` these from `$PROFILE` into `$SERVER_DIR`:
     `BepInEx/core/`, `BepInEx/plugins/`, `doorstop_libs/`, `doorstop_config.ini`.
     When `--no-pullmats` is given, the plugins sync adds
     `--exclude Spronglehump-PullMats --delete-excluded`, so a copy left over
     from an earlier run is removed.
   - `BepInEx/config/` is synced with `rsync -a --ignore-existing`. Configs
     are seeded on the first run, and server-side edits (e.g. the MP-6
     `Spronglehump.PullMats.cfg` values) are never overwritten by the client
     profile.
   - With `--pullmats-dll`, copy that file over
     `$SERVER_DIR/BepInEx/plugins/Spronglehump-PullMats/PullMats.dll`.
   - `cd "$SERVER_DIR"`, then export the same variables as BepInExPack's Linux
     `start_server_bepinex.sh`:
     - `DOORSTOP_ENABLED=1`
     - `DOORSTOP_TARGET_ASSEMBLY=./BepInEx/core/BepInEx.Preloader.dll`
     - `LD_LIBRARY_PATH=./doorstop_libs:./linux64:$LD_LIBRARY_PATH`
     - `LD_PRELOAD=libdoorstop_x64.so:$LD_PRELOAD`
     - `SteamAppId=892970`
   - `exec ./valheim_server.x86_64 -name PullMatsTest -port 2456 -world PullMatsTest -password pullmats -public 0`
4. Admin setup: the client's Steam ID (shown in the F2 panel) goes on its own
   line in `~/.config/unity3d/IronGate/Valheim/adminlist.txt` **only for MP-6a**.
   It's removed again (the file left empty) for every other row. The server
   rereads the list on restart.
5. **Mismatched build for MP-8:** set `Plugin.Version` to `0.1.1`, run
   `dotnet build -c Release -p:Version=0.1.1 -p:OutDir=$PWD/dist/mp8/` (the
   deploy target still copies to the dev profile, so rebuild normally
   afterwards), revert `Plugin.Version`, and start the server with
   `--pullmats-dll dist/mp8/PullMats.dll`.
6. **Second player:** a friend with their own Steam account installs the
   `pullmats-dev` mod set (share the Gale profile via Gale's export code) and
   joins the local dedicated server. Forward port 2456-2457 UDP if they're
   not on the LAN. MP-3 and MP-4 need them, and the release is blocked until
   both pass.
7. Run the matrix and record the results. **Modes:** `L` = you host with
   "Start server" ticked in `pullmats-dev`. `D` = you're a client of the local
   dedicated server.

   | Row | Modes | Setup | Expected |
   |---|---|---|---|
   | MP-1 Basic pull | L, D | Stocked chest; pull Portal ×2; log out and back in | Inventory +2 sets; chest −2 sets; no dupes after relog |
   | MP-2 Restart persistence | D | Pull ×1, stop server (Ctrl-C), restart, rejoin | Chest counts still reflect the pull |
   | MP-3 Chest in use | D | Friend opens the stocked chest, you pull | That chest is skipped (Missing if it's the only source); after they close it, the pull succeeds and they see the new counts on reopening |
   | MP-4 Foreign ward | D | Friend places a ward (you not permitted) covering a stocked chest they built | That chest is skipped |
   | MP-5 Server without PullMats | D | Server with `--no-pullmats` | You join; pull works; your local config values apply |
   | MP-6a Sync, admin | D | Server config `Pull mode = TopUp`, `Allowed tools = Hammer,Hoe`; you're in adminlist | Your client shows those values (editable); TopUp and Hoe behave accordingly |
   | MP-6b Sync, non-admin | D | Same server config; adminlist empty | Values shown read-only and applied; after disconnecting, your local values are back |
   | MP-7 Client without PullMats | D | Server with PullMats; you join from `pullmats-nomod` | Join succeeds, no errors in either log |
   | MP-8 Version mismatch | D | Server with the 0.1.1 DLL (step 5); client on 0.1.0 | Join succeeds; pull works |
   | MP-9 Rapid presses | L, D | Stock for 50 sets; mash N for 3 s | Items moved = accepted presses (count of success messages) × set; no negative stacks; no dupes after relog |

8. If a row fails, fix it in the owning file from phases 03–05, rerun that row
   in every mode it lists, then rerun MP-1 and MP-9 as regression checks.

## Build gate
- `dotnet build -c Release` has 0 errors.
- `dotnet test tests/PullMats.Tests` passes.
- In `docs/test-checklist.md`, every row is marked pass for every mode in its
  **Modes** column.

## Test plan
The matrix above is the test plan. Also rerun the single-player scenarios from
phase 04 once after any code fix made in this phase.

## Commit
`test: add multiplayer/dedicated-server checklist and server launch script`,
with fixes (if any) as separate `fix(mp): …` commits.

## Rollback
The checklist and script are additive and safe to keep. A fix commit made here
can be reverted on its own. If it's reverted, the matching MP rows go back to
failing and have to be re-examined before release. Remove `dist/mp8/` and
restore the normal server DLL by running the script without `--pullmats-dll`.
