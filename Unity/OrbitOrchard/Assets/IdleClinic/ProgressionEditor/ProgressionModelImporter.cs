using System;
using System.IO;
using UnityEditor;

namespace IdleClinic.ProgressionEditor
{
    /// <summary>Characters play legacy clips by name; furniture is static. Applies only to the progression models.</summary>
    public sealed class ProgressionModelImporter : AssetPostprocessor
    {
        private const string Prefix = "Assets/IdleClinic/Resources/Progression/Models/";

        private void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(Prefix, StringComparison.Ordinal)) return;
            var importer = (ModelImporter)assetImporter;
            importer.importCameras = false;
            importer.importLights = false;
            importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
            var stem = Path.GetFileNameWithoutExtension(assetPath);
            var character = stem.StartsWith("Boss_") || stem.StartsWith("Patient_") || stem.StartsWith("Nurse_") || stem.StartsWith("Receptionist_");
            importer.importAnimation = character;
            importer.animationType = character ? ModelImporterAnimationType.Legacy : ModelImporterAnimationType.None;
            if (character) importer.animationCompression = ModelImporterAnimationCompression.Off;
        }
    }
}
