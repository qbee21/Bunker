#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class BunkerLimitNoticeInstaller
{
    const string PrefabPath = "Assets/Prefabs/AlliedUnitLimitNotice.prefab";

    [MenuItem("Tools/Bunker Defense/Install Limit Notice")]
    static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
        {
            EditorUtility.DisplayDialog("UI лимита", "Остановите Play Mode и дождитесь компиляции.", "OK");
            return;
        }
        GameCore game = Selection.activeGameObject != null ? Selection.activeGameObject.GetComponent<GameCore>() : null;
        if (game == null)
        {
            GameCore[] games = Resources.FindObjectsOfTypeAll<GameCore>()
                .Where(g => !EditorUtility.IsPersistent(g) && g.gameObject.scene.IsValid() && g.gameObject.scene.isLoaded).ToArray();
            if (games.Length == 1) game = games[0];
        }
        if (game == null || game.battlePanel == null || EditorUtility.IsPersistent(game))
        {
            EditorUtility.DisplayDialog("UI лимита", "Откройте игровую сцену и выберите объект с GameCore в Hierarchy.", "OK");
            return;
        }
        if (game.alliedLimitNotice != null)
        {
            Selection.activeGameObject = game.alliedLimitNotice.gameObject;
            EditorUtility.DisplayDialog("UI лимита", "Надпись уже подключена. Новый экземпляр не создан.", "OK");
            return;
        }
        // Reuse an unassigned instance if it already exists; never duplicate or move existing UI.
        Text notice = game.battlePanel.GetComponentsInChildren<Text>(true)
            .FirstOrDefault(t => t.gameObject.name == "AlliedUnitLimitNotice");
        if (notice == null)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                EditorUtility.DisplayDialog("UI лимита", "Не найден префаб: " + PrefabPath, "OK");
                return;
            }
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, game.battlePanel.transform);
            Undo.RegisterCreatedObjectUndo(instance, "Add allied limit UI");
            notice = instance.GetComponent<Text>();
        }
        Undo.RecordObject(game, "Connect allied limit UI");
        game.alliedLimitNotice = notice;
        PrefabUtility.RecordPrefabInstancePropertyModifications(game);
        EditorUtility.SetDirty(game);
        EditorSceneManager.MarkSceneDirty(game.gameObject.scene);
        Selection.activeGameObject = notice.gameObject;
        EditorUtility.DisplayDialog("UI лимита подключён", "Надпись сохранена как UI-объект в Battle.\n" +
            "Сохраните сцену (Ctrl+S). В Play Mode новые UI-объекты не создаются.", "OK");
    }
}
#endif
