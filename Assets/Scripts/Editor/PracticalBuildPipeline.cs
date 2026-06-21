using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class PracticalBuildPipeline
{
    private const string ProductName = "Half Deflection Practical";
    private const string CompanyName = "Final Practical";
    private const string PackageName = "com.finalpractical.halfdeflection";

    private static readonly string[] Scenes =
    {
        "Assets/Scenes/MainMenu.unity",
        "Assets/Scenes/CourseWork.unity",
        "Assets/Scenes/HalfDeflectionScene.unity"
    };

    [MenuItem("Build/Final Practical/Build WebGL")]
    public static void BuildWebGL()
    {
        ConfigurePlayer();
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
        PlayerSettings.WebGL.decompressionFallback = true;

        Build(
            BuildTarget.WebGL,
            Path.Combine("Builds", "WebGL"),
            BuildOptions.None);
    }

    [MenuItem("Build/Final Practical/Build Android APK")]
    public static void BuildAndroid()
    {
        ConfigurePlayer();
        EditorUserBuildSettings.buildAppBundle = false;
        PlayerSettings.Android.useCustomKeystore = false;
#pragma warning disable 0618
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, PackageName);
#pragma warning restore 0618
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;

        Build(
            BuildTarget.Android,
            Path.Combine("Builds", "Android", "HalfDeflectionPractical.apk"),
            BuildOptions.None);
    }

    [MenuItem("Build/Final Practical/Build Windows App")]
    public static void BuildWindows()
    {
        ConfigurePlayer();

        Build(
            BuildTarget.StandaloneWindows64,
            Path.Combine("Builds", "Windows", "HalfDeflectionPractical.exe"),
            BuildOptions.None);
    }

    [MenuItem("Build/Final Practical/Build Demo Package")]
    public static void BuildDemoPackage()
    {
        BuildWebGL();
        BuildAndroid();
        BuildWindows();
    }

    private static void ConfigurePlayer()
    {
        PlayerSettings.companyName = CompanyName;
        PlayerSettings.productName = ProductName;
        EditorBuildSettings.scenes = Array.ConvertAll(Scenes, scene => new EditorBuildSettingsScene(scene, true));
    }

    private static void Build(BuildTarget target, string outputPath, BuildOptions options)
    {
        string directory = target == BuildTarget.WebGL ? outputPath : Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = Scenes,
            locationPathName = outputPath,
            target = target,
            options = options
        });

        if (report.summary.result != BuildResult.Succeeded)
            throw new Exception($"{target} build failed: {report.summary.result}");

        Debug.Log($"{target} build completed: {outputPath}");
    }
}
