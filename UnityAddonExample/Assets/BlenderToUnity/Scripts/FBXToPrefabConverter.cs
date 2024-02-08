using UnityEditor;
using UnityEngine;
using System.IO;
using System;

public class FBXToPrefabConverter : MonoBehaviour
{

    [MenuItem("Assets/Convert Meshes to Prefabs", false, 1)]
    private static void ConvertMeshesToPrefabs()
    {
        var selected = Selection.activeObject;
        string assetPath = AssetDatabase.GetAssetPath(selected);

        // Check if the selected asset is an FBX file
        if (Path.GetExtension(assetPath).ToLower() == ".fbx")
        {
            string baseFolder = Path.GetDirectoryName(assetPath);
            CreateRequiredFolders(baseFolder);
            ExtractAssets(assetPath, baseFolder);
        }
        else
        {
            Debug.LogWarning("Selected file is not an FBX file.");
        }
    }

    private static void CreateRequiredFolders(string baseFolder)
    {
        // Create folders if they do not exist
        CreateFolderIfNeeded(baseFolder, "Meshes");
        CreateFolderIfNeeded(baseFolder, "Prefabs");
        CreateFolderIfNeeded(baseFolder, "Materials");
        CreateFolderIfNeeded(baseFolder, "Textures");
        AssetDatabase.Refresh();
    }

    private static void CreateFolderIfNeeded(string basePath, string folderName)
    {
        string fullPath = Path.Combine(basePath, folderName);
        if (!AssetDatabase.IsValidFolder(fullPath))
        {
            AssetDatabase.CreateFolder(basePath, folderName);
        } else
        {
            Debug.LogWarning("Folder " + folderName + " already exist!");
        }
    }

    private static void ExtractAssets(string assetPath, string baseFolder)
    {
        // Load the FBX file
        GameObject fbx = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

        if (fbx == null)
        {
            Debug.LogError("Failed to load FBX file.");
            return;
        }

        // Create a parent GameObject to hold all instantiated prefabs
        GameObject parentObject = new GameObject(fbx.name + "_Prefabs");

        // Extract Meshes
        MeshFilter[] meshFilters = fbx.GetComponentsInChildren<MeshFilter>();
        foreach (MeshFilter mf in meshFilters)
        {
            string meshPath = Path.Combine(baseFolder, "Meshes", mf.sharedMesh.name + ".asset");
            AssetDatabase.CreateAsset(UnityEngine.Object.Instantiate(mf.sharedMesh), meshPath);
        }

        // Extract Materials and Textures
        Renderer[] renderers = fbx.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            foreach (Material material in renderer.sharedMaterials)
            {
                string materialPath = Path.Combine(baseFolder, "Materials", material.name + ".mat");
                Material newMaterial = new Material(material);
                AssetDatabase.CreateAsset(newMaterial, materialPath);

                // Extract and reassign textures
                //ExtractAndAssignTextures(material, newMaterial, baseFolder);
            }
        }

        // Create Prefabs and instantiate them in the scene
        foreach (MeshFilter mf in meshFilters)
        {
            GameObject prefab = new GameObject(mf.sharedMesh.name);
            MeshRenderer meshRenderer = prefab.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(Path.Combine(baseFolder, "Materials", mf.GetComponent<Renderer>().sharedMaterial.name + ".mat"));
            MeshFilter meshFilter = prefab.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(Path.Combine(baseFolder, "Meshes", mf.sharedMesh.name + ".asset"));

            string prefabPath = Path.Combine(baseFolder, "Prefabs", mf.sharedMesh.name + ".prefab");
            PrefabUtility.SaveAsPrefabAsset(prefab, prefabPath);
            UnityEngine.Object.DestroyImmediate(prefab); // Cleanup

            // Instantiate prefab in the scene with original transform
            GameObject scenePrefab = PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath)) as GameObject;
            if (scenePrefab != null)
            {
                Transform originalTransform = mf.transform;
                scenePrefab.transform.position = originalTransform.position;
                scenePrefab.transform.rotation = originalTransform.rotation;
                scenePrefab.transform.localScale = originalTransform.localScale;
                scenePrefab.transform.SetParent(parentObject.transform, true);
            }
        }
        AssetDatabase.Refresh();
    }

}