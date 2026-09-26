#if UNITY_EDITOR && UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace OrbitOrchard.Editor
{
    /// <summary>Development exports only: run the app from Xcode against a local StoreKit configuration so
    /// gem packs can be bought without App Store Connect. Release exports never reference it.</summary>
    public sealed class StoreKitTestingPostprocessor : IPostprocessBuildWithReport
    {
        private const string FileName = "Clinic.storekit";
        public int callbackOrder => 101;

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.iOS || (report.summary.options & BuildOptions.Development) == 0) return;
            var exportPath = report.summary.outputPath;
            File.Copy(Path.Combine(Application.dataPath, "OrbitOrchard/Plugins/iOS/Configuration~", FileName), Path.Combine(exportPath, FileName), true);
            var scheme = Path.Combine(exportPath, "Unity-iPhone.xcodeproj/xcshareddata/xcschemes/Unity-iPhone.xcscheme");
            if (!File.Exists(scheme)) throw new BuildFailedException("Missing Unity-iPhone scheme for StoreKit testing.");
            var text = File.ReadAllText(scheme);
            if (text.Contains("StoreKitConfigurationFileReference")) return;
            // Relative to the project's embedded workspace, as Xcode writes it.
            const string reference = "      <StoreKitConfigurationFileReference\n         identifier = \"../../" + FileName + "\">\n      </StoreKitConfigurationFileReference>\n";
            var end = text.IndexOf("   </LaunchAction>", System.StringComparison.Ordinal);
            if (end < 0) throw new BuildFailedException("Unity-iPhone scheme has no launch action.");
            File.WriteAllText(scheme, text.Insert(end, reference));
        }
    }
}
#endif
