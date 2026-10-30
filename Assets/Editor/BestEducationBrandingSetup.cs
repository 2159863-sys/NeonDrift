using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Adds a small Best Education logo centred under the "HIGHEST SCORE" text on the
// MainMenu Canvas, with no background panel. The rocket and the "Best Education" name sit
// side by side so they fit above the bottom edge of the menu.
// Both images are light-text copies of the original logo with mipmaps, so they stay sharp
// when scaled down. Runs automatically once after compiling when the branding is missing
// (MainMenu is opened additively, so the scene you are working in is left alone), and can
// be run any time from the menu: Window > Neon Drift > Add Best Education Branding.
[InitializeOnLoad]
public static class BestEducationBrandingSetup
{
    const string ScenePath = "Assets/Scenes/MainMenu.unity";
    const string RocketPath = "Assets/Branding/BestEducation_Rocket.png";
    const string NamePath = "Assets/Branding/BestEducation_Name_Light.png";
    const string RootName = "BestEducationBranding";

    const float RocketHeight = 40f;
    const float NameWidth = 150f;
    const float NameGap = 10f;        // space between the rocket and the name
    const float Spacing = 6f;         // gap between "HIGHEST SCORE" and the logo

    static BestEducationBrandingSetup()
    {
        EditorApplication.delayCall += AutoSetup;
    }

    static void AutoSetup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        SetUp(false);
    }

    [MenuItem("Window/Neon Drift/Add Best Education Branding")]
    static void SetUpFromMenu()
    {
        SetUp(true);
    }

    static void SetUp(bool interactive)
    {
        Sprite rocket = ImportAsSprite(RocketPath);
        Sprite name = ImportAsSprite(NamePath);
        if (rocket == null || name == null)
        {
            Debug.LogError("NEON DRIFT: Could not load the Best Education logo images in Assets/Branding.");
            return;
        }

        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        bool openedHere = false;
        if (!scene.isLoaded)
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            openedHere = true;
        }
        else if (scene.isDirty && !interactive)
        {
            // Never save someone's unsaved MainMenu edits behind their back.
            return;
        }

        Canvas canvas = FindCanvas(scene);
        if (canvas == null)
        {
            Debug.LogError("NEON DRIFT: No Canvas found in MainMenu.");
            if (openedHere) EditorSceneManager.CloseScene(scene, true);
            return;
        }

        Transform existing = canvas.transform.Find(RootName);
        if (existing != null)
        {
            if (!interactive)
            {
                if (openedHere) EditorSceneManager.CloseScene(scene, true);
                return;
            }
            Undo.DestroyObjectImmediate(existing.gameObject);
        }

        BuildBranding(canvas.transform, rocket, name);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        if (openedHere) EditorSceneManager.CloseScene(scene, true);

        Debug.Log("NEON DRIFT: Best Education branding added to MainMenu and scene saved.");
    }

    static Sprite ImportAsSprite(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return null;

        // Mipmaps + trilinear keep the big source image crisp at menu size.
        if (importer.textureType != TextureImporterType.Sprite || !importer.mipmapEnabled)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = true;
            importer.filterMode = FilterMode.Trilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static Canvas FindCanvas(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Canvas canvas = root.GetComponentInChildren<Canvas>(true);
            if (canvas != null && canvas.isRootCanvas) return canvas;
        }
        return null;
    }

    static void BuildBranding(Transform canvas, Sprite rocket, Sprite name)
    {
        float rocketWidth = RocketHeight * rocket.rect.width / rocket.rect.height;
        float nameHeight = NameWidth * name.rect.height / name.rect.width;
        float width = rocketWidth + NameGap + NameWidth;
        float height = RocketHeight;

        // Transparent group (no panel), centred under the high score, rocket left and name right,
        // so it fits in the space above the bottom edge of the menu.
        RectTransform score = canvas.Find("HighScoreText") as RectTransform;

        GameObject root = new GameObject(RootName, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(root, "Add Best Education Branding");
        root.layer = canvas.gameObject.layer;
        RectTransform rootRect = root.GetComponent<RectTransform>();
        rootRect.SetParent(canvas, false);
        rootRect.sizeDelta = new Vector2(width, height);
        rootRect.pivot = new Vector2(0.5f, 1f);
        if (score != null)
        {
            rootRect.anchorMin = score.anchorMin;
            rootRect.anchorMax = score.anchorMax;
            rootRect.anchoredPosition = new Vector2(
                score.anchoredPosition.x,
                score.anchoredPosition.y - TextHeight(score) * 0.5f - Spacing);
        }
        else
        {
            rootRect.anchorMin = rootRect.anchorMax = new Vector2(0.5f, 0f);
            rootRect.anchoredPosition = new Vector2(0f, 70f);
        }

        AddImage(rootRect, "BestEducationLogo", rocket,
            new Vector2(0f, 0.5f), new Vector2(rocketWidth, RocketHeight));
        AddImage(rootRect, "BestEducationName", name,
            new Vector2(1f, 0.5f), new Vector2(NameWidth, nameHeight));
    }

    static void AddImage(RectTransform parent, string objectName, Sprite sprite, Vector2 anchor, Vector2 size)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        go.layer = parent.gameObject.layer;
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;

        Image image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
    }

    static float TextHeight(RectTransform rect)
    {
        TMP_Text text = rect.GetComponent<TMP_Text>();
        if (text == null) return rect.rect.height;
        text.ForceMeshUpdate();
        return text.GetPreferredValues(text.text).y;
    }
}
