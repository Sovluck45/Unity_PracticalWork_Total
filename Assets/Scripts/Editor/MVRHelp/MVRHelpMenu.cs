#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class MVRHelpMenu
{
    [MenuItem("MVR/Setup Full Project (All 5 Labs)", false, 0)]
    [MenuItem("Window/MVR/Setup Full Project (All 5 Labs)", false, 2050)]
    public static void RunSetup()
    {
        if (EditorApplication.isCompiling)
        {
            EditorUtility.DisplayDialog(
                "MVR",
                "Подождите, Unity ещё компилирует скрипты.\nПовторите через несколько секунд.",
                "OK");
            return;
        }

        Type setupType = FindSetupType();
        if (setupType == null)
        {
            ShowCompileHelpDialog();
            return;
        }

        try
        {
            setupType.GetMethod("SetupFullProject", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                ?.Invoke(null, null);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorUtility.DisplayDialog(
                "MVR",
                "Ошибка при настройке. Подробности в Console (Ctrl+Shift+C).",
                "OK");
        }
    }

    [MenuItem("MVR/Убрать Mirror Examples (ошибка NetworkIdentity)", false, 75)]
    public static void RemoveMirrorExamples()
    {
        const string path = "Assets/Mirror/Examples";
        if (!AssetDatabase.IsValidFolder(path))
        {
            EditorUtility.DisplayDialog("MVR", "Папка Assets/Mirror/Examples уже удалена.", "OK");
            return;
        }

        if (EditorUtility.DisplayDialog("MVR", "Удалить Assets/Mirror/Examples?\nУбирает ошибку PrefabWithChildrenForClientScene.", "Удалить", "Отмена"))
        {
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.Refresh();
            Debug.Log("MVR: Mirror Examples removed.");
        }
    }

    [MenuItem("MVR/Помощь — меню не видно?", false, 100)]
    [MenuItem("Window/MVR/Помощь — меню не видно?", false, 2051)]
    public static void ShowHelp()
    {
        ShowCompileHelpDialog();
    }

    [MenuItem("MVR/Установить Mirror (скрипт)", false, 50)]
    public static void InstallMirrorHint()
    {
        string scriptPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Tools", "InstallMirror.ps1"));
        EditorUtility.DisplayDialog(
            "Установка Mirror",
            "1. Закройте Unity полностью\n" +
            "2. Закройте Unity, запустите:\n" +
            "   Tools\\1_CleanPackageCache.cmd\n" +
            "   Tools\\2_InstallMirror.cmd\n" +
            "3. (Или PowerShell:)\n" +
            "   powershell -ExecutionPolicy Bypass -File InstallMirror.ps1\n" +
            "4. Откройте проект снова\n\n" +
            "Mirror копируется в Assets\\Mirror (без Package Manager).",
            "OK");
        EditorGUIUtility.systemCopyBuffer = ".\\InstallMirror.ps1";
    }

    static Type FindSetupType()
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type type = assembly.GetTypes().FirstOrDefault(t => t.Name == "MVRProjectSetup");
            if (type != null)
                return type;
        }

        return null;
    }

    static void ShowCompileHelpDialog()
    {
        EditorUtility.DisplayDialog(
            "MVR — настройка недоступна",
            "Пункт меню есть, но полная настройка не запустится, пока в Console есть красные ошибки.\n\n" +
            "Что сделать:\n" +
            "1. Console: Ctrl+Shift+C\n" +
            "2. Package Manager: установите Mirror (MVR > Установить Mirror)\n" +
            "3. Дождитесь AR Foundation / ARCore из manifest.json\n" +
            "4. После исчезновения ошибок: MVR > Setup Full Project\n\n" +
            "Где меню: верхняя строка Unity — MVR или Window > MVR",
            "OK");
    }
}
#endif
