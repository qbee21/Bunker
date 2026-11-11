using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

public class BunkerBuild
{
    [InitializeOnLoadMethod]
    static void FirstOpen()
    {
        if (SessionState.GetBool("BunkerConfigured", false)) return;
        SessionState.SetBool("BunkerConfigured", true);
        EditorApplication.delayCall += Configure;
    }

    [MenuItem("Bunker/Configure project")]
    public static void Configure()
    {
        PlayerSettings.companyName = "StarodymovGames";
        PlayerSettings.productName = "Bunker Evolution";
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 720;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.runInBackground = false;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToPortrait = true;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = true;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, "com.starodymov.bunker");
        PlayerSettings.iOS.targetOSVersionString = "15.0";
        PlayerSettings.iOS.applicationDisplayName = "Bunker Evolution";
        PlayerSettings.iOS.requiresFullScreen = true;
        PlayerSettings.statusBarHidden = true;
        PlayerSettings.iOS.appleDeveloperTeamID = "CFG7FY3R8S";
        PlayerSettings.iOS.appleEnableAutomaticSigning = false;
        Texture2D iOSIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Branding/BunkerEvolutionAppIcon.jpg");
        if (iOSIcon != null) PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.iOS, new[] { iOSIcon });
        else Debug.LogWarning("Bunker iOS icon source is missing: Assets/Branding/BunkerEvolutionAppIcon.jpg");
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
        PlayerSettings.WebGL.decompressionFallback = true;
        PlayerSettings.WebGL.dataCaching = true;
        PlayerSettings.stripEngineCode = true;
        PlayerSettings.WebGL.template = "PROJECT:YandexGames";
        Object settings = Unsupported.GetSerializedAssetInterfaceSingleton("PlayerSettings");
        if (settings != null)
        {
            SerializedObject player = new SerializedObject(settings);
            SerializedProperty input = player.FindProperty("activeInputHandler");
            if (input != null) input.intValue = 1;
            player.ApplyModifiedPropertiesWithoutUndo();
        }
        RenderPipelineAsset pipeline = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/Settings/UniversalRP.asset");
        if (pipeline != null)
        {
            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;
        }
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene("Assets/Scenes/SampleScene.unity", true) };
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Bunker/Build Windows")]
    public static void BuildWindows()
    {
        Build(BuildTarget.StandaloneWindows64, "Builds/Windows/BunkerDefense.exe");
    }

    [MenuItem("Bunker/Build WebGL")]
    public static void BuildWebGL()
    {
        Build(BuildTarget.WebGL, "Builds/WebGL");
    }

    /// <summary>GitHub Actions entry point. Exports an unsigned Xcode project; signing happens only on macOS CI.</summary>
    public static void BuildIOSForCI()
    {
        const string path = "Builds/iOS";
        Configure();
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.iOS, BuildTarget.iOS))
            throw new Exception("iOS Build Support is not installed in this Unity editor.");
        if (Directory.Exists(path)) Directory.Delete(path, true);
        Directory.CreateDirectory(path);
        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/SampleScene.unity" },
            locationPathName = path,
            target = BuildTarget.iOS,
            options = BuildOptions.None
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new Exception("iOS Xcode export failed: " + report.summary.result);
    }

    static void Build(BuildTarget target, string path)
    {
        Configure();
        if (!BuildPipeline.IsBuildTargetSupported(target == BuildTarget.WebGL ? BuildTargetGroup.WebGL : BuildTargetGroup.Standalone, target))
        {
            EditorUtility.DisplayDialog("Bunker", "Install the build support module in Unity Hub: " + target, "OK");
            return;
        }
        Directory.CreateDirectory(target == BuildTarget.WebGL ? path : Path.GetDirectoryName(path));
        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/SampleScene.unity" },
            locationPathName = path,
            target = target,
            options = BuildOptions.None
        });
        if (report.summary.result != BuildResult.Succeeded) throw new System.Exception("Build failed: " + report.summary.result);
        if (target == BuildTarget.WebGL && report.summary.totalSize > 50UL * 1024 * 1024)
            Debug.LogWarning("WebGL build exceeds 50 MB.");
    }
}
