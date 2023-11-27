using System.IO;
using UnityEngine;
using UnityEditor;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;

public class ModelImporter : MonoBehaviour
{
    // This method is called to crate a prafab from an OBJ model
    public static void CreatePrefab(string patientID, string modelName)
    {
        GameObject importedObj = ImportObj(patientID, modelName);
        SavePrefab(importedObj, patientID, modelName);
    }

    // Logic for importing an OBJ model as a GameObject into Unity
    public static GameObject ImportObj(string patientID, string modelName)
    {
        GameObject model = Resources.Load<GameObject>(Path.Combine("Models", "SpineModels", patientID , modelName));

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

    public static void SavePrefab(GameObject importedObj, string patientID, string modelName)
    {
        string prefabPath = Path.Combine("Assets", "Resources", "Prefabs", "SpinePrefabs");

        if (!Directory.Exists(Path.Combine(prefabPath, patientID)))
        {
            AssetDatabase.CreateFolder(prefabPath, patientID);
        }

        string prefabFilePath = Path.Combine(prefabPath, patientID, modelName + ".prefab");

        bool prefabSuccess;
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(importedObj, prefabFilePath, out prefabSuccess);

        if (prefabSuccess)
        {
            Debug.Log(modelName + " prefab was saved successfully");  
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

        // Modify the TwoHandedManipulationType after adding the component
        if (objectManipulatorScript != null)
        {
            objectManipulatorScript.ExcludeScaleManipulation();
        }

        // Add NearInteractionGrabbable script
        NearInteractionGrabbable nearInteractionGrabbableScript = prefab.AddComponent<NearInteractionGrabbable>();
    }
}

