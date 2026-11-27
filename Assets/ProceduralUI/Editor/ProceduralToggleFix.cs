using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class ProceduralToggleFix
{
    [MenuItem("Tools/Audio Procedural/Corregir toggles existentes")]
    static void Fix()
    {
        var controller = Object.FindFirstObjectByType<ProceduralPreviewController>();
        if (controller == null || controller.instruments == null) {
            EditorUtility.DisplayDialog("Toggles", "No se encontró el controlador.", "OK"); return;
        }
        foreach (var toggle in controller.instruments) {
            if (toggle == null) continue;
            var visual = toggle.GetComponent<ProceduralToggleVisual>();
            if (visual == null) visual = Undo.AddComponent<ProceduralToggleVisual>(toggle.gameObject);
            Undo.RecordObject(visual, "Configurar indicador");
            visual.Configure();
            EditorUtility.SetDirty(visual);
        }
        EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
        EditorUtility.DisplayDialog("Toggles", "Indicadores instalados. Guarda la escena y prueba en Play.", "OK");
    }
}
