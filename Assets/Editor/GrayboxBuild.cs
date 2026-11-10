using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class GrayboxBuild
{
    public static void Prepare()
    {
        System.IO.Directory.CreateDirectory("Assets/Resources");
        var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Resources/GrayboxRenderer.asset");
        if (renderer == null)
        {
            renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
            AssetDatabase.CreateAsset(renderer, "Assets/Resources/GrayboxRenderer.asset");
        }

        var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/Resources/GrayboxPipeline.asset");
        if (pipeline == null)
        {
            pipeline = UniversalRenderPipelineAsset.Create(renderer);
            AssetDatabase.CreateAsset(pipeline, "Assets/Resources/GrayboxPipeline.asset");
        }

        GraphicsSettings.defaultRenderPipeline = pipeline;
        QualitySettings.renderPipeline = pipeline;
        if (AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/GrayboxMaterial.mat") == null)
            AssetDatabase.CreateAsset(new Material(Shader.Find("Universal Render Pipeline/Lit")), "Assets/Resources/GrayboxMaterial.mat");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var game = new GameObject("Graybox").AddComponent<Graybox>();
        var chapter = AssetDatabase.LoadAssetAtPath<Jiaozi.FirstChapterData>("Assets/Resources/FirstChapter.asset");
        if (chapter == null)
        {
            chapter = ScriptableObject.CreateInstance<Jiaozi.FirstChapterData>();
            chapter.stations = game.layout;
            chapter.partitions = game.partitionPositions;
            AssetDatabase.CreateAsset(chapter, "Assets/Resources/FirstChapter.asset");
        }

        game.chapter = chapter;
        System.IO.Directory.CreateDirectory("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Graybox.unity");
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/Graybox.unity", true)
        };
        PlayerSettings.productName = "Jiaozi Graybox";
        PlayerSettings.companyName = "JiaoziWorkshop";
        PlayerSettings.resizableWindow = true;
        PlayerSettings.runInBackground = true;
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 800;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        AssetDatabase.SaveAssets();
    }

    public static void Build()
    {
        Prepare();
        var report = BuildPipeline.BuildPlayer(new[] { "Assets/Scenes/Graybox.unity" }, "Builds/Windows/JiaoziGraybox.exe", BuildTarget.StandaloneWindows64, BuildOptions.Development);
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            throw new System.Exception("Build failed");
    }
}
