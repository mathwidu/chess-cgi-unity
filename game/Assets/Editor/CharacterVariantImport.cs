using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Deterministic import of the Blender-authored direction 02 characters.</summary>
public static class CharacterVariantImport
{
    private const string ArtRoot = "Assets/Art/Characters/Direction02";
    private static readonly string[] Kinds = { "Pawn", "Rook", "Knight", "Bishop", "Queen", "King" };
    private static readonly string[] Prefabs =
    {
        "Pawn_Mathwidu_Redhead_v2", "Rook_Alex", "Knight_Gustavo", "Bishop_Rafael", "Queen_Marta", "King_Ricardo_Carioca"
    };

    [MenuItem("Chess CGI/Import Direction 02 Characters")]
    public static void Build()
    {
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var shared = new Dictionary<string, Material>();
        Texture2D whiteBrand = ConfigureTexture(ArtRoot + "/Brand/White.png", 1024, false, true);
        Texture2D blackBrand = ConfigureTexture(ArtRoot + "/Brand/Black.png", 1024, false, true);
        for (int index = 0; index < Kinds.Length; index++)
        {
            string kind = Kinds[index];
            string directory = ArtRoot + "/" + kind;
            string modelPath = directory + "/Model.glb";
            if (!File.Exists(modelPath)) throw new FileNotFoundException("Generate all six Blender characters first.", modelPath);
            AssetDatabase.ImportAsset(modelPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (model == null) throw new InvalidOperationException("glTF model did not import: " + modelPath);
            Texture2D whiteTexture = ConfigureTexture(directory + "/Body_White.png", 4096);
            Texture2D blackTexture = ConfigureTexture(directory + "/Body_Black.png", 4096);
            Texture2D normalTexture = ConfigureTexture(directory + "/Body_Normal.png", 2048, true);
            GameObject root = UnityEngine.Object.Instantiate(model);
            root.name = Prefabs[index];
            try
            {
                var bindings = new List<CustomPieceAppearance.MaterialBinding>();
                foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    Material[] imported = renderer.sharedMaterials;
                    var white = new Material[imported.Length];
                    var black = new Material[imported.Length];
                    for (int slot = 0; slot < imported.Length; slot++)
                    {
                        Material source = imported[slot];
                        if (source == null) throw new InvalidOperationException("Missing material on " + renderer.name);
                        string key = source.name;
                        if (key.Contains(kind + "_Body_White"))
                        {
                            white[slot] = SaveMaterial(source, directory + "/Body_White.mat");
                            black[slot] = SaveMaterial(source, directory + "/Body_Black.mat");
                            SetBaseTexture(white[slot], whiteTexture);
                            SetBaseTexture(black[slot], blackTexture);
                            foreach (Material material in new[] { white[slot], black[slot] })
                            {
                                material.SetTexture("normalTexture", normalTexture);
                                EditorUtility.SetDirty(material);
                            }
                        }
                        else
                        {
                            string commonPath = ArtRoot + "/Materials/" + key + ".mat";
                            if (!shared.TryGetValue(key, out Material value))
                            {
                                value = SaveMaterial(source, commonPath);
                                shared.Add(key, value);
                            }
                            white[slot] = value;
                            black[slot] = value;
                            if (key.StartsWith("Side_Surface_White", StringComparison.Ordinal))
                            {
                                black[slot] = SharedAlternate(shared, "Side_Surface_Black", source, new Color(.035f, .042f, .05f, 1));
                            }
                            else if (key.StartsWith("Side_Inlay_White", StringComparison.Ordinal))
                            {
                                black[slot] = SharedAlternate(shared, "Side_Inlay_Black", source, new Color(.88f, .85f, .77f, 1));
                            }
                            else if (key.StartsWith("FeevalePatch_White", StringComparison.Ordinal))
                            {
                                SetBaseTexture(white[slot], whiteBrand);
                                if (!shared.TryGetValue("FeevalePatch_Black", out Material darkBrand))
                                {
                                    darkBrand = SaveMaterial(source, ArtRoot + "/Materials/FeevalePatch_Black.mat");
                                    SetBaseTexture(darkBrand, blackBrand);
                                    shared.Add("FeevalePatch_Black", darkBrand);
                                }
                                black[slot] = darkBrand;
                            }
                        }
                    }
                    // Embroidery, inlays and thin trim should not cast a second
                    // near-coplanar shadow onto their supporting surface.
                    if (Array.Exists(imported, m => m.name.StartsWith("FeevalePatch_", StringComparison.Ordinal)
                        || m.name.StartsWith("Side_Inlay_", StringComparison.Ordinal)
                        || m.name.StartsWith("BrushedBrass", StringComparison.Ordinal)))
                        renderer.shadowCastingMode = ShadowCastingMode.Off;
                    bindings.Add(new CustomPieceAppearance.MaterialBinding { Renderer = renderer, White = white, Black = black });
                }
                root.AddComponent<CustomPieceAppearance>().Configure(bindings.ToArray());
                string prefabPath = "Assets/Resources/CustomPieces/" + Prefabs[index] + ".prefab";
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                Debug.Log($"CHARACTER_IMPORTED {kind} renderers={bindings.Count} prefab={prefabPath}");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Debug.Log("DIRECTION02_IMPORT_COMPLETE");
    }

    private static Texture2D ConfigureTexture(string path, int size, bool normal = false, bool branding = false)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new InvalidOperationException("Missing texture importer: " + path);
        importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
        importer.sRGBTexture = !normal;
        importer.alphaIsTransparency = branding;
        importer.mipmapEnabled = true;
        importer.isReadable = false;
        importer.maxTextureSize = size;
        importer.filterMode = FilterMode.Trilinear;
        importer.anisoLevel = 8;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.textureCompression = branding ? TextureImporterCompression.Uncompressed : TextureImporterCompression.CompressedHQ;
        importer.compressionQuality = 100;
        // Desktop close-ups retain the 4K bake; the standalone Android/VR
        // profile bounds body texture memory without reducing the logo atlas.
        var desktop = importer.GetPlatformTextureSettings("Standalone");
        desktop.overridden = true;
        desktop.maxTextureSize = size;
        desktop.format = branding ? TextureImporterFormat.RGBA32 : normal ? TextureImporterFormat.BC5 : TextureImporterFormat.BC7;
        importer.SetPlatformTextureSettings(desktop);
        var android = importer.GetPlatformTextureSettings("Android");
        android.overridden = true;
        android.maxTextureSize = Mathf.Min(size, 2048);
        android.format = branding ? TextureImporterFormat.RGBA32 : TextureImporterFormat.ASTC_6x6;
        importer.SetPlatformTextureSettings(android);
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private static Material SaveMaterial(Material source, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(source) { name = Path.GetFileNameWithoutExtension(path) };
            AssetDatabase.CreateAsset(material, path);
        }
        else
        {
            EditorUtility.CopySerialized(source, material);
            material.name = Path.GetFileNameWithoutExtension(path);
        }
        material.enableInstancing = true;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material SharedAlternate(Dictionary<string, Material> cache, string key, Material source, Color color)
    {
        if (cache.TryGetValue(key, out Material existing)) return existing;
        Material result = SaveMaterial(source, ArtRoot + "/Materials/" + key + ".mat");
        string property = result.HasProperty("baseColorFactor") ? "baseColorFactor" : "_BaseColor";
        if (!result.HasProperty(property)) throw new InvalidOperationException("Unsupported base color shader: " + result.shader.name);
        result.SetColor(property, color);
        cache.Add(key, result);
        return result;
    }

    private static void SetBaseTexture(Material material, Texture texture)
    {
        string property = material.HasProperty("baseColorTexture") ? "baseColorTexture" : "_BaseMap";
        if (!material.HasProperty(property)) throw new InvalidOperationException("Unsupported base texture shader: " + material.shader.name);
        material.SetTexture(property, texture);
        EditorUtility.SetDirty(material);
    }
}
