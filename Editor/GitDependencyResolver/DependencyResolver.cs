using System.IO;
using System.Linq;
using ET;
using UnityEditor;
using UnityEditor.PackageManager;
using Debug = UnityEngine.Debug;

namespace Hibzz.DependencyResolver
{
    [InitializeOnLoad]
    public class DependencyResolver
    {
        private const string package_prefix = "qgame";
        
        //[MenuItem("ET/MoveToPackage")]
        private static void MoveToPackage(string package, string version)
        {
            string packageName = default;
            string moveFileName = default;
#if UNITY_6000_0_OR_NEWER
            packageName = package;
            moveFileName = "MoveToPackages_6";
#else
            packageName = $"{package}@{version}";
            moveFileName = "MoveToPackages";
#endif
            var dir = Path.Combine("Library/PackageCache", packageName);
            if (!Directory.Exists(dir))
            {
                return;
            }

            Debug.Log($"move package: {packageName}");
            var process = ProcessHelper.PowerShell(
                $"-NoExit -ExecutionPolicy Bypass -File ./Packages/com.etetet.init/{moveFileName}.ps1 {package} {version}",
                waitExit: true);
            Debug.Log(process.StandardOutput.ReadToEnd());
        }

        static DependencyResolver()
        {
            Events.registeredPackages += OnPackagesRegistered;
        }

        // Invoked when the package manager completes registering new packages
        private static void OnPackagesRegistered(PackageRegistrationEventArgs packageRegistrationInfo)
        {
            if (packageRegistrationInfo.added.Count == 0 && packageRegistrationInfo.changedFrom.Count == 0)
            {
                return;
            }

            Debug.Log($"Packages Registered: {string.Join(" ", packageRegistrationInfo.added.Select(x => x.name))}");

            // loop through all of the added packages and get their git
            // dependencies and add it to the list that contains all the
            // dependencies that need to be installed
            foreach (var package in packageRegistrationInfo.added)
            {
                if (!package.name.StartsWith($"{package_prefix}."))
                {
                    continue;
                }

                MoveToPackage(package.name, package.version);
            }

            foreach (var package in packageRegistrationInfo.changedFrom)
            {
                if (!package.name.StartsWith($"{package_prefix}."))
                {
                    continue;
                }

                MoveToPackage(package.name, package.version);
            }

            AssetDatabase.Refresh();
        }

        [MenuItem("Tools/Init/RepairDependencies")]
        private static void RepairDependencies()
        {
            foreach (var directory in Directory.GetDirectories("Library/PackageCache", $"{package_prefix}.*"))
            {
                var baseName = Path.GetFileName(directory);
                if (!baseName.StartsWith($"{package_prefix}."))
                {
                    continue;
                }

                var ss = baseName.Split("@");
                var packageName = ss[0];
#if UNITY_6000_0_OR_NEWER
                string version = "";
#else
                var version = ss[1];
#endif

                MoveToPackage(packageName, version);
            }

            AssetDatabase.Refresh();

            Debug.Log($"repaire package finish");
        }
    }
}