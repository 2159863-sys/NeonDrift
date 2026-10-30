using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Adds NeonDriftAudio to PlayerShip in MainGame and assigns the clips from Assets/Audio.
// Runs automatically once after compiling when MainGame is open with no unsaved changes,
// and can be run any time from the menu: Neon Drift > Set Up Audio.
[InitializeOnLoad]
public static class NeonDriftAudioSetup
{
    const string ScenePath = "Assets/Scenes/MainGame.unity";
    const string MusicPath = "Assets/Audio/NeonDriftMusic.wav";
    const string CrashPath = "Assets/Audio/AsteroidCrash.wav";
    const string PickupPath = "Assets/Audio/EnergyPickup.wav";

    static NeonDriftAudioSetup()
    {
        EditorApplication.delayCall += AutoSetup;
    }

    static void AutoSetup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath || scene.isDirty) return;

        if (NeedsSetup()) SetUp(false);
    }

    [MenuItem("Window/Neon Drift/Set Up Audio")]
    static void SetUpFromMenu()
    {
        SetUp(true);
    }

    static bool NeedsSetup()
    {
        PlayerHealth player = Object.FindFirstObjectByType<PlayerHealth>(FindObjectsInactive.Include);
        if (player == null) return false;

        NeonDriftAudio audio = player.GetComponent<NeonDriftAudio>();
        return audio == null || audio.backgroundMusic == null
            || audio.asteroidCrash == null || audio.energyPickup == null;
    }

    static void SetUp(bool interactive)
    {
        if (SceneManager.GetActiveScene().path != ScenePath)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(ScenePath);
        }

        PlayerHealth player = Object.FindFirstObjectByType<PlayerHealth>(FindObjectsInactive.Include);
        if (player == null)
        {
            Debug.LogError("NEON DRIFT: No object with PlayerHealth found in MainGame.");
            return;
        }

        NeonDriftAudio audio = player.GetComponent<NeonDriftAudio>();
        if (audio == null) audio = Undo.AddComponent<NeonDriftAudio>(player.gameObject);

        Undo.RecordObject(audio, "Set Up Neon Drift Audio");
        audio.backgroundMusic = AssetDatabase.LoadAssetAtPath<AudioClip>(MusicPath);
        audio.asteroidCrash = AssetDatabase.LoadAssetAtPath<AudioClip>(CrashPath);
        audio.energyPickup = AssetDatabase.LoadAssetAtPath<AudioClip>(PickupPath);
        audio.musicVolume = 0.35f;
        audio.effectsVolume = 0.75f;

        if (audio.backgroundMusic == null || audio.asteroidCrash == null || audio.energyPickup == null)
        {
            Debug.LogError("NEON DRIFT: One or more clips are missing from Assets/Audio.");
            return;
        }

        EditorUtility.SetDirty(audio);
        EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
        EditorSceneManager.SaveScene(player.gameObject.scene);

        Debug.Log("NEON DRIFT: Audio set up on " + player.name + " and MainGame saved.");
        if (interactive) Selection.activeObject = audio;
    }
}
