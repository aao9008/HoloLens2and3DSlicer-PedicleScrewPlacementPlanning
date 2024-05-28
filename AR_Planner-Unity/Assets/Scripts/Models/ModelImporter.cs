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

        GameObject parent = Importer.LoadFromFile(gltfPath); // This parent game object may hold one or many meshes.

        Transform parentTransform = parent.transform;

        int count = parentTransform.childCount;

        for(int i = parentTransform.childCount - 1; i >= 0; i--)
        {
            Transform child = parentTransform.GetChild(i);

            string modelName = child.name;

            // Create a new parent GameObject with the name of the child
            GameObject newParent = new GameObject(child.name);
            // Set the newParent's position and rotation to match the child
            newParent.transform.SetPositionAndRotation(child.position, child.rotation);

            // Set the child's name to "grp1"
            child.gameObject.name = "grp1";
            child.SetParent(newParent.transform);

            child.localScale = Vector3.one;

            // Instantiate a prefab as a child of the new parent GameObject
            GameObject newPrefab = Instantiate(newParent, Vector3.zero, Quaternion.identity);

            BoxCollider boxCollider = newPrefab.AddComponent<BoxCollider>();

            AddTightBoxColliderToMeshObject(newPrefab, modelName); // Resize box collider to roughly the size of the model

            Debug.Log("Hi from model importer. The parent model is: " + parentModel);

            AddScriptsToPrefab(newPrefab, modelName); // Add necessary scripts to the model

            newPrefab.transform.localScale = Vector3.one * 0.001f; // Set scale to 0.001

            newPrefab.name = modelName; // Set name to modelName


            models.Add(newPrefab);

            DestroyImmediate(newParent);
            
        }
        DestroyImmediate(parent);

        return models; 
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

        //AdjustPivotToCenter adjustPivot = prefab.AddComponent<AdjustPivotToCenter>();
       // adjustPivot.AdjustPivot();
    }
}

