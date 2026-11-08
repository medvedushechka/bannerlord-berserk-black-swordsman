Bannerlord UpgradeGuard v6 — экспериментальный точечный патч совместимости
=============================================================================

Цель: предотвратить сбой PartyCharacterVM.InitializeUpgrades, когда список
улучшений, возвращаемый BetterBanditsPlus, не совпадает с тем, что использует
интерфейс игры. Установлено по логам и разбору IL:
- BetterBanditsPlus фильтрует UpgradeTargets для бандитов.
- My Little Warband увеличивает список Upgrades при нехватке, но не сокращает.
- Исходный метод игры обращается к UpgradeTargets несколько раз.
- На последнем вылете mountain_bandits_bandit: requestedIndex=1, targetsLength=1.

Что делает v6:
1. Только в PartyCharacterVM.InitializeUpgrades заменяет 2 вызова
   get_UpgradeTargets на вызов через Reflection, чтобы не обходить Harmony
   postfix BetterBanditsPlus из-за возможного JIT-inlining.
2. После префикса My Little Warband убирает лишние элементы списка Upgrades,
   если их больше, чем фактических доступных целей.
3. Оставляет диагностические записи v5. Сохраняет защиту GetUpgradeGoldCost
   с временной стоимостью 1 000 000 для ДРУГИХ необработанных ошибок; для
   исправленного случая она больше не должна срабатывать.

Ограничения:
- Компиляция и запуск v6 на Windows не проверены в этой среде.
- Гипотеза про обход Harmony-патча из-за JIT-inlining пока не доказана.
- Вызов через Reflection немного медленнее, но только при открытии экрана
  отряда; на игровой бой не влияет.
- Сохранения, DLL и XML других модов установщик не изменяет.
- Если после установки лог покажет getterCallsReplaced не равным 2, НЕ
  проверяйте вылет: пришлите лог, и мы скорректируем патч.

Установка:
1. Сохраните резервную копию сохранений, полностью закройте игру.
2. Распакуйте ZIP, запустите в Windows PowerShell:
   powershell.exe -NoProfile -ExecutionPolicy Bypass -File "ПУТЬ\Install_Bannerlord_UpgradeGuard_v6.ps1"
3. В лаунчере включите Bannerlord UpgradeGuard после Harmony.
4. Запустите игру до главного меню, проверьте лог:
   Get-Content "$env:LOCALAPPDATA\BannerlordUpgradeGuard\upgrade_guard.log" -Tail 40
5. Должны быть ACTIVE UI_COMPAT и UI_COMPAT_TRANSPILE getterCallsReplaced=2.
6. Только затем загрузите копию сохранения и откройте отряд.

Откат: закройте игру и выполните установщик с -Remove.
Установщик удаляет только модуль BannerlordUpgradeGuard.
