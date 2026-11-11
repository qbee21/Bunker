#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Editor-only utility. Never calls YG2.SaveProgress or any production cloud API.
public sealed class BunkerProgressResetWindow : EditorWindow
{
    static readonly string[] ProgressKeys = { "BunkerSave1", "BunkerSaveBeforeCloud" };
    bool resetTelegram = true;
    string lastBackup;

    [Serializable]
    public class LocalBackup
    {
        public bool hasProgress, hasCloudBackup, hasTelegram;
        public string progress, cloudBackup;
        public int telegram;
        public bool resetTelegram;
    }

    [MenuItem("Tools/Bunker Defense/Reset Progress")]
    static void Open()
    {
        GetWindow<BunkerProgressResetWindow>("Сброс прогресса");
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Сброс тестового прогресса", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Сбрасывает только прогресс этого проекта в Unity Editor: волны, монеты, " +
            "улучшения, оружие и слоты. Также очищает копию прогресса в редакторном сохранении YG2.\n\n" +
            "Язык, звук и чужие ключи сохраняются. Облако аккаунта Яндекса и сохранения браузера не затрагиваются.",
            MessageType.Info);
        resetTelegram = EditorGUILayout.ToggleLeft("Сбросить разовую награду Telegram", resetTelegram);
        bool busy = EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling;
        if (busy) EditorGUILayout.HelpBox("Сначала остановите Play Mode и дождитесь компиляции.", MessageType.Warning);
        using (new EditorGUI.DisabledScope(busy))
        {
            if (GUILayout.Button("Сбросить игровой прогресс")) ResetProgress();
        }
        if (!string.IsNullOrEmpty(lastBackup))
        {
            EditorGUILayout.HelpBox("Резервная копия: " + lastBackup, MessageType.Info);
            if (GUILayout.Button("Открыть резервную копию")) EditorUtility.RevealInFinder(lastBackup);
        }
    }

    void ResetProgress()
    {
        // Check again even if invoked without the window, or Play Mode changed after drawing.
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
        if (!EditorUtility.DisplayDialog("Сброс тестового прогресса",
            "Начать со свежего прогресса при следующем запуске Play Mode?\n" +
            "Перед сбросом будет создана резервная копия. Язык и звук сохранятся.\n" +
            "Реальное облако Яндекса не изменяется.", "Сбросить", "Отмена")) return;

        LocalBackup local = CaptureLocal(resetTelegram);
        string sdkPath = null, originalSdk = null, resetSdk = null;
        bool sdkWritten = false;
        try
        {
#if Storage_yg
            sdkPath = Path.Combine(YG.InfoYG.PATCH_PC_EDITOR, "SavesEditorYG2.json");
            if (File.Exists(sdkPath))
            {
                originalSdk = File.ReadAllText(sdkPath);
                // Parse and validate BEFORE touching either persistence store.
                resetSdk = PrepareEditorSave(originalSdk, resetTelegram);
            }
#endif
            string project = Path.GetDirectoryName(Application.dataPath);
            string folder = Path.Combine(project, "Library", "BunkerDefenseProgressBackups",
                DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff") + "_" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "UnityPlayerPrefs.json"), JsonUtility.ToJson(local, true));
            if (originalSdk != null) File.WriteAllText(Path.Combine(folder, "SavesEditorYG2.json"), originalSdk);
            File.WriteAllText(Path.Combine(folder, "README_RU.txt"),
                "Снимок до сброса. UnityPlayerPrefs.json содержит только игровые ключи Unity PlayerPrefs.\n" +
                "SavesEditorYG2.json (если был) — полная редакторная копия YG2 до сброса.\n" +
                "Это не резервная копия облака или браузерного прогресса Яндекса.\n");
            lastBackup = folder;
            if (resetSdk != null)
            {
                // Backup is already durable. Roll back both stores on a write failure.
                sdkWritten = true;
                File.WriteAllText(sdkPath, resetSdk);
            }
            foreach (string key in ProgressKeys) PlayerPrefs.DeleteKey(key);
            if (resetTelegram) PlayerPrefs.DeleteKey("telegram");
            PlayerPrefs.Save();
            // No SDK singleton initialization or save callbacks in Edit Mode.
            // YG2.InitStorage reads the edited file on the next Play Mode launch.
            AssetDatabase.Refresh();
            Debug.Log("Bunker Defense: тестовый прогресс сброшен. Резервная копия: " + folder);
            EditorUtility.DisplayDialog("Готово", "Прогресс сброшен. Следующий Play Mode начнётся заново.\n" +
                "Резервная копия: " + folder, "OK");
        }
        catch (Exception error)
        {
            try
            {
                if (sdkWritten && originalSdk != null) File.WriteAllText(sdkPath, originalSdk);
                RestoreLocal(local);
            }
            catch (Exception rollbackError) { Debug.LogError("Bunker reset: ошибка восстановления. Копия: " + lastBackup + "\n" + rollbackError); }
            Debug.LogError("Bunker reset: сброс не завершён. " + error);
            EditorUtility.DisplayDialog("Ошибка сброса", "Сброс не завершён. Проверьте Console.\n" +
                "Не удаляйте резервную копию: " + lastBackup, "OK");
        }
    }

    static LocalBackup CaptureLocal(bool telegram)
    {
        return new LocalBackup
        {
            hasProgress = PlayerPrefs.HasKey(ProgressKeys[0]), progress = PlayerPrefs.GetString(ProgressKeys[0], ""),
            hasCloudBackup = PlayerPrefs.HasKey(ProgressKeys[1]), cloudBackup = PlayerPrefs.GetString(ProgressKeys[1], ""),
            hasTelegram = PlayerPrefs.HasKey("telegram"), telegram = PlayerPrefs.GetInt("telegram", 0), resetTelegram = telegram
        };
    }
    static void RestoreLocal(LocalBackup b)
    {
        RestoreString(ProgressKeys[0], b.hasProgress, b.progress);
        RestoreString(ProgressKeys[1], b.hasCloudBackup, b.cloudBackup);
        if (b.resetTelegram)
        {
            if (b.hasTelegram) PlayerPrefs.SetInt("telegram", b.telegram); else PlayerPrefs.DeleteKey("telegram");
        }
        PlayerPrefs.Save();
    }
    static void RestoreString(string key, bool exists, string value)
    {
        if (exists) PlayerPrefs.SetString(key, value); else PlayerPrefs.DeleteKey(key);
    }
#if Storage_yg
    internal static string PrepareEditorSave(string json, bool telegram)
    {
        if (string.IsNullOrWhiteSpace(json) || !json.TrimStart().StartsWith("{"))
            throw new InvalidDataException("Редакторное сохранение YG2 повреждено; сброс отменён.");
        YG.SavesYG save = JsonUtility.FromJson<YG.SavesYG>(json);
        if (save == null) throw new InvalidDataException("Не удалось прочитать сохранение YG2.");
        save.bunkerProgress = null;
#if RedefinePlayerPrefs_yg
        save.stringKeys = save.stringKeys ?? new List<string>();
        save.stringValues = save.stringValues ?? new List<string>();
        save.intKeys = save.intKeys ?? new List<string>();
        save.intValues = save.intValues ?? new List<int>();
        save.floatKeys = save.floatKeys ?? new List<string>();
        save.floatValues = save.floatValues ?? new List<float>();
        ClearKeys(save.stringKeys, save.stringValues, telegram);
        ClearKeys(save.intKeys, save.intValues, telegram);
        ClearKeys(save.floatKeys, save.floatValues, telegram);
#endif
        // Keep idSave and all other current SavesYG fields, including language and mute.
        return JsonUtility.ToJson(save, true);
    }
    static void ClearKeys<T>(List<string> keys, List<T> values, bool telegram)
    {
        if (keys == null && values == null) return;
        if (keys == null || values == null || keys.Count != values.Count)
            throw new InvalidDataException("Несогласованные списки PlayerPrefs в YG2; сброс отменён.");
        for (int i = keys.Count - 1; i >= 0; i--)
            if (keys[i] == ProgressKeys[0] || keys[i] == ProgressKeys[1] || (telegram && keys[i] == "telegram"))
            { keys.RemoveAt(i); values.RemoveAt(i); }
    }
#endif
}
#endif
