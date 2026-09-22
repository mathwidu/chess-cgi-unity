using UnityEditor;
using UnityEditor.SceneManagement;

public static class ChessCgiEditorPlay
{
    [MenuItem("Chess CGI/Abrir cena principal")]
    public static void OpenMainScene()
    {
        if (EditorApplication.isPlaying) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");
        EditorApplication.ExecuteMenuItem("Window/General/Game");
    }

    [MenuItem("Chess CGI/Jogar no Editor")]
    public static void PlayInEditor()
    {
        if (EditorApplication.isPlaying) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene("Assets/Scenes/Main.unity");
        EditorApplication.ExecuteMenuItem("Window/General/Game");
        EditorApplication.isPlaying = true;
    }
}
