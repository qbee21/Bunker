using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;

#if UNITY_IOS
using UnityEditor.iOS.Xcode;
#endif

public static class BunkerIOSPostBuild
{
#if UNITY_IOS
    [PostProcessBuild(1000)]
    public static void AddPrivacyManifest(BuildTarget target, string pathToBuiltProject)
    {
        if (target != BuildTarget.iOS) return;

        const string source = "Assets/Plugins/iOS/PrivacyInfo.xcprivacy";
        string destination = Path.Combine(pathToBuiltProject, "PrivacyInfo.xcprivacy");
        File.Copy(source, destination, true);

        string pbxPath = PBXProject.GetPBXProjectPath(pathToBuiltProject);
        PBXProject project = new PBXProject();
        project.ReadFromFile(pbxPath);
        string targetGuid = project.GetUnityMainTargetGuid();
        string fileGuid = project.AddFile("PrivacyInfo.xcprivacy", "PrivacyInfo.xcprivacy", PBXSourceTree.Source);
        project.AddFileToBuild(targetGuid, fileGuid);
        project.WriteToFile(pbxPath);
    }
#endif
}
