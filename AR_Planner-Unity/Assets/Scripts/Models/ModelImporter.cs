using System.IO;
using UnityEngine;
using UnityEditor;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;

public class ModelImporter : MonoBehaviour
{
    public static string parentModel;

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

        GameObject importedObj = Instantiate(model, Vector3.zero, Quaternion.identity); // Set model to origin coordinates ([0,0,0]) and set model with 0 roatation in any axis. 

        float scaleFactor = .001f; // Unity standard UOM is meters and Slicer UOM is mm. 
        importedObj.transform.localScale = Vector3.one * scaleFactor; // Scale model down form meters to mm

        BoxCollider boxCollider = importedObj.AddComponent<BoxCollider>();

        AddTightBoxColliderToMeshObject(importedObj); // Resize box collider to roughly the size of the model
        AddScriptsToPrefab(importedObj); // Add necessary scripts to the model

        


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

        // Check if there is a folder for the patient in the spinePrefabs folder
        if (!Directory.Exists(Path.Combine(prefabPath, patientID)))
        {
            // Create patient prefab folder if one does not exist
            AssetDatabase.CreateFolder(prefabPath, patientID);
        }

        string prefabFilePath = Path.Combine(prefabPath, patientID, modelName + ".prefab"); //This is the file path where newly constructed prefabs will be saved to. 

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

        DestroyImmediate(importedObj); // Removes model form hierarchy to keep scene view clean. 
    }

    // Logic for sizing box collider to roughtly the size of the model it is attached to. 
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

    // Logic for adding necessary scripts to a model depending on if the model is the parent or a child model. 
    public static void AddScriptsToPrefab(GameObject prefab)
    {
        // Add ModelInfo script
        ModelInfo modelInfoScript = prefab.AddComponent<ModelInfo>();
        // Add ObjectManipulator script
        ObjectManipulator objectManipulatorScript = prefab.AddComponent<ObjectManipulator>();

        // Modify the TwoHandedManipulationType after adding the component
        // If ExcludeScaleManipulation() method is not found, add it to the ObjectManipulator script in the SerializedFields section under the TwoHandedManipulationType transform flags declerations
        /*
            public void ExcludeScaleManipulation()
            {
                twoHandedManipulationType &= ~TransformFlags.Scale;
            }
        */
        if (objectManipulatorScript != null)
        {
            objectManipulatorScript.ExcludeScaleManipulation();
        }

        // Add NearInteractionGrabbable script
        NearInteractionGrabbable nearInteractionGrabbableScript = prefab.AddComponent<NearInteractionGrabbable>();
    }
}

