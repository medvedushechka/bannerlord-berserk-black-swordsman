# BERSERK: Black Swordsman — Bannerlord Overhaul

**Язык: [Русский](#русский) · [English](#english)**

<a id="русский"></a>

## Русский

**Неофициальная сборка модов для Mount & Blade II: Bannerlord в духе Berserk.**

Мрачные сражения, тяжёлое оружие, расчленение, усиленные бандиты, масштабные битвы, осады и расширенные возможности управления отрядами и королевством.

> **Статус: предварительная версия.** На GitHub опубликованы документация, манифест рабочей локальной конфигурации и исходники нашего исправления UpgradeGuard v6. **Nexus Collection ещё не опубликована**, поэтому установка всей сборки в один клик пока недоступна.

### Версии игры

| Компонент | Версия |
| --- | --- |
| Mount & Blade II: Bannerlord | **1.4.8.119303** |
| War Sails | **1.2.8.119303** |
| Модули в лаунчере | **41 из 41 включены** (8 официальных, 33 пользовательских) |
| Последний модуль | **BannerlordUpgradeGuard** (наш патч, итерация v6) |

Конфигурация экспортирована **8 ноября 2026 года** из реально используемой установки. Она работала в локальной кампании после исправления ошибок, **но ещё не прошла испытание чистой установкой на другом ПК**.

### Что входит в проект

- [**Точный порядок загрузки 41 модуля**](manifest/current-load-order-2026-11-08.csv) — основной файл для воспроизведения сборки.
- [Список модулей и их версий](manifest/installed-module-inventory-2026-11-08.csv).
- [Папки модов в Vortex](manifest/vortex-staging-folders-2026-11-08.csv) — справочный список, **не** список модов для установки.
- [Контрольные SHA-256 некоторых DLL](manifest/critical-dll-sha256.csv).
- [**UpgradeGuard v6**](patches/UpgradeGuard_v6/) — исходный код, установщик и сбор диагностических логов.
- [Инструкция по установке на русском](docs/INSTALL_RU.md).
- [План публикации Nexus Collection](docs/NEXUS_COLLECTION.md).
- [**Известная проблема Vortex/BUTR с пропадающими DLL (issue #196)**](docs/VORTEX_DLL_ISSUE_RU.md).

### Зачем нужен UpgradeGuard v6

В нашей кампании экран отряда вылетал при улучшении некоторых бандитов: интерфейс обращался к несуществующему варианту улучшения бойца. **UpgradeGuard v6** исправляет обработку вариантов улучшения через Harmony. Работа патча подтверждена локальными логами и последующими сражениями.

**Ограничение:** в патче пока остаётся аварийный запасной вариант стоимости улучшения в **1 000 000 золота** для неожиданных индексов. Перед стабильным релизом это поведение нужно дополнительно проверить. Патч не гарантирует совместимость с другими версиями игры.

### Важные замечания

1. Версии модов берём из установленной папки игры, а не автоматически из Vortex staging. Например, в игре: **BetterBanditsPlus 9.8.2** и **ImprovedGarrisons 4.2.0.7**; в staging обнаружены папки с более новыми номерами.
2. У **AutoTrader** в метаданных указана зависимость **CargoHolds**, которой нет среди 41 включённого модуля. Причину ещё предстоит проверить.
3. Перед публикацией Nexus Collection необходимо проверить источники загрузки, лицензии модов, зависимости и установку с чистого профиля Vortex.
4. **UpgradeGuard v6 не входит в 34 мода коллекции Vortex.** Его исходники и локальный установщик размещены в [GitHub](patches/UpgradeGuard_v6/). Патч экспериментальный: установщик привязан к пути `C:\\Games\\Mount and Blade II Bannerlord` и точному SHA-256 игровой DLL, поэтому не является универсальной установкой в один клик.
5. При установке через Vortex возможна отдельная [ошибка расширения BUTR](docs/VORTEX_DLL_ISSUE_RU.md): при неизвестном магазине игры DLL модов могут пропускаться. [Исходное обращение #196](https://github.com/BUTR/game-mount-and-blade2/issues/196).
4. **Чужие моды и игровые DLL в репозиторий не включены.** Их следует скачивать из оригинальных источников.

Это фанатский проект, не связанный официально с TaleWorlds или авторами *Berserk*.

---

<a id="english"></a>

## English

**Unofficial Berserk-themed modpack configuration for Mount & Blade II: Bannerlord.**

Focused on brutal melee combat, oversized swords, dismemberment, stronger bandits, large battles, sieges, and expanded party/kingdom gameplay.

> **Status: pre-release.** This GitHub repository contains documentation, a manifest of the working local configuration, and the UpgradeGuard v6 patch source. **The Nexus Collection has not been published yet**, so there is no one-click installation at this time.

### Required game versions

| Component | Version |
| --- | --- |
| Mount & Blade II: Bannerlord | **1.4.8.119303** |
| War Sails | **1.2.8.119303** |
| Launcher modules | **41/41 enabled** (8 official, 33 community/custom) |
| Last module | **BannerlordUpgradeGuard** (our v6 patch iteration) |

The configuration was exported from the actual local installation on **November 8, 2026**. It was used successfully in the local campaign after the compatibility fix, but **has not yet been validated through a clean install on another PC**.

### Repository contents

- [**Exact 41-module load order**](manifest/current-load-order-2026-11-08.csv) — authoritative launcher export.
- [Installed module inventory](manifest/installed-module-inventory-2026-11-08.csv).
- [Vortex staging folder inventory](manifest/vortex-staging-folders-2026-11-08.csv) — reference only, **not** an installation list.
- [SHA-256 hashes of selected DLLs](manifest/critical-dll-sha256.csv).
- [**UpgradeGuard v6**](patches/UpgradeGuard_v6/) — source, installer, and log collector.
- [Russian installation guide](docs/INSTALL_RU.md).
- [**Known Vortex/BUTR missing-DLL issue (GitHub #196, Russian guide)**](docs/VORTEX_DLL_ISSUE_RU.md).
- [Nexus Collection publication checklist](docs/NEXUS_COLLECTION.md).

### UpgradeGuard v6

The party screen previously crashed when a modded bandit upgrade attempted to access an invalid upgrade index. UpgradeGuard v6 uses Harmony to correct upgrade-target handling. Its activation was confirmed by local diagnostics and subsequent gameplay.

**Known limitation:** an emergency upgrade-cost fallback of **1,000,000 gold** remains in the patch for unexpected indices and requires further review before a stable release. Compatibility with other game versions is not guaranteed.

### Before a public Nexus Collection release

- Use the **installed game versions**, not newer staging-folder labels: **BetterBanditsPlus 9.8.2** and **ImprovedGarrisons 4.2.0.7** were observed in the game.
- Investigate the **AutoTrader → CargoHolds** dependency recorded in XML but absent from the enabled module list.
- Validate original mod sources, permissions, dependencies, and a clean Vortex profile installation.
- Do not redistribute third-party mod archives or game DLLs without permission.
- **UpgradeGuard v6 is not included among the 34 Vortex collection mods.** Its [source and local installer](patches/UpgradeGuard_v6/) are on GitHub, but the installer is hard-coded to the author's game path and a specific game DLL SHA-256; it is **not** a universal one-click patch.
- **Known Vortex/BUTR issue:** unknown game store detection can silently omit DLLs during mod installation. See [issue #196](https://github.com/BUTR/game-mount-and-blade2/issues/196) and [troubleshooting guide](docs/VORTEX_DLL_ISSUE_RU.md).

This is an unofficial fan project, not endorsed by TaleWorlds or the creators of *Berserk*.
