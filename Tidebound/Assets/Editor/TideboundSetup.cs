using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.IO;

namespace Tidebound.Editor
{
    [InitializeOnLoad]
    public static class TideboundSetup
    {
        const string ScenePath="Assets/Scenes/Tidebound.unity";
        static TideboundSetup() { EditorApplication.delayCall+=EnsureScene; }
        static void EnsureScene()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode||File.Exists(ScenePath))return;
            CreateScene();
        }
        [MenuItem("Tidebound/Create or open game scene")]
        public static void CreateScene()
        {
            if(File.Exists(ScenePath)){EditorSceneManager.OpenScene(ScenePath);return;}
            if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;
            Directory.CreateDirectory("Assets/Scenes");
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            new GameObject("Tidebound Game").AddComponent<TideboundGame>();
            EditorSceneManager.SaveScene(scene,ScenePath);
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true)};
            AssetDatabase.Refresh();
        }
        [MenuItem("Tidebound/Reset prototype save")]
        static void ResetSave(){PlayerPrefs.DeleteKey("Tidebound.prototype.v1");PlayerPrefs.Save();}
    }
}
