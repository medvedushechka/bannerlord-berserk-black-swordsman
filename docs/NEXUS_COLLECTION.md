# Nexus Mods Collection — подготовка публикации

**Статус:** черновик. Публичной коллекции пока нет. Текущая установка Bannerlord не изменяется.

## Название / Title

BERSERK: Black Swordsman — Bannerlord Overhaul

## Краткое описание / Summary

**Русский:** Неофициальная сборка Bannerlord в духе Berserk: тяжёлые мечи, жестокие сражения, расчленение, усиленные бандиты, масштабные осады и улучшенное управление армиями.

**English:** Unofficial Berserk-themed Bannerlord modpack focused on brutal melee, oversized swords, dismemberment, tougher bandits, large-scale sieges and enhanced army management.

## Полное описание / Description

### Русский

**BERSERK: Black Swordsman** — неофициальная подборка модификаций для Mount & Blade II: Bannerlord, ориентированная на суровые сражения и атмосферу тёмного фэнтези.

**Особенности:**
- Тяжёлые сражения, крупные мечи и жестокие добивания.
- Расчленение и более зрелищные бои.
- Расширенные возможности отрядов, наёмников, гарнизонов и армий.
- Более опасные бандиты и насыщенные осады.
- Собственный экспериментальный патч **UpgradeGuard v6**, исправляющий локально воспроизведённый сбой экрана улучшения войск.

**Требуемые версии:** Bannerlord **1.4.8.119303**, War Sails **1.2.8.119303**.

**Важно:** сборка основана на рабочей локальной конфигурации из **41 включённого модуля**, но **ещё не проверена на чистой установке**. UpgradeGuard v6 экспериментальный; устанавливайте на свой риск и создавайте резервные копии сохранений. Совместимость с другими версиями игры не гарантируется.

**Техническая документация, порядок загрузки и исходники патча:** https://github.com/medvedushechka/bannerlord-berserk-black-swordsman

Сборка не является официальным продуктом TaleWorlds или создателей Berserk. Все сторонние моды принадлежат их авторам и должны загружаться из разрешённых источников.

### English

**BERSERK: Black Swordsman** is an unofficial Mount & Blade II: Bannerlord mod selection focused on brutal combat and a dark-fantasy atmosphere.

**Features:**
- Heavy melee combat, oversized swords and brutal finishing moves.
- Dismemberment and more intense battles.
- Expanded party, mercenary, garrison and army gameplay.
- Tougher bandits and large-scale sieges.
- Experimental custom **UpgradeGuard v6** compatibility patch for a locally reproduced troop-upgrade screen crash.

**Required versions:** Bannerlord **1.4.8.119303**, War Sails **1.2.8.119303**.

**Important:** this modpack is based on a locally working **41-enabled-module** configuration but **has not been clean-install tested**. UpgradeGuard v6 is experimental. Back up your saves and use at your own risk. Other game versions are not supported.

**Documentation, exact load order and patch source:** https://github.com/medvedushechka/bannerlord-berserk-black-swordsman

This is an unofficial fan project, not endorsed by TaleWorlds or the creators of Berserk. Third-party mods remain the property of their respective authors and must be obtained from permitted sources.

---

## Создание черновика через Vortex

1. Откройте Vortex, выберите **Mount & Blade II: Bannerlord** и убедитесь, что активен профиль с нашей рабочей сборкой. **Не обновляйте моды**.
2. Перейдите **Collections → Workshop → Create a collection → From Profile**.
3. Укажите название **BERSERK: Black Swordsman — Bannerlord Overhaul**.
4. Откройте **Edit** и проверьте список модов. Обратите внимание на пометки **Has Problems**, **Source**, **Install Type**.
5. Не публикуйте коллекцию до проверки источников всех модов, прав на распространение, AutoTrader → CargoHolds, корректного включения UpgradeGuard и теста установки с чистого профиля.
6. После исправления проблем нажмите **Upload**, чтобы отправить черновик на Nexus, затем заполните описание и изображения на сайте. **Upload не равен Publish**.
7. После успешного теста публикуйте первую ревизию и добавьте её URL в README.

**Документация:** https://modding.wiki/en/nexusmods/collections/create и https://modding.wiki/en/nexusmods/collections/create/publish

## Риски

- Список `LauncherData.xml` показывает реальный порядок запуска, но не гарантирует, что Vortex сможет скачать все моды.
- У `BetterBanditsPlus` и `ImprovedGarrisons` версии в папке игры и Vortex staging различаются; используйте проверенные версии из манифеста.
- `AutoTrader` ссылается на `CargoHolds`, отсутствующий в экспортированном списке.
- `BannerlordUpgradeGuard` — собственный экспериментальный патч; необходимо проверить способ его поставки и воспроизводимую сборку.
- Не загружайте в коллекцию чужие моды в обход разрешений, а в GitHub — чужие архивы, игровые DLL, личные сохранения или приватные файлы Vortex.
