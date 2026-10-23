using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using Object = UnityEngine.Object;

/// <summary>Temporary, recoverable Web assets. Never rewrites a source GLB or PNG.</summary>
public static class ChessWebAssetOptimizer
{
    private const string StageRoot = "Assets/__ChessWebBuildStage";
    private const string CustomPieces = "Assets/Resources/CustomPieces/";
    private const int TextureLimit = 1024;
    private static bool active;

    private static string RecoveryDirectory => Path.GetFullPath(Path.Combine(Application.dataPath, "../../.local/unity-web-asset-stage"));
    private static string JournalPath => Path.Combine(RecoveryDirectory, "journal.json");

    /// <summary>Use immediately around BuildPipeline.BuildPlayer, in a using block.</summary>
    public static IDisposable Stage()
    {
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
            throw new BuildFailedException("ChessWebAssetOptimizer.Stage requires the active WebGL target.");
        if (active) throw new InvalidOperationException("A Web asset stage is already active.");
        RestoreInterruptedStage();
        if (AssetDatabase.IsValidFolder(StageRoot) || Directory.Exists(StageRoot))
            throw new BuildFailedException($"Unowned staging directory exists: {StageRoot}. Inspect it before continuing.");

        active = true;
        var scope = new AssetStage();
        try
        {
            scope.Prepare();
            return scope;
        }
        catch (Exception preparationError)
        {
            try { scope.Dispose(); }
            catch (Exception recoveryError) { throw new AggregateException(preparationError, recoveryError); }
            throw;
        }
    }

    /// <summary>Also available via -executeMethod after a killed Editor/build.</summary>
    [MenuItem("Chess CGI/Build/Restore interrupted Web asset stage")]
    public static void RestoreInterruptedStage()
    {
        if (active) throw new InvalidOperationException("Cannot recover while a Web asset stage is active.");
        if (File.Exists(JournalPath)) Restore(JsonUtility.FromJson<Journal>(File.ReadAllText(JournalPath)));
    }

    /// <summary>Editor smoke check for staging, dependency guards and byte/GUID restoration, without a player build.</summary>
    [MenuItem("Chess CGI/Build/Verify Web asset stage round trip")]
    public static void VerifyStageRoundTrip()
    {
        using (Stage()) { }
        if (File.Exists(JournalPath) || AssetDatabase.IsValidFolder(StageRoot))
            throw new BuildFailedException("Web asset stage left recovery files after a successful round trip.");
        Debug.Log("CHESS_WEB_ASSETS_ROUND_TRIP_PASSED");
    }

    private sealed class AssetStage : IDisposable
    {
        private readonly Journal journal = new Journal();
        private bool disposed;

        public void Prepare()
        {
            Directory.CreateDirectory(RecoveryDirectory);
            SaveJournal(journal);
            journal.stageGuid = AssetDatabase.CreateFolder("Assets", "__ChessWebBuildStage");
            if (string.IsNullOrEmpty(journal.stageGuid)) throw new BuildFailedException("Could not create " + StageRoot);
            SaveJournal(journal);
            CreateFolder(StageRoot + "/Parked");
            CreateFolder(StageRoot + "/Generated");

            string[] legacyFolders =
            {
                "Bishop_Rafael_Assets", "King_Ricardo_Carioca_Assets", "Knight_Gustavo_Assets",
                "Pawn_Mathwidu_Redhead_Assets", "Pawn_Mathwidu_Redhead_v2_Assets",
                "Queen_Marta_Assets", "Rook_Alex_Assets"
            };
            // GUIDs survive the move; an unexpected real dependency is caught below.
            foreach (string folder in legacyFolders)
                Move(CustomPieces + folder, StageRoot + "/Parked/" + folder, journal);
            Move(CustomPieces + "Pawn_Mathwidu_Redhead.prefab", StageRoot + "/Parked/Pawn_Mathwidu_Redhead.prefab", journal);

            var remap = new Dictionary<Object, Object>();
            CreateModelVariants("Assets/Art/Environment/ChessBoard/ChessBoard.glb", "Board", remap);
            CreateModelVariants("Assets/Art/Environment/FeevaleLab/FeevaleComputerLab.glb", "Room", remap);
            StagePrefab("Assets/Resources/Environment/ChessBoardFrame.prefab", remap);
            StagePrefab("Assets/Resources/Environment/FeevaleComputerLab.prefab", remap);

            // Interactive squares use standalone materials, not the decorative prefab.
            StageBoardMaterial("Assets/Materials/Board_Light.mat", remap);
            StageBoardMaterial("Assets/Materials/Board_Dark.mat", remap);
            VerifyDependencies(journal);
            Debug.Log($"CHESS_WEB_ASSETS_STAGED textures={remap.Keys.OfType<Texture2D>().Count()} maxTextureSize={TextureLimit} recovery={JournalPath}");
        }

        private void StagePrefab(string path, Dictionary<Object, Object> remap)
        {
            string generated = StageRoot + "/Generated/" + Path.GetFileName(path);
            GameObject root = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
            if (root == null) throw new BuildFailedException("Missing environment prefab: " + path);
            try
            {
                PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                foreach (Component component in root.GetComponentsInChildren<Component>(true))
                {
                    if (component == null) throw new BuildFailedException("Missing component in " + path);
                    var serialized = new SerializedObject(component);
                    SerializedProperty property = serialized.GetIterator();
                    while (property.Next(true))
                    {
                        if (property.propertyType == SerializedPropertyType.ObjectReference
                            && property.objectReferenceValue != null
                            && remap.TryGetValue(property.objectReferenceValue, out Object replacement))
                            property.objectReferenceValue = replacement;
                    }
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
                if (PrefabUtility.SaveAsPrefabAsset(root, generated) == null)
                    throw new BuildFailedException("Could not save Web environment prefab: " + path);
            }
            finally { Object.DestroyImmediate(root); }

            Move(path, StageRoot + "/Parked/" + Path.GetFileName(path), journal);
            Move(generated, path, journal);
        }

        private void StageBoardMaterial(string path, Dictionary<Object, Object> remap)
        {
            if (EditorUtility.IsDirty(AssetDatabase.LoadAssetAtPath<Material>(path)))
                throw new BuildFailedException("Save the existing material changes before staging: " + path);
            string generated = StageRoot + "/Generated/" + Path.GetFileName(path);
            if (!AssetDatabase.CopyAsset(path, generated)) throw new BuildFailedException("Could not copy " + path);
            Material variant = AssetDatabase.LoadAssetAtPath<Material>(generated);
            RemapTextures(variant, remap);
            EditorUtility.SetDirty(variant);
            AssetDatabase.SaveAssetIfDirty(variant);
            ReplaceWithSnapshot(path, File.ReadAllBytes(generated), journal);
            // The optimizer never changes an existing asset's metadata.
            ReplaceWithSnapshot(path + ".meta", File.ReadAllBytes(path + ".meta"), journal);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            try { if (File.Exists(JournalPath)) Restore(journal); }
            finally { active = false; }
        }
    }

    private static void CreateModelVariants(string modelPath, string label, Dictionary<Object, Object> remap)
    {
        string folder = StageRoot + "/Generated/" + label;
        CreateFolder(folder);
        byte[] glb = File.ReadAllBytes(modelPath);
        GlbMetadata metadata = ReadGlb(glb, out int binaryOffset, out int binaryLength);
        Object[] sourceAssets = AssetDatabase.LoadAllAssetsAtPath(modelPath);
        Texture2D[] textures = sourceAssets.OfType<Texture2D>().ToArray();
        if (metadata.images == null || textures.Length != metadata.images.Length)
            throw new BuildFailedException("Image/sampler variants need explicit review before staging " + modelPath);
        var images = metadata.images.ToDictionary(image => image.name, StringComparer.Ordinal);

        for (int index = 0; index < textures.Length; index++)
        {
            Texture2D source = textures[index];
            if (!images.TryGetValue(source.name, out GlbImage image)
                || image.mimeType != "image/png" || image.bufferView < 0 || image.bufferView >= metadata.bufferViews.Length)
                throw new BuildFailedException("Unsupported embedded texture: " + source.name);
            GlbBufferView view = metadata.bufferViews[image.bufferView];
            if (view.buffer != 0 || view.byteOffset < 0 || view.byteLength <= 0
                || (long)view.byteOffset + view.byteLength > binaryLength)
                throw new BuildFailedException("Invalid embedded texture range in " + modelPath);
            string path = folder + "/Texture_" + index + ".png";
            var bytes = new byte[view.byteLength];
            Buffer.BlockCopy(glb, binaryOffset + view.byteOffset, bytes, 0, bytes.Length);
            File.WriteAllBytes(path, bytes);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            // glTFast's shader samples RGB normal maps directly. Unity NormalMap packing would change their meaning.
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = GraphicsFormatUtility.IsSRGBFormat(source.graphicsFormat);
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = false;
            importer.mipmapEnabled = source.mipmapCount > 1;
            importer.mipmapFilter = TextureImporterMipFilter.BoxFilter;
            importer.filterMode = source.filterMode;
            importer.wrapModeU = source.wrapModeU;
            importer.wrapModeV = source.wrapModeV;
            importer.wrapModeW = source.wrapModeW;
            importer.anisoLevel = source.anisoLevel;
            importer.isReadable = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            var settings = importer.GetPlatformTextureSettings("WebGL");
            settings.name = "WebGL";
            settings.overridden = true;
            settings.maxTextureSize = TextureLimit;
            settings.format = TextureImporterFormat.ETC2_RGBA8;
            settings.textureCompression = TextureImporterCompression.CompressedHQ;
            settings.compressionQuality = 100;
            settings.crunchedCompression = false;
            importer.SetPlatformTextureSettings(settings);
            importer.SaveAndReimport();
            Texture2D variant = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (variant == null || variant.width > TextureLimit || variant.height > TextureLimit
                || GraphicsFormatUtility.IsSRGBFormat(variant.graphicsFormat) != importer.sRGBTexture)
                throw new BuildFailedException("Web texture import did not preserve size/color contract: " + source.name);
            remap.Add(source, variant);
        }

        int meshIndex = 0;
        foreach (Mesh source in sourceAssets.OfType<Mesh>())
        {
            Mesh variant = Object.Instantiate(source);
            variant.name = source.name;
            AssetDatabase.CreateAsset(variant, folder + "/Mesh_" + meshIndex++ + ".asset");
            remap.Add(source, variant);
        }
        int materialIndex = 0;
        foreach (Material source in sourceAssets.OfType<Material>())
        {
            var variant = new Material(source) { name = source.name };
            RemapTextures(variant, remap);
            AssetDatabase.CreateAsset(variant, folder + "/Material_" + materialIndex++ + ".mat");
            remap.Add(source, variant);
        }
    }

    private static void RemapTextures(Material material, Dictionary<Object, Object> remap)
    {
        foreach (string property in material.GetTexturePropertyNames())
        {
            Texture texture = material.GetTexture(property);
            if (texture != null && remap.TryGetValue(texture, out Object replacement))
                material.SetTexture(property, (Texture)replacement);
        }
    }

    private static void VerifyDependencies(Journal journal)
    {
        string[] roots = AssetDatabase.GetAllAssetPaths()
            .Where(path => path.StartsWith("Assets/Resources/", StringComparison.Ordinal) && !AssetDatabase.IsValidFolder(path))
            .Concat(new[] { "Assets/Scenes/Main.unity" }).ToArray();
        string[] forbidden = journal.moves.Where(move => move.destination.StartsWith(StageRoot + "/Parked/", StringComparison.Ordinal))
            .Select(move => move.destination).Concat(new[]
            {
                "Assets/Art/Environment/ChessBoard/ChessBoard.glb",
                "Assets/Art/Environment/FeevaleLab/FeevaleComputerLab.glb"
            }).ToArray();
        foreach (string dependency in AssetDatabase.GetDependencies(roots, true))
            if (forbidden.Any(path => dependency == path || dependency.StartsWith(path + "/", StringComparison.Ordinal)))
                throw new BuildFailedException("Web roots still depend on an original/legacy asset: " + dependency);
    }

    private static GlbMetadata ReadGlb(byte[] bytes, out int binaryOffset, out int binaryLength)
    {
        if (bytes.Length < 28 || BitConverter.ToUInt32(bytes, 0) != 0x46546c67
            || BitConverter.ToUInt32(bytes, 4) != 2 || BitConverter.ToUInt32(bytes, 8) != bytes.Length
            || BitConverter.ToUInt32(bytes, 16) != 0x4e4f534a)
            throw new BuildFailedException("Expected a complete glTF 2.0 GLB with a JSON first chunk.");
        uint jsonLength = BitConverter.ToUInt32(bytes, 12);
        long binHeader = 20L + jsonLength;
        if (binHeader + 8 > bytes.Length || jsonLength > int.MaxValue
            || BitConverter.ToUInt32(bytes, (int)binHeader + 4) != 0x004e4942)
            throw new BuildFailedException("Expected an embedded BIN chunk after the GLB JSON.");
        uint binLength = BitConverter.ToUInt32(bytes, (int)binHeader);
        if (binLength > int.MaxValue || binHeader + 8 + binLength != bytes.Length)
            throw new BuildFailedException("Invalid GLB binary chunk length.");
        binaryOffset = (int)binHeader + 8;
        binaryLength = (int)binLength;
        GlbMetadata metadata = JsonUtility.FromJson<GlbMetadata>(Encoding.UTF8.GetString(bytes, 20, (int)jsonLength));
        if (metadata?.bufferViews == null) throw new BuildFailedException("GLB has no buffer views.");
        return metadata;
    }

    private static void CreateFolder(string path)
    {
        if (!AssetDatabase.IsValidFolder(path)
            && string.IsNullOrEmpty(AssetDatabase.CreateFolder(Path.GetDirectoryName(path).Replace('\\', '/'), Path.GetFileName(path))))
            throw new BuildFailedException("Could not create " + path);
    }

    private static void Move(string source, string destination, Journal journal)
    {
        string guid = AssetDatabase.AssetPathToGUID(source);
        if (string.IsNullOrEmpty(guid)) throw new BuildFailedException("Expected asset is missing: " + source);
        journal.moves.Add(new AssetMove { source = source, destination = destination, guid = guid });
        SaveJournal(journal); // Record intent before the move, including a move that throws.
        string error = AssetDatabase.MoveAsset(source, destination);
        if (!string.IsNullOrEmpty(error)) throw new BuildFailedException(error);
    }

    private static void ReplaceWithSnapshot(string path, byte[] stagedBytes, Journal journal)
    {
        byte[] original = File.ReadAllBytes(path);
        string backup = "snapshot-" + journal.snapshots.Count;
        File.WriteAllBytes(Path.Combine(RecoveryDirectory, backup), original);
        journal.snapshots.Add(new FileSnapshot
        {
            path = path, backup = backup, originalHash = Hash(original), stagedHash = Hash(stagedBytes)
        });
        SaveJournal(journal);
        if (!original.SequenceEqual(stagedBytes)) File.WriteAllBytes(path, stagedBytes);
    }

    private static void SaveJournal(Journal journal)
    {
        string temporary = JournalPath + ".tmp";
        File.WriteAllText(temporary, JsonUtility.ToJson(journal, true));
        if (File.Exists(JournalPath)) File.Replace(temporary, JournalPath, null);
        else File.Move(temporary, JournalPath);
    }

    private static void Restore(Journal journal)
    {
        if (journal == null || journal.version != 1 || journal.moves == null || journal.snapshots == null)
            throw new BuildFailedException("Invalid Web recovery journal: " + JournalPath);
        var errors = new List<Exception>();
        foreach (FileSnapshot snapshot in journal.snapshots.AsEnumerable().Reverse())
        {
            try
            {
                string currentHash = File.Exists(snapshot.path) ? Hash(File.ReadAllBytes(snapshot.path)) : "(missing)";
                if (currentHash == snapshot.originalHash) continue;
                byte[] original = File.ReadAllBytes(Path.Combine(RecoveryDirectory, snapshot.backup));
                if (Hash(original) != snapshot.originalHash) throw new IOException("Backup checksum mismatch: " + snapshot.path);
                if (currentHash != snapshot.stagedHash)
                    throw new IOException("Concurrent edit detected; recover manually from " + JournalPath + ": " + snapshot.path);
                File.WriteAllBytes(snapshot.path, original);
                AssetDatabase.ImportAsset(snapshot.path.EndsWith(".meta", StringComparison.Ordinal)
                    ? snapshot.path.Substring(0, snapshot.path.Length - 5) : snapshot.path,
                    ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            }
            catch (Exception error) { errors.Add(error); }
        }
        foreach (AssetMove move in journal.moves.AsEnumerable().Reverse())
        {
            try
            {
                string current = AssetDatabase.GUIDToAssetPath(move.guid);
                if (current == move.source) continue; // Move never happened or recovery already restored it.
                if (journal.cleanupStarted && string.IsNullOrEmpty(current)
                    && move.source.StartsWith(StageRoot + "/Generated/", StringComparison.Ordinal))
                    continue; // Generated assets may already have been deleted by an interrupted cleanup.
                if (current != move.destination)
                    throw new IOException("Asset moved concurrently; inspect " + move.guid + " at " + current);
                if (!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(move.source)))
                    throw new IOException("Restore destination occupied: " + move.source);
                string error = AssetDatabase.MoveAsset(move.destination, move.source);
                if (!string.IsNullOrEmpty(error)) throw new IOException(error);
            }
            catch (Exception error) { errors.Add(error); }
        }
        if (errors.Count > 0) throw new AggregateException("Web recovery incomplete. Keep " + RecoveryDirectory, errors);
        if (!string.IsNullOrEmpty(journal.stageGuid))
        {
            string currentGuid = AssetDatabase.AssetPathToGUID(StageRoot);
            bool alreadyRemoved = journal.cleanupStarted && string.IsNullOrEmpty(currentGuid)
                && !Directory.Exists(StageRoot) && !File.Exists(StageRoot + ".meta");
            if (!alreadyRemoved)
            {
                if (currentGuid != journal.stageGuid)
                    throw new IOException("Staging directory identity changed. Keep " + RecoveryDirectory);
                journal.cleanupStarted = true;
                SaveJournal(journal);
                if (!AssetDatabase.DeleteAsset(StageRoot)) throw new IOException("Could not remove " + StageRoot);
            }
        }
        Directory.Delete(RecoveryDirectory, true);
        Debug.Log("CHESS_WEB_ASSETS_RESTORED sourceAssets=unchanged");
    }

    private static string Hash(byte[] bytes)
    {
        using (SHA256 sha = SHA256.Create())
            return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
    }

    [Serializable] private sealed class Journal
    {
        public int version = 1;
        public string stageGuid;
        public bool cleanupStarted;
        public List<AssetMove> moves = new List<AssetMove>();
        public List<FileSnapshot> snapshots = new List<FileSnapshot>();
    }
    [Serializable] private sealed class AssetMove { public string source; public string destination; public string guid; }
    [Serializable] private sealed class FileSnapshot { public string path; public string backup; public string originalHash; public string stagedHash; }
    // Populated by JsonUtility from the original GLB JSON, rather than assigned by C# code.
#pragma warning disable CS0649
    [Serializable] private sealed class GlbMetadata { public GlbImage[] images; public GlbBufferView[] bufferViews; }
    [Serializable] private sealed class GlbImage { public string name; public string mimeType; public int bufferView = -1; }
    [Serializable] private sealed class GlbBufferView { public int buffer; public int byteOffset; public int byteLength; }
#pragma warning restore CS0649
}
