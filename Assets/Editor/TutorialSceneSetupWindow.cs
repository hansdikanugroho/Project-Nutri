using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public class TutorialSceneSetupWindow : EditorWindow
{
    [MenuItem("Tools/Setup Tutorial Scene From MainDev")]
    public static void SetupTutorialScene()
    {
        // 1. Simpan scene aktif jika ada
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();

        // 2. Buka Main Dev scene untuk mengambil referensi
        Scene mainScene = EditorSceneManager.OpenScene("Assets/Scenes/Main Dev.unity", OpenSceneMode.Single);
        if (!mainScene.IsValid())
        {
            Debug.LogError("Main Dev.unity tidak ditemukan!");
            return;
        }

        // Cari Canvas dan Game Systems
        GameObject canvasSource = GameObject.Find("Canvas");
        GameObject bgSource = GameObject.Find("BG") ?? GameObject.Find("Environment") ?? GameObject.Find("Desk");

        // Buka Scene Tutorial
        Scene tutScene = EditorSceneManager.OpenScene("Assets/Scenes/Tutorial.unity", OpenSceneMode.Single);
        
        Debug.Log("Scene Tutorial siap diatur dengan environment dan Flowchart!");
    }
}
