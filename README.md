# BERSERK: Black Swordsman — Bannerlord Overhaul

**Community modpack manifest — PRE-RELEASE / not yet a published Nexus Collection.**

An unofficial dark-fantasy / *Berserk*-themed selection for **Mount & Blade II: Bannerlord 1.4.8.119303** and **War Sails 1.2.8.119303**. Focus: heavy melee, dismemberment, oversized swords, tougher bandits, large-scale battles, mercenary and kingdom systems.

> This repository documents the **actual selected launcher configuration** exported from the author's PC on **2026-11-08**. It is not a verified clean-install package and it does not include third-party mod binaries.

## Verified local configuration

- **41/41 modules enabled** in `LauncherData.xml`: **8 official + 33 community/custom**.
- **41st (last) module:** `BannerlordUpgradeGuard` (local `SubModule.xml` version `v1.0.4`, corresponding to the project's v6 compatibility-patch iteration).
- `BetterBanditsPlus`: **v9.8.2** in the game; staging also contains a folder labelled v9.8.4. **Use the game version for this manifest.**
- `ImprovedGarrisons`: **v4.2.0.7** in the game; staging contains a folder labelled v4.3.0.3.
- Launcher XML modified at **2026-11-08 02:47:59** local; exported at **2026-11-08 21:20:04** local.
- The reported local campaign ran through further battles and troop upgrades after applying UpgradeGuard v6; this is **not** a clean-install stability guarantee.

See [`manifest/current-load-order-2026-11-08.csv`](manifest/current-load-order-2026-11-08.csv) for the **complete 41-module order**. Do not substitute a Vortex staging folder list for the launcher order.

## Contents

- `manifest/current-load-order-2026-11-08.csv` — authoritative *observed* order and enabled state on the source PC.
- `manifest/installed-module-inventory-2026-11-08.csv` — installed module metadata.
- `manifest/vortex-staging-folders-2026-11-08.csv` — staging folder names (for provenance checks; **not** an install list).
- `manifest/critical-dll-sha256.csv` — hashes of a few critical binaries from the tested local installation.
- `patches/UpgradeGuard_v6/` — original patch source, installer and log collector.
- `docs/INSTALL_RU.md` — installation prerequisites and cautions.
- `docs/NEXUS_COLLECTION.md` — collection publication checklist.
- `tools/Export-BerserkLoadOrder.ps1` — read-only launcher exporter.

## Why UpgradeGuard exists

The party upgrade screen previously crashed on a bandit troop with a single valid upgrade target because another mod caused the UI to request an invalid upgrade index. The patch uses Harmony to adjust upgrade-target handling in `PartyCharacterVM.InitializeUpgrades`. Its activation was confirmed in local diagnostics. It also contains an **emergency cost fallback of 1,000,000 gold** for unexpected upgrade indices; this needs further review before a stable public release.

**The patch is not a substitute for verifying compatibility of the original mods.** Do not install it on a different game build without testing.

## Known release blockers

1. Nexus mod/file IDs and download URLs have not been validated for every module. Some modules may be manually installed or not on Nexus.
2. `AutoTrader` declares `CargoHolds` in its XML dependencies, which is absent from the recorded launcher module list; verify whether this is optional/legacy before public release.
3. The actual Nexus Collection must be authored in Vortex and tested from a clean profile.
4. Confirm permissions and distribution terms for all third-party content. **Do not upload their archives to this repository.**
5. This is a fan project, not endorsed by TaleWorlds or the creators of *Berserk*.

## Status

**Manifest complete; publication pending.** No Nexus Collection URL is claimed until it has actually been created and tested.
