using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class LoadingUIBuilder
{
    private static readonly Color BackgroundColor =
        new Color32(20, 17, 25, 255);

    private static readonly Color AccentColor =
        new Color32(233, 157, 197, 255);

    private static readonly Color TextColor =
        new Color32(248, 234, 243, 255);

    private static readonly Color SecondaryColor =
        new Color32(175, 162, 179, 255);

    [MenuItem("Tools/Audio Procedural/Disenar menu de inicio")]
    public static void BuildMenu()
    {
        Canvas canvas =
            Object.FindFirstObjectByType<Canvas>();

        if (canvas == null)
        {
            Debug.LogError("No se encontro el Canvas.");
            return;
        }

        if (canvas.gameObject.scene.name != "PreScene")
        {
            Debug.LogError(
                "Abre la escena PreScene primero."
            );
            return;
        }

        Button[] buttons =
            canvas.GetComponentsInChildren<Button>(true);

        if (buttons.Length != 1)
        {
            Debug.LogError(
                "El Canvas debe contener un unico boton."
            );
            return;
        }

        Button startButton = buttons[0];

        if (canvas.transform.Find("LoadingUI") != null)
        {
            Debug.LogWarning(
                "La interfaz ya fue construida."
            );
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(
            canvas.gameObject,
            "Disenar menu de inicio"
        );

        // Configurar Canvas.

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler =
            canvas.GetComponent<CanvasScaler>();

        if (scaler == null)
        {
            scaler = Undo.AddComponent<CanvasScaler>(
                canvas.gameObject
            );
        }

        scaler.uiScaleMode =
            CanvasScaler.ScaleMode.ScaleWithScreenSize;

        scaler.referenceResolution =
            new Vector2(1920, 1080);

        scaler.matchWidthOrHeight = 0.5f;

        // Crear contenedor visual.

        GameObject root = new GameObject(
            "LoadingUI",
            typeof(RectTransform)
        );

        Undo.RegisterCreatedObjectUndo(
            root,
            "Crear interfaz"
        );

        root.transform.SetParent(
            canvas.transform,
            false
        );

        RectTransform rootRect =
            root.GetComponent<RectTransform>();

        Stretch(rootRect);

        // Reutilizar el boton existente.

        Undo.SetTransformParent(
            startButton.transform,
            root.transform,
            "Mover boton existente"
        );

        // Fondo.

        Image background = CreateImage(
            "Background",
            root.transform,
            BackgroundColor
        );

        Stretch(background.rectTransform);

        background.transform.SetAsFirstSibling();

        // Encabezado.

        CreateText(
            "Header",
            root.transform,
            "MUSIC GENERATOR",
            23,
            AccentColor,
            new Vector2(0, 440),
            new Vector2(700, 45),
            FontStyle.Bold
        );

        // Decoracion de onda sonora.

        CreateWave(root.transform);

        // Titulo.

        CreateText(
            "Title",
            root.transform,
            "MUSIC GENERATOR",
            76,
            TextColor,
            new Vector2(0, 45),
            new Vector2(1500, 110),
            FontStyle.Bold
        );

        // Subtitulo.

        CreateText(
            "Subtitle",
            root.transform,
            "Generador procedural de acompanamientos musicales",
            30,
            SecondaryColor,
            new Vector2(0, -45),
            new Vector2(1500, 65),
            FontStyle.Normal
        );

        // Boton existente.

        RectTransform buttonRect =
            startButton.GetComponent<RectTransform>();

        SetRect(
            buttonRect,
            new Vector2(0, -180),
            new Vector2(340, 85)
        );

        Image buttonImage =
            startButton.GetComponent<Image>();

        if (buttonImage == null)
        {
            buttonImage = Undo.AddComponent<Image>(
                startButton.gameObject
            );
        }

        buttonImage.color = Color.white;

        startButton.targetGraphic = buttonImage;

        startButton.transition =
            Selectable.Transition.ColorTint;

        ColorBlock colors = startButton.colors;

        colors.normalColor = AccentColor;

        colors.highlightedColor =
            new Color32(255, 196, 225, 255);

        colors.pressedColor =
            new Color32(192, 112, 157, 255);

        colors.selectedColor = AccentColor;

        colors.colorMultiplier = 1f;

        colors.fadeDuration = 0.15f;

        startButton.colors = colors;

        // Ocultar texto anterior del boton.

        foreach (Transform child in startButton.transform)
        {
            child.gameObject.SetActive(false);
        }

        CreateText(
            "ButtonLabel",
            startButton.transform,
            "CONTINUAR",
            30,
            BackgroundColor,
            Vector2.zero,
            new Vector2(340, 85),
            FontStyle.Bold
        );

        startButton.transform.SetAsLastSibling();

        // Linea decorativa inferior.

        Image line = CreateImage(
            "FooterLine",
            root.transform,
            new Color32(65, 48, 65, 255)
        );

        SetRect(
            line.rectTransform,
            new Vector2(0, -405),
            new Vector2(600, 2)
        );

        // Footer.

        CreateText(
            "Footer",
            root.transform,
            "INGENIERIA EN MULTIMEDIA",
            21,
            SecondaryColor,
            new Vector2(0, -450),
            new Vector2(900, 50),
            FontStyle.Normal
        );

        // Guardar cambios de escena.

        EditorSceneManager.MarkSceneDirty(
            canvas.gameObject.scene
        );

        Selection.activeGameObject = root;

        Debug.Log(
            "Menu visual creado. " +
            "Se conserva el evento del boton existente."
        );
    }

    private static void CreateWave(Transform parent)
    {
        int bars = 65;

        for (int i = 0; i < bars; i++)
        {
            float t = (float)i / (bars - 1);

            float envelope = Mathf.Sin(t * Mathf.PI);

            float wave = Mathf.Abs(
                Mathf.Sin(i * 0.65f) *
                Mathf.Cos(i * 0.17f)
            );

            float height =
                8f + wave * envelope * 145f;

            Image bar = CreateImage(
                "Wave_" + i,
                parent,
                AccentColor
            );

            SetRect(
                bar.rectTransform,
                new Vector2(
                    (i - (bars - 1) / 2f) * 11f,
                    240f
                ),
                new Vector2(5f, height)
            );
        }
    }

    private static Image CreateImage(
        string objectName,
        Transform parent,
        Color color)
    {
        GameObject obj = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(Image)
        );

        obj.transform.SetParent(parent, false);

        Image image = obj.GetComponent<Image>();

        image.color = color;

        image.raycastTarget = false;

        return image;
    }

    private static Text CreateText(
        string objectName,
        Transform parent,
        string content,
        int fontSize,
        Color color,
        Vector2 position,
        Vector2 size,
        FontStyle style)
    {
        GameObject obj = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(Text)
        );

        obj.transform.SetParent(parent, false);

        Text text = obj.GetComponent<Text>();

        text.font =
            Resources.GetBuiltinResource<Font>(
                "LegacyRuntime.ttf"
            );

        text.text = content;

        text.fontSize = fontSize;

        text.fontStyle = style;

        text.color = color;

        text.alignment = TextAnchor.MiddleCenter;

        text.raycastTarget = false;

        SetRect(
            text.rectTransform,
            position,
            size
        );

        return text;
    }

    private static void SetRect(
        RectTransform rect,
        Vector2 position,
        Vector2 size)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);

        rect.anchorMax = new Vector2(0.5f, 0.5f);

        rect.pivot = new Vector2(0.5f, 0.5f);

        rect.localScale = Vector3.one;

        rect.localRotation = Quaternion.identity;

        rect.anchoredPosition = position;

        rect.sizeDelta = size;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;

        rect.anchorMax = Vector2.one;

        rect.offsetMin = Vector2.zero;

        rect.offsetMax = Vector2.zero;
    }
}