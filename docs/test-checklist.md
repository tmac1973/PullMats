# PullMats multiplayer test checklist

Run before every release. **Modes:** `L` = you host from `pullmats-dev` with "Start server" ticked.
`D` = you're a client of the local dedicated server (`scripts/run-dedicated-server.sh`).

## Setup

- **Dedicated server:** `scripts/run-dedicated-server.sh` (add `--no-pullmats` or `--pullmats-dll <path>` per row).
  Join via *Join game → Add server* `127.0.0.1:2456`, password `pullmats`.
- **Admin list:** `.../Valheim dedicated server/pullmats-save/adminlist.txt`. Put your Steam ID
  (`76561197980064368`) on its own line **only for MP-6a**; leave the file empty for every other row.
  Restart the server after changing it.
- **Server-side PullMats config:** `.../Valheim dedicated server/BepInEx/config/Spronglehump.PullMats.cfg`.
  Reset it to defaults (`Pull mode = FullSet`, `Allowed tools = Hammer`) after MP-6.
- **`pullmats-nomod` profile (MP-7):** in Gale, clone `pullmats-dev` to `pullmats-nomod` and disable
  `Spronglehump-PullMats` there.
- **Mismatched build (MP-8):** set `Plugin.Version` to `0.1.1`, run
  `dotnet build -c Release -p:Version=0.1.1 -p:OutDir=$PWD/dist/mp8/`, revert `Plugin.Version`, rebuild
  normally, then start the server with `--pullmats-dll dist/mp8/PullMats.dll`.
- **Second player (MP-3, MP-4):** a friend with their own Steam account installs the `pullmats-dev` mod set
  (Gale profile export code) and joins. Forward UDP 2456–2457 if they're not on your LAN.
- **Stock:** `devcommands` (needs the `-console` launch arg) then `spawn`, e.g. `spawn Wood 100`,
  `spawn FineWood 50`, `spawn SurtlingCore 10`, `spawn GreydwarfEye 30`.

## Matrix

| Row | Modes | Setup | Expected |
|---|---|---|---|
| MP-1 Basic pull | L, D | Stocked chest; pull Portal ×2; log out and back in | Inventory +2 sets; chest −2 sets; no dupes after relog |
| MP-2 Restart persistence | D | Pull ×1, stop server (Ctrl-C), restart, rejoin | Chest counts still reflect the pull |
| MP-3 Chest in use | D | Friend opens the stocked chest, you pull | That chest is skipped (Missing if it's the only source); after they close it the pull succeeds and they see the new counts on reopening |
| MP-4 Foreign ward | D | Friend places a ward (you not permitted) covering a stocked chest they built | That chest is skipped |
| MP-5 Server without PullMats | D | Server with `--no-pullmats` | You join; pull works; your local config values apply |
| MP-6a Sync, admin | D | Server config `Pull mode = TopUp`, `Allowed tools = Hammer,Hoe`; you're in adminlist | Your client shows those values (editable); TopUp and Hoe behave accordingly |
| MP-6b Sync, non-admin | D | Same server config; adminlist empty | Values shown read-only and applied; after disconnecting your local values are back |
| MP-7 Client without PullMats | D | Server with PullMats; you join from `pullmats-nomod` | Join succeeds, no errors in either log |
| MP-8 Version mismatch | D | Server with the 0.1.1 DLL; client on 0.1.0 | Join succeeds; pull works |
| MP-9 Rapid presses | L, D | Stock for 50 sets; mash N for 3 s | Items moved = accepted presses (count of success messages) × set; no negative stacks; no dupes after relog |

If a row fails: fix it, rerun that row in every listed mode, then rerun MP-1 and MP-9.

## Results

| Date | Build (commit) | Row | Mode | Result | Notes |
|---|---|---|---|---|---|
