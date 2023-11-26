using System.IO;
using UnityEngine;
using UnityEditor;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;

public class ModelImporter : MonoBehaviour
{
    public static void CreatePrefab(string modelName)
    {
        GameObject importedObj = ImportObj(modelName);
        SavePrefab(importedObj, modelName);
    }

    public static GameObject ImportObj(string modelName)
    {
        GameObject model = Resources.Load<GameObject>("Models/SpineModels/Patient Pua/" + modelName);

        GameObject importedObj = Instantiate(model, Vector3.zero, Quaternion.identity);

        float scaleFactor = .001f;
        importedObj.transform.localScale = Vector3.one * scaleFactor;

        BoxCollider boxCollider = importedObj.AddComponent<BoxCollider>();

        AddTightBoxColliderToMeshObject(importedObj);
        AddScriptsToPrefab(importedObj);
        

        if (importedObj == null)
        {
            Debug.Log("Obj model not loaded");
            return null;
        }

        return importedObj;
    }

    public static void SavePrefab(GameObject importedObj, string modelName)
    {
        string prefabPath = Path.Combine("Assets", "Resources", "Prefabs", "SpinePrefabs");

        if (!Directory.Exists(Path.Combine(prefabPath, "Patient Pua")))
        {
            AssetDatabase.CreateFolder(prefabPath, "Patient Pua");
        }

        string prefabFilePath = Path.Combine(prefabPath, "Patient Pua", modelName + ".prefab");

        bool prefabSuccess;
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(importedObj, prefabFilePath, out prefabSuccess);

        if (prefabSuccess)
        {
            Debug.Log("Prefab was saved successfully");  
        }
        else
        {
            Debug.Log("Prefab failed to save");
        }

        DestroyImmediate(importedObj);
    }

    public static void AddTightBoxColliderToMeshObject(GameObject obj)
    {
        // Get or add a BoxCollider component to the GameObject
        BoxCollider boxCollider = obj.GetComponent<BoxCollider>();
        if (boxCollider == null)
        {
            boxCollider = obj.AddComponent<BoxCollider>();
        }

        // Find the child object named "grp1"
        Transform grp1 = obj.transform.Find("grp1");

        if (grp1 != null)
        {
            MeshFilter meshFilter = grp1.GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                Mesh mesh = meshFilter.sharedMesh;

                // Get the vertices of the mesh
                Vector3[] vertices = mesh.vertices;

                // Set initial min and max values
                Vector3 min = vertices[0];
                Vector3 max = vertices[0];

                // Find the min and max extents of the mesh
                foreach (Vector3 vertex in vertices)
                {
                    min = Vector3.Min(min, vertex);
                    max = Vector3.Max(max, vertex);
                }

                // Calculate the center of the box
                Vector3 center = (min + max) * 0.5f;

                // Calculate the size of the box
                Vector3 size = max - min;

                // Set the center and size of the BoxCollider
                boxCollider.center = center;
                boxCollider.size = size;
            }
        }
    }

    public static void AddScriptsToPrefab(GameObject prefab)
    {
        // Add ModelInfo script
        ModelInfo modelInfoScript = prefab.AddComponent<ModelInfo>();
        // Add ObjectManipulator script
        ObjectManipulator objectManipulatorScript = prefab.AddComponent<ObjectManipulator>();
        // Add NearInteractionGrabbable script
        NearInteractionGrabbable nearInteractionGrabbableScript = prefab.AddComponent<NearInteractionGrabbable>();

        // You can optionally configure or set properties of the added scripts here
        // For example:
        // modelInfoScript.SetModelInfoData(data);
    }
}

