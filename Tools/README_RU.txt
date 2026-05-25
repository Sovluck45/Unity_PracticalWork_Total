ИСПРАВЛЕНИЕ SAFE MODE / EPERM / MIRROR / AR
============================================

ПРИЧИНА
-------
Unity не смог переименовать папки в Library\PackageCache (EPERM).
Пакеты AR и Mirror установились наполовину → сотни ошибок CS0234.

ПОРЯДОК (СТРОГО ПО ШАГАМ)
-------------------------
1. Закройте только окно Unity Editor (проект).
   Unity Hub может оставаться запущенным.
   Диспетчер задач: завершите процесс Unity.exe (не Unity Hub).

2. Дважды щёлкните по файлам:
   Tools\1_CleanPackageCache.cmd
   Tools\2_InstallMirror.cmd

   Или в PowerShell:
   cd C:\Users\kosty\PracticalWork\Tools
   powershell -ExecutionPolicy Bypass -File .\CleanPackageCache.ps1
   powershell -ExecutionPolicy Bypass -File .\InstallMirror.ps1
   Должна появиться папка: Assets\Mirror

4. Если EPERM повторяется:
   - Временно отключите антивирус для папки PracticalWork
   - Не открывайте Library в Проводнике во время импорта
   - Запустите PowerShell не от администратора (от вашего пользователя)

5. Откройте проект в Unity.
   Safe Mode → Exit Safe Mode (если Console без красных ошибок)

6. MVR → Setup Full Project (All 5 Labs)

EPERM (operation not permitted)
-------------------------------
Часто блокирует com.unity.ai.assistant — пакет УДАЛЁН из manifest.
Если EPERM снова:
  - Закройте Unity Editor
  - 1_CleanPackageCache.cmd
  - Добавьте PracticalWork в исключения Защитника Windows
  - Не нажимайте Continue — лучше Retry после очистки Library

MIRROR
------
- Git URL и OpenUPM для новых версий Mirror НЕ работают.
- Используйте только Tools\InstallMirror.ps1 → Assets\Mirror

AR FOUNDATION (Unity 6.4)
-------------------------
Нужна версия 6.3.4+ (6.0.x ломается с URP 17 на Unity 6000.4).
В manifest: arfoundation 6.3.4, arcore 6.3.4.

После смены manifest:
  1. Закройте Unity Editor (окно проекта)
  2. Запустите 1_CleanPackageCache.cmd
  3. Откройте проект — дождитесь импорта пакетов
