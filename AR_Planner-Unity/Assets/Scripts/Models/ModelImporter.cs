using System.IO;
using UnityEngine;
using UnityEditor;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UI;
using Siccity.GLTFUtility;
using System.Collections.Generic;

public class ModelImporter : MonoBehaviour
{
    public static string parentModel;
    public PressableButtons pressableButtonsScript;

    
    void Start()
    {
        // Find the "Models" GameObject
        GameObject modelsObject = GameObject.Find("Models");

        if (modelsObject == null) {
            Debug.Log("Models object is null!");
        }

        // Access Parent model variable in the PressableButtons script
        parentModel = pressableButtonsScript.parentModel;

        Debug.Log("Test 117, Parent Model is: " + parentModel);
    }
    

    public static void CreatePrefabsFromOBJ(string patientID, string[] modelsList)
    {
        

        // Iterate over models list and crate a prefab
        foreach (string model in modelsList)
        {
            CreatePrefabFromOBJ(patientID, model);
        }
    }

    public static List<GameObject> CreatePrefabsFromGLTF(string patientID, string gltfPath)
    {

        List <GameObject> models = new List<GameObject>();

        // Load the model from the GLTF file
        GameObject gltfModel = Importer.LoadFromFile(gltfPath); // This parent game object may hold one or many meshes.
        Transform parentTransform = gltfModel.transform;

        // Iterate over each mesh (structure/organ) within the gltf model
        for(int i = parentTransform.childCount - 1; i >= 0; i--)
        {
            Transform child = parentTransform.GetChild(i);
            string modelName = child.name; // Name of organ/structure

            /* Desired Object structure for child models: 
             * 
             * Empty Gameobject "Organ Name" (used for transformations and manipulations)
             *  |
             *  |__"grp1" (object holds mesh data of child model)
            */

            // rename object holding mesh data to "grp1"
            child.gameObject.name = "grp1";

            // Create emptpy gameobject and set object as parent of meshdata
            GameObject newParent = CreateNewParent(child);
            child.SetParent(newParent.transform);
            child.localScale = Vector3.one;

            // Instantiate the new prefab and set up its components
            GameObject newPrefab = Instantiate(newParent, Vector3.zero, Quaternion.identity);
            SetupPrefab(newPrefab, modelName);

            models.Add(newPrefab);

            // Clean up the temporary new parent
            Object.DestroyImmediate(newParent);   
        }
        DestroyImmediate(gltfModel);

        return models; 
    }

    private static GameObject CreateNewParent(Transform child)
    {
        GameObject newParent = new GameObject(child.name);
        newParent.transform.SetPositionAndRotation(child.position, child.rotation);
        return newParent;
    }

    private static void SetupPrefab(GameObject prefab, string modelName)
    {
        if(modelName == parentModel) // Only primary parent model needs scripts and a box collider
        {
            prefab.AddComponent<BoxCollider>();
            AddTightBoxColliderToMeshObject(prefab, modelName); // Resize box collider to roughly the size of the model
            AddScriptsToPrefab(prefab, modelName); // Add necessary scripts to the model
        }

        prefab.transform.localScale = Vector3.one * 0.001f;
        prefab.name = modelName;
    }

    // This method is called to crate a prafab from an OBJ model
    public static void CreatePrefabFromOBJ(string patientID, string modelName)
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

        AddTightBoxColliderToMeshObject(importedObj, modelName); // Resize box collider to roughly the size of the model

        Debug.Log("Hi from model importer. The parent model is: " + parentModel);

        AddScriptsToPrefab(importedObj, modelName); // Add necessary scripts to the model

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
    public static void AddTightBoxColliderToMeshObject(GameObject obj, string modelName)
    {
        // If prefab is marked as parent 
        if (modelName != parentModel)
        {
            return;
        }
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

        
        // Double the height while maintaining the center position
        Vector3 newSize = boxCollider.size;
        newSize.y *= -2;
        newSize.x *= 1.35f;
        newSize.z *= 1.35f;
        boxCollider.size = newSize;

        //Adjust the center position to maintain the lower edge at the same position
        Vector3 newCenter = boxCollider.center;
        newCenter.y += newSize.y * 0.25f; // Adjust the center by half of the increased height
        boxCollider.center = newCenter;
  
    }

    // Logic for adding necessary scripts to a model depending on if the model is the parent or a child model. 
    public static void AddScriptsToPrefab(GameObject prefab, string modelName)
    {
        // Add ModelInfo script
        ModelInfo modelInfoScript = prefab.AddComponent<ModelInfo>();
        // Add ObjectManipulator script

        // If current model is not the parent model
        if (modelName != parentModel)
        {
            // Exit the function, child models do not need manipulation scripts. 
            return;
        }

        ObjectManipulator objectManipulatorScript = prefab.AddComponent<ObjectManipulator>();

        // Modify the TwoHandedManipulationType after adding the component
        // If ExcludeScaleManipulation() method is not found, add it to the ObjectManipulator script in the SerializedFields section under the TwoHandedManipulationType transform flags declerations
        /*
            public void ExcludeScaleManipulation()
            {
                twoHandedManipulationType &= ~TransformFlags.Scale;
            }
        */
        
        // Remove ability for user to scale models with both hands. 
        objectManipulatorScript.ExcludeScaleManipulation();
 
        // Add NearInteractionGrabbable script
        NearInteractionGrabbable nearInteractionGrabbableScript = prefab.AddComponent<NearInteractionGrabbable>();
    }
}

