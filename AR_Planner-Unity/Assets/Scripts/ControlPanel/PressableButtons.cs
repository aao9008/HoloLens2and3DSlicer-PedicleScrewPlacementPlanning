// This code was developed by Alicia Pose Díez de la Lastra, from Universidad Carlos III de Madrid
// This is the main script of the project. 
// It creates all the functions associated to the pressable buttons in the ControlPanel

// First, import some libraries of interest
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using TMPro;

using Microsoft;
using Microsoft.MixedReality.Toolkit.Utilities;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Input;


using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using System.IO;

// Define the class PressableButtons
public class PressableButtons : MonoBehaviour
{
    /// GENERAL INFORMATION ///    
    Transform modelsParentTransform; // Parent Gameobject that contains all the models in the scene. In this app, it is called "Models"
    GameObject openIGTLinkConnectScriptHolder; // Gameobject that contains the openIGTLink script. We import it to access infoToSendArray
    List<ModelInfo> infoToSendArray; // Array of elements that will be sent to 3D Slicer. In our case, the Spine, the image plane and all the screws
    public List<string> patientIDs; // Array of names of patient folders. These folders hold all models for a given patient.
    public string patientID; // Folder name of the patient of interest. This folder name/patient identifier is used to automatically load the models of interest. 
    
    /// Parent Model Information ///
    GameObject spineModel; // Model of the spine corresponding to the patient "patientNumber"
    Material spine_mat; // Material associated to the spine model
    Material clipping_mat; // Material with the clipping property
    Color fixSpineColor; // Color of the spine when it's fixed in the 3D world
    Color mobileSpineColor; // Color of the spine when it can be moved in the 3D world

    /// Model Information /// 
    public string[] patientModels; // List of maodel names within a patients folder
    public string parentModel; // Name of the model which will hold all other additonal child models
    public List<string> target_models; // Name of the model that repersents the region of interest. 
    public List<GameObject> modelList = new List<GameObject>(); // Reference to all child models
    public List<GameObject> gltfModels = new List<GameObject>(); // This list will hold references to all GLTF models. 

    /// SCREWS INFORMATION ///
    Material screwMobile_mat; // Material of the screw when it can be moved in the 3D world
    Material screwFixed_mat; // Material of the screw when it's fixed in the 3D world
    public int screwSelected; // Integer that identifies the screw selected in the scene
    TextMeshPro screwSelected_label; // UI label that shows the number of the screw selected and its dimensions
    string[] diametersArray; // Array of possible screw diameters
    int diameterIndex; // Index of the diameter selected in the array. It iterates over all the elements in diametersArray
    string[] lengthsArray; // Array of possible screw lengths
    int lengthIndex; // Index of the length selected in the array. It iterates over all the elements in lengthsArray
    string screwPrefabsPath; // Path to the screw prefabs

    // Define a delegate type for the callback
    public delegate void ModelListReadyCallback();

    // Define an event using the delegate
    public static event ModelListReadyCallback ModelListReady;

    ////////////////////////////////// START //////////////////////////////////////
    void Start()
    {
        // Initialize the Main Camera in the scene with black background
        Camera mainCamera = GameObject.Find("MixedRealityPlayspace").transform.Find("Main Camera").GetComponent<Camera>();
        mainCamera.clearFlags = CameraClearFlags.SolidColor;

        // Identify the modelsParentTransform in the scene
        modelsParentTransform = GameObject.Find("Models").transform; // Identify the modelsParentTransform in the scene

        Debug.Log(gltfModels.Count);

        if (gltfModels.Count < 1)
        {
            Debug.Log("I made it to the correct obj path");
            // Instantiate the spine model corresponding to the patient of interest
            string spineModelName = parentModel; // Spine filename
            string spineModelPath = Path.Combine("Prefabs", "SpinePrefabs", patientID, parentModel); // Path to the spine prefab
            GameObject spineItem = Resources.Load(spineModelPath) as GameObject; // Load the spine Model
            spineModel = GameObject.Instantiate(spineItem, modelsParentTransform) as GameObject; // Instantiate the spine as a child of modelsParentTransform
            spineModel.name = spineModelName; // Change the spine model name to spineModelName
            spineModel.transform.localPosition = Vector3.zero; // Set the model in the origin of coordinates ([0,0,0])
            //spineModel.transform.localRotation = Quaternion.identity; // Set the model with 0 rotation in any axis
            spineModel.transform.eulerAngles = new Vector3(0, 0, 180); // patient is in supine position by default

            CreateChildOBJModels();
        }
        else if (gltfModels.Count > 1)
        {
            GameObject parentObject = null; // Assign a default value
            foreach (GameObject model in gltfModels)
            {
                if (model.name == parentModel)
                {
                    parentObject = model;
                    break; // Exit the loop once the object is found
                }
            }

            // Instantiate the spine model corresponding to the patient of interest
            GameObject spineItem = parentObject;

            Debug.Log(spineItem.name);

            string spineModelName = parentObject.name; // Spine filename
            spineModel = GameObject.Instantiate(spineItem, modelsParentTransform) as GameObject; // Instantiate the spine as a child of modelsParentTransform
            spineModel.name = spineModelName; // Change the spine model name to spineModelName
            spineModel.transform.localPosition = Vector3.zero; // Set the model in the origin of coordinates ([0,0,0])
                                                               //spineModel.transform.localRotation = Quaternion.identity; // Set the model with 0 rotation in any axis
            spineModel.transform.eulerAngles = new Vector3(0, 0, 180); // patient is in supine position by defaultn

            CreateChildGLTFModels();
        }

        // Intantiate the image body within the spine model
        string imageModelPath = Path.Combine("Prefabs", "ImagePrefab", "MobileCTPlane"); // Path to the image prefab
        GameObject imageItem = Resources.Load(imageModelPath) as GameObject; // Load the image model
        GameObject imageModel = GameObject.Instantiate(imageItem, spineModel.transform) as GameObject; // Instantiate the image as a child of the spineModel
        imageModel.name = "Image"; // Change the imageModel name
        imageModel.transform.localPosition = Vector3.zero; // Set the model in the origin of coordinates ([0,0,0])
        imageModel.transform.localRotation = Quaternion.identity; // Set the model with 0 rotation in any axis
        imageModel.SetActive(false); // Hide the image in the scene
        
        // Access the infoToSendArray from the OpenIGTLinkConnect script
        OpenIGTLinkConnect openIGTLinkConnectScript = GameObject.Find("OpenIGTLinkConnectHandler").GetComponent<OpenIGTLinkConnect>(); // Find the GameObject in the hierarchy that holds the OpenIGTLinkConnect script
        infoToSendArray = openIGTLinkConnectScript.infoToSend; // Get the infoToSendArray element from this script      
        

        // Initialize the spine colors that will indicate if its mobile or not
        spine_mat = Resources.Load("Materials/Spine_mat") as Material; // Load the material of interest from the path
        fixSpineColor = new Color(0.8f, 0.8f, 0.4f, 0.1f); // Define the fixSpineColor in the RGBA format
        mobileSpineColor = new Color(0.8f, 0.5f, 0.5f, 1.0f); // Define the mobileSpineColor in the RGBA format
        //// Set the initial color to mobile
        spine_mat.SetColor("_Color", mobileSpineColor); // Set mobileSpineColor as the initial color of the spine
        spineModel.GetComponentInChildren<MeshRenderer>().material = spine_mat; // Assign this color to the spineModel
        //// Also set the clipping plane color to mobile
        clipping_mat = Resources.Load("Materials/Clipping_mat") as Material; // Load the material of interest from the path
        clipping_mat.SetColor("_Color", mobileSpineColor); // Align the color of clipping_mat with the color of the spine (mobileSpineColor in this case) 


        // Initialize the spine colors that will indicate if its mobile or not
        anatomy_mat = Resources.Load("Materials/Anatomy_mat") as Material;
        anatomyModel.GetComponentInChildren<MeshRenderer>().material = anatomy_mat;

        targets_mat = Resources.Load("Materials/Targets_mat") as Material;
        targetsModel.GetComponentInChildren<MeshRenderer>().material = targets_mat;


        // Initialize the screw variables
        screwSelected = 0;
        screwPrefabsPath = Path.Combine("Prefabs", "ScrewPrefabs"); // Path to the screw prefabs
        diametersArray = new string[]{"4.5", "5", "6"}; // Define possible diameters
        diameterIndex = 0;
        lengthsArray = new string[]{"30", "35", "40", "45", "50", "55", "60"}; // Define possible lengths
        lengthIndex = 0; 
        //// Initialize the materials
        screwMobile_mat = Resources.Load("Materials/ScrewMobile_mat") as Material; // Load the material of interest from the path
        screwFixed_mat = Resources.Load("Materials/ScrewFixed_mat") as Material; // Load the material of interest from the path
        //// Set the screw number label
        screwSelected_label = GameObject.Find("ControlPanel").transform.Find("ScrewButtons").transform.Find("ButtonCollection").transform.Find("ScrewNumberLabel").GetComponent<TextMeshPro>(); // Find the screwSelected_label GameObject in the hierarchy and retrieve its TextMeshPro components
        screwSelected_label.text = "Needle X"; // Set the initial label to "Needle X" (The scene is initialized with no screws on the scene)
        
        //// Initialize the image handler color
        Material imageHandlerMobile_mat = Resources.Load("Materials/ImageMobile_mat") as Material; // Path to the mobile image color
        GameObject imageHandler = imageModel.transform.Find("ImageHandler").gameObject;
        imageHandler.GetComponent<MeshRenderer>().material = imageHandlerMobile_mat;
        
        // Initialize the variables of the rest of scripts in the scene
        ///// Initialize MODELSCALING script
        modelsParentTransform.transform.GetComponent<ModelScaling>().spineModel = spineModel;
        modelsParentTransform.transform.GetComponent<ModelScaling>().modelsParent = modelsParentTransform;

        ///// Initialize SWITCHBUTTONS script
        GameObject switchButtonsScriptHolder = GameObject.Find("ControlPanel");
        switchButtonsScriptHolder.GetComponent<SwitchButtons>().spineModel = spineModel;
        switchButtonsScriptHolder.GetComponent<SwitchButtons>().mobileImageGO = imageModel;
        switchButtonsScriptHolder.GetComponent<SwitchButtons>().spine_mat = spine_mat;
        switchButtonsScriptHolder.GetComponent<SwitchButtons>().clipping_mat = clipping_mat;
        switchButtonsScriptHolder.GetComponent<SwitchButtons>().imageHandler = imageHandler;
        switchButtonsScriptHolder.GetComponent<SwitchButtons>().imageHandlerMobile_mat = imageHandlerMobile_mat;

        ///// Initialize OPENIGTLINKCONNECT script
        infoToSendArray.Add(spineModel.GetComponent<ModelInfo>());
        infoToSendArray.Add(imageModel.GetComponent<ModelInfo>());
        openIGTLinkConnectScript.movingPlane = imageModel.transform.Find("MovingPlane").gameObject;

        ///// Initialize CANVASBUTTONS script
        GameObject canvasButtonsScriptHolder = GameObject.Find("Canvas");
        canvasButtonsScriptHolder.GetComponent<CanvasButtons>().spineGO = spineModel;
        canvasButtonsScriptHolder.GetComponent<CanvasButtons>().screwFixed_mat = screwFixed_mat;

    }

    ///////////////////////////////// Child Model Creation Logic////////////////////
    public void CreateChildGLTFModels()
    {
        foreach (GameObject model in gltfModels)
        {
            // Do not create parentModel as a child
            if (model.name == parentModel)
            {
                Destroy(GameObject.Find(model.name));
                continue;
            }

            GameObject childModel = CreatChildGLTFModel(model); // Create instance of childModel from the GLTF reference

            FormatMat(childModel);

            modelList.Add(childModel); // Add new instatniated model to modelsList for voice command functionality
            Destroy(GameObject.Find(model.name)); // Remove GLTF refernce from hierarchy
        }

        // Populate your model list...
        // When the list is ready, invoke the callback
        InvokeModelListReady();
    }

    // This function creates a child model from GLTF file
    public GameObject CreatChildGLTFModel(GameObject model)
    {
        GameObject childModel; // Model of the spine corresponding to the patient "patientNumber"

        string childModelName = model.name; // Child model filename
        childModel = GameObject.Instantiate(model, spineModel.transform) as GameObject; // Instantiate the spine as a child of modelsParentTransform
        childModel.name = childModelName; // Change the spine model name to spineModelName
        childModel.transform.localPosition = Vector3.zero; // Set the model in the origin of coordinates ([0,0,0])
        childModel.transform.localRotation = Quaternion.identity; // Set the model with 0 rotation in any axis

        // Reset the child model's local scale to (1, 1, 1)
        // When objects are nested in Unity, their transformations are relative to their parent.
        // If the parent (spineModel) has a scale different from (1, 1, 1), it impacts the child's size.
        // For instance, if a child object is scaled by 0.001 and then nested under a parent object scaled by .001, the overall size becomes the product of these scales.
        childModel.transform.localScale = Vector3.one;

        return childModel;
    }


    // This function creates all child models from OBJ files
    public void CreateChildOBJModels()
    {
        foreach (string model in patientModels)
        {
            if (model == parentModel)
            {
                continue;
            }

            GameObject childModel = CreatChildOBJModel(model);



            if (MTLPresent(childModel.name))
            {
                FormatMat(childModel);
            }
            else
            {
                AssignDefaultMat(childModel);
            }

            modelList.Add(childModel);
        }

        // Populate your model list...
        // When the list is ready, invoke the callback
        InvokeModelListReady();
    }

    // Method to invoke the callback when the model list is ready
    void InvokeModelListReady()
    {
        Debug.Log("invoke called");

        // Check if anyone is subscribed to the event
        if (ModelListReady != null)
        {
            ModelListReady.Invoke();
        }
        else
        {
            Debug.Log("No subscribers to OnModelListReady event");
        }
    }

    // This function creates a child model 
    public GameObject CreatChildOBJModel(string model)
    {
        GameObject childModel; // Model of the spine corresponding to the patient "patientNumber"

        string childModelName = model; // Child model filename
        string childModelPath = Path.Combine("Prefabs", "SpinePrefabs", patientID, model); // Path to the spine prefab
        GameObject childItem = Resources.Load(childModelPath) as GameObject; // Load the spine Model
        childModel = GameObject.Instantiate(childItem, spineModel.transform) as GameObject; // Instantiate the spine as a child of modelsParentTransform
        childModel.name = childModelName; // Change the spine model name to spineModelName
        childModel.transform.localPosition = Vector3.zero; // Set the model in the origin of coordinates ([0,0,0])
        childModel.transform.localRotation = Quaternion.identity; // Set the model with 0 rotation in any axis

        // Reset the child model's local scale to (1, 1, 1)
        // When objects are nested in Unity, their transformations are relative to their parent.
        // If the parent (spineModel) has a scale different from (1, 1, 1), it impacts the child's size.
        // For instance, if a child object is scaled by 0.001 and then nested under a parent object scaled by .001, the overall size becomes the product of these scales.
        childModel.transform.localScale = Vector3.one;

        return childModel;
    }
    
    // This function determines whether a model has MTL data associated with it or not
    public bool MTLPresent(string modelName)
    {
        //Get the path of the MTL file associated witht the model
        string mtlPath = PatientModelsFetcher.GetMTLPath(patientID, modelName); 

        // Check if the MTL file exists at the specified path
        if (File.Exists(mtlPath))
        {
            
            return true; // Return true indicating the MTL file exists
        }

        // Return false indicating the MTL file does not exist
        return false;
    }

    // This function is called when OBJ model has an associated MTL file
    public void FormatMat(GameObject childModel)
    {
        Material mtl_mat = childModel.GetComponentInChildren<MeshRenderer>().material;
        Color mtlColor = mtl_mat.color;

        // If the model is marked as a target, no need to make the model transparent, just copy the MTL color onto a Unity mat asset.
        if (target_models.Contains(childModel.name))
        {
            Material reference_mat = Resources.Load("Materials/Targets_mat") as Material;

            Material new_mat = new Material(reference_mat);

            new_mat.color = mtlColor;

            childModel.GetComponentInChildren<MeshRenderer>().material = new_mat;

            return;
        }

        // Load the custom material, this material has all transparency settings preset. 
        Material child_mat = Resources.Load("Materials/Child_mat") as Material;

        // Create a copy of the custom mateiral to prevent modifiying the original asset
        // Each childModel will have it's own uniqe material copy
        Material transparentMaterial = new Material(child_mat);

        // Copy the color from the orignal material 
        Color transparentColor = new Color(mtlColor.r, mtlColor.g, mtlColor.b, 0.2745f);
        transparentMaterial.color = transparentColor;

        // Apply the modified transparent material to the renderer of the childModel.
        childModel.GetComponentInChildren<MeshRenderer>().material = transparentMaterial;
    }

    // This function is called when an OBJ model has no associated MTL file 
    public void AssignDefaultMat(GameObject childModel)
    {
        Material anatomy_mat;
        Material target_mat;

        if (target_models.Contains(childModel.name))
        {
            target_mat = Resources.Load("Materials/Targets_mat") as Material;
            childModel.GetComponentInChildren<MeshRenderer>().material = target_mat;
        }
        else
        {
            //Material associated to the spine model                     
            anatomy_mat = Resources.Load("Materials/Anatomy_mat") as Material;
            childModel.GetComponentInChildren<MeshRenderer>().material = anatomy_mat;
        }   
    }

    ////////////////////////////////// WIDGET //////////////////////////////////////

    // This function is called everytime the user creates a new screw, either clicking the corresponding button or speaking the associated voice command
    public void OnCreateScrewClicked()
    {
        // Find the number of screws in the scene
        int numberOfScrews = GameObject.FindGameObjectsWithTag("Screw").Length;
        // Set the local position and rotation of the new screw
        Vector3 localPosition = new Vector3(0, 0, 0);
        Vector3 localRotation = new Vector3(0, 0, 0);
        // If there was a screw previously selected (highlighted), dissable its emission mark
        try
        {
            GameObject screwGO = GameObject.Find("Screw-" + screwSelected).gameObject;
            SelectThisScrew(screwGO, false);
        } 
        catch{}    
        // Create the new screw
        ModelInfo screwMI = CreateScrew(numberOfScrews + 1, modelsParentTransform, spineModel, localPosition, localRotation);

        // Highlight the new screw to mark it as "selected"
        screwSelected = screwMI._number;
        screwSelected_label.text =  "Screw " + screwMI._number + ":\nD" + screwMI._diameter + "L" + screwMI._length;
        SelectThisScrew(screwMI._gameObject, true);
    }

    // This function is called everytime the user deletes a screw, either clicking the corresponding button or speaking the associated voice command
    public void OnDeleteScrewClicked()
    {
        // Find the number of screws in the scene
        GameObject[] screwModels = GameObject.FindGameObjectsWithTag("Screw");
        int numberOfScrews = screwModels.Length;
        // If there is at least one screw on the scene, delete the selected one
        if (numberOfScrews > 0)
        {
            DeleteScrew(screwModels);
        }
        // If there are no screws on the scene, do nothing
        else
        {
            Debug.Log("No more screws on scene");
        }
        // Once deleted the selected Screw, let's check if there are any remaining screws in the scene and select the last one
        numberOfScrews = GameObject.FindGameObjectsWithTag("Screw").Length;
        screwSelected--;
        if (numberOfScrews > 0)
        {
            // If the screw deleted was Screw-1, now screwSelected is 0. Set it to 1
            if (screwSelected == 0)
            {
               screwSelected = 1;
            }
            // Fin the new selected screw, highlight it in the scene and update the screw label in the UI
            GameObject screwGO = GameObject.Find("Screw-" + screwSelected).gameObject;
            ModelInfo screwMI = screwGO.GetComponent<ModelInfo>();
            SelectThisScrew(screwMI._gameObject, true);
            screwSelected_label.text =  "Screw " + screwMI._number+ ":\nD" + screwMI._diameter + "L" + screwMI._length;
        }
        // If there are no more screws in the scene, set screwSelected to 0 and reset the screw label in the UI as Screw X
        else
        {
            screwSelected = 0;
            screwSelected_label.text =  "Screw X";
        }
    }

    // This function is called everytime the user fixes a screw in the 3D world, either clicking the corresponding button or speaking the associated voice command
    public void OnReleaseScrewClicked()
    {
        // Check if there are any screws in the scene
        int numberOfScrews = GameObject.FindGameObjectsWithTag("Screw").Length;
        // If there is at least one, continue
        if (numberOfScrews > 0)
        {
            // Find the screw with the number requested
            GameObject screwGO = GameObject.Find("Screw-" + screwSelected).gameObject;
            // Access to the model's information
            ModelInfo screwMI = screwGO.GetComponent<ModelInfo>();
            // Make it non-modifiable
            ModifyScrew(screwMI._gameObject, false);
            // Update the model information with the new color
            Color modelColor = screwMI._gameObject.GetComponentInChildren<MeshRenderer>().material.color;
            screwMI._color = modelColor[0].ToString() + "," + modelColor[1].ToString() + "," + modelColor[2].ToString();
            // Highlight this screw
            SelectThisScrew(screwMI._gameObject, true);
        }
        else
        {
            Debug.Log("No screws on scene");
        }
    }


    // This function is called everytime the user enables the manipulation of a screw in the 3D world, either clicking the corresponding button or speaking the associated voice command
    public void OnModifyScrewClicked()
    {
        // Check if there are any screws in the scene
        int numberOfScrews = GameObject.FindGameObjectsWithTag("Screw").Length;
        // If there is at least one, continue
        if (numberOfScrews > 0)
        {
            // Find the screw with the number requested
            GameObject screwGO = GameObject.Find("Screw-" + screwSelected).gameObject;
            // Access to the model's information
            ModelInfo screwMI = screwGO.GetComponent<ModelInfo>();
            // Make it modifiable
            ModifyScrew(screwMI._gameObject, true);
            // Update the model information with the new color
            Color modelColor = screwMI._gameObject.GetComponentInChildren<MeshRenderer>().material.color;
            screwMI._color = modelColor[0].ToString() + "," + modelColor[1].ToString() + "," + modelColor[2].ToString();
            // Highlight this screw
            SelectThisScrew(screwMI._gameObject, true);
        }
        else
        {
            Debug.Log("No screws on scene");
        }
    }

    // This function is called everytime the user iterates to the next screw in the scene, either clicking the corresponding button or speaking the associated voice command
    public void OnNextScrewClicked()
    {
        // Find the number of screws in the scene
        int numberOfScrews = GameObject.FindGameObjectsWithTag("Screw").Length;
        // If there are any, iterate over them
        if (numberOfScrews > 0)
        {
            NextScrew();
        }
        // If there are no screws in the scene, do nothing
        else
        {
            Debug.Log("No screws on scene");
        }
    }

    // This function is called everytime the user fixes the spine in the 3D world, either clicking the corresponding button or speaking the associated voice command
    public void OnReleaseSpineClicked()
    {
        ModifySpine(spineModel, false, spine_mat, clipping_mat, fixSpineColor);
    }

    // This function is called everytime the user enables the manipulation of the spine in the 3D world, either clicking the corresponding button or speaking the associated voice command
    public void OnModifySpineClicked()
    {
        ModifySpine(spineModel, true, spine_mat, clipping_mat, mobileSpineColor);
    }

    // This function is called everytime the user iterates to the next possible diameter, either clicking the corresponding button or speaking the associated voice command
    public void OnNextDiameterClicked()
    {
        int numberOfScrews = GameObject.FindGameObjectsWithTag("Screw").Length;
        if (numberOfScrews > 0)
        {
            ChangeScrewDiameter();
        }
        else
        {
            Debug.Log("No screws on scene");
        }

    }

    // This function is called everytime the user iterates to the next possible length, either clicking the corresponding button or speaking the associated voice command
    public void OnNextLengthClicked()
    {
        int numberOfScrews = GameObject.FindGameObjectsWithTag("Screw").Length;
        if (numberOfScrews > 0)
        {
            ChangeScrewLength();
        }
        else
        {
            Debug.Log("No screws on scene");
        }

    }




    ////////////////////////////////// LOGIC //////////////////////////////////////

    // Make the screw manipulable with the function "MakeObjectManipulable" and change its color accordingly
    public void ModifyScrew(GameObject myGO, bool modifiable)
    {
        MakeObjectManipulable(myGO, modifiable);
        Material screwMat = myGO.GetComponentInChildren<MeshRenderer>().material;
        if (modifiable)
        {
            screwMat = screwMobile_mat;
            myGO.transform.SetParent(modelsParentTransform);
        }
        else
        {
            screwMat = screwFixed_mat;
            myGO.transform.SetParent(spineModel.transform);
        }
        myGO.GetComponentInChildren<MeshRenderer>().material = screwMat;
    }

    // Make the spine manipulable with the function "MakeObjectManipulable" and change its color accordingly
    void ModifySpine(GameObject spineGO, bool boolean, Material spineMat, Material clippingMat, Color spineColor)
    {
        MakeObjectManipulable(spineGO, boolean);
        spineMat.SetColor("_Color", spineColor);
        clippingMat.SetColor("_Color", spineColor);
    }

    // En/unable the gameobject components to make it modifiable (or not)
    public void MakeObjectManipulable(GameObject myGO, bool modifiable)
    {
        myGO.GetComponent<BoxCollider>().enabled = modifiable;
        myGO.GetComponent<NearInteractionGrabbable>().enabled = modifiable;
        myGO.GetComponent<ObjectManipulator>().enabled = modifiable;
    }

    // Create a new screw
    ModelInfo CreateScrew(int screwNumber, Transform screwParentTransform, GameObject spineGO, Vector3 localPosition, Vector3 localRotation)
    {
        // Access the path where all the screw prefabs are stored
        string screwPath = Path.Combine(screwPrefabsPath, "D" + diametersArray[diameterIndex] + "L" + lengthsArray[lengthIndex]);
        // Load a screw model with the desired dimensions
        GameObject screwItem = Resources.Load(screwPath) as GameObject;
        // Make it child of the screwParentTransform
        GameObject screw_clone = GameObject.Instantiate(screwItem, screwParentTransform) as GameObject;
        // Add the screw information to the new GameObject. This information is stored in the class ModelInfo
        ModelInfo screw_MI = AddInfoToModel(screw_clone, screwNumber, diametersArray[diameterIndex], lengthsArray[lengthIndex]);
        
        // Apply the required transform to the new model
        (screw_MI._gameObject).transform.localPosition = localPosition;
        (screw_MI._gameObject).transform.localRotation = Quaternion.Euler(localRotation);
        //(screw_MI._gameObject).transform.localScale = spineGO.transform.localScale;

        // Make the object manipulable
        ModifyScrew(screw_MI._gameObject, true);
        // Set color attribute to screw_MI
        Color modelColor = screw_MI._gameObject.GetComponentInChildren<MeshRenderer>().material.color;
        screw_MI._color = modelColor[0].ToString() + "," + modelColor[1].ToString() + "," + modelColor[2].ToString();
        // Add this screw to the list of elements that will be sent to 3D Slicer
        infoToSendArray.Add((screw_MI));
        return screw_MI;
    }

    // ModelInfo class. Every model sent to 3D Slicer should belong to this class. It includes information on the model number, 
    // name in the local storage and dimensions. The spine and image models also belong to this class. The fields that don't apply 
    // to them have to be also filled (i.e. "0"), but 3D Slicer won't read them
    public ModelInfo AddInfoToModel(GameObject modelGO, int modelNumber, string modelDiameter, string modelLength)
    {
        // Get the ModelInfo class from modelGO
        ModelInfo model_InspectorInfo = modelGO.GetComponent<ModelInfo>();
        // If modelGO doesn't have ModelInfo class, add it
        if (model_InspectorInfo == null)
        {
            model_InspectorInfo = modelGO.AddComponent<ModelInfo>();
        }
        // Add the info to the ModelInfo class
        string modelName = "Screw-" + modelNumber;
        model_InspectorInfo._name = modelName;
        model_InspectorInfo._number = modelNumber;
        model_InspectorInfo._diameter = modelDiameter;
        model_InspectorInfo._length = modelLength;
        model_InspectorInfo._gameObject = modelGO;
        (model_InspectorInfo._gameObject).name = modelName;
                
        return model_InspectorInfo;
    }

    // Delete the screw selected
    void DeleteScrew(GameObject[] screwModelsArray)
    {
        int numberOfScrews = GameObject.FindGameObjectsWithTag("Screw").Length;
        GameObject screwGO = GameObject.Find("Screw-" + screwSelected).gameObject;
        ModelInfo screwMI = screwGO.GetComponent<ModelInfo>();
        
        // If there are screws in the scene, delete the selected one
        if (screwGO != null)
        {
            infoToSendArray.Remove(screwMI);
            Destroy(screwMI._gameObject);
            // Rename all the remaining screws so that they all have consecutive numbers
            // i.e. if we have Screw-1, Screw-2 and Screw-3 and we delete Screw-2, rename Screw-3 as Screw-2
            for (int i = screwSelected; i < numberOfScrews; i++)
            {
                if (screwModelsArray[i] != null) // Recall that arrays start with 0 index and our screws are named from 1
                                            // so in this loop we are looking for the next screw in the array and renaming it as the previous one.
                                            // Here everything corresponds to the same "i" variable but same variable = different meaning.
                {
                    //screwModelsArray[i].name = "Screw-" + i;
                    screwModelsArray[i].GetComponent<ModelInfo>()._number = i;
                    screwModelsArray[i].GetComponent<ModelInfo>()._name = "Screw-" + i;
                    screwModelsArray[i].GetComponent<ModelInfo>()._gameObject.name = "Screw-" + i;
                }
            }
        }
        else
        {
            Debug.Log("Screw not found");
        }
    }

    // Select the next screw in the list. If we are in the last one, reset the counter to 1 to select the first model.
    void NextScrew()
    {
        int numberOfScrews = GameObject.FindGameObjectsWithTag("Screw").Length;
        // If there is a screw selected, deselect it
        try
        {
            GameObject screw_GO = GameObject.Find("Screw-" + screwSelected).gameObject;
            SelectThisScrew(screw_GO, false);
        }
        catch{}
        
        // Select next screw in the scene
        if (screwSelected < numberOfScrews)
        {
            screwSelected ++;
        }
        else
        {
            screwSelected = 1;
        }
        
        GameObject screwGO = GameObject.Find("Screw-" + screwSelected).gameObject; // Find this screw in the scene  
        ModelInfo screwMI = screwGO.GetComponent<ModelInfo>(); // Access the model information
        SelectThisScrew(screwMI._gameObject, true); // Highlight this screw
        screwSelected_label.text =  "Screw " + screwMI._number+ ":\nD" + screwMI._diameter + "L" + screwMI._length; // Update the screw label in the UI
    }

    // Change the diameter of the screw to the next possible in the list
    void ChangeScrewDiameter()
    {
        // Find screw to modify
        GameObject screwGO = GameObject.Find("Screw-" + screwSelected).gameObject;
        ModelInfo screwMI = screwGO.GetComponent<ModelInfo>();
        // Get its local position and rotation, and check if it's modifiable
        Vector3 localPosition = (screwMI._gameObject).transform.localPosition;
        Vector3 localRotation = (screwMI._gameObject).transform.localRotation.eulerAngles;
        bool modifiable = (screwMI._gameObject).GetComponent<BoxCollider>().enabled;
        // If it's not modifiable, do nothing. If it is, continue
        if (modifiable)
        {
            // Destroy current screw and delete it from infoToSendArray
            infoToSendArray.Remove(screwMI);
            Destroy(screwMI._gameObject);
            // Find next diameter
            NextDiameter();
            // Create new screw with new diameter
            ModelInfo newScrew = CreateScrew(screwSelected, modelsParentTransform, spineModel, localPosition, localRotation);
            // Make the new screw also modifiable
            ModifyScrew(newScrew._gameObject, modifiable);
            // Update emission mark
            SelectThisScrew(newScrew._gameObject, true);
            // Update screw label in screen       
            screwSelected_label.text = "Screw " + newScrew._number+ ":\nD" + newScrew._diameter + "L" + newScrew._length;
        }
        else
        {
            Debug.Log("This screw is not modifiable");
        }
        
    }

    // Iterate over the diameters array
    void NextDiameter()
    {
        if (diameterIndex < diametersArray.Length - 1)
        {
            diameterIndex ++;
        }
        else
        {
            diameterIndex = 0;
        }
    }

    // Change the length of the screw to the next possible in the list
    void ChangeScrewLength()
    {
        // Find screw to modify
        GameObject screwGO = GameObject.Find("Screw-" + screwSelected).gameObject;
        ModelInfo screwMI = screwGO.GetComponent<ModelInfo>();
        // Get its local position and rotation, and check if it's modifiable
        Vector3 localPosition = (screwMI._gameObject).transform.localPosition;
        Vector3 localRotation = (screwMI._gameObject).transform.localRotation.eulerAngles;
        bool modifiable = (screwMI._gameObject).GetComponent<BoxCollider>().enabled;
        // If it's not modifiable, do nothing. If it is, continue
        if (modifiable)
        {
            // Destroy current screw and delete it from infoToSendArray
            infoToSendArray.Remove(screwMI);
            Destroy(screwMI._gameObject);
            // Find next length
            NextLength();
            // Create new screw with new length
            ModelInfo newScrew = CreateScrew(screwSelected, modelsParentTransform, spineModel, localPosition, localRotation);
            // Make the new screw also modifiable
            ModifyScrew(newScrew._gameObject, modifiable);
            // Update emission mark
            SelectThisScrew(newScrew._gameObject, true);
            // Update screw label in screen       
            screwSelected_label.text = "Screw " + newScrew._number+ ":\nD" + newScrew._diameter + "L" + newScrew._length;
        }
        else
        {
            Debug.Log("This screw is not modifiable");
        }
    }

    // Iterate over the lengths array
    void NextLength()
    {
        if (lengthIndex < lengthsArray.Length - 1)
        {
            
            lengthIndex ++;
        }
        else
        {
            lengthIndex = 0;
        }
        
    }

    // Create an emission mark in the screw selected to easily identify it in the 3D world
    void SelectThisScrew(GameObject screwGO, bool selectedBool)
    {
        if (selectedBool)
        {
            screwGO.GetComponentInChildren<MeshRenderer>().material.EnableKeyword("_EMISSION");
        }
        else
        {
            screwGO.GetComponentInChildren<MeshRenderer>().material.DisableKeyword("_EMISSION");
        }
    }

    void OnApplicationQuit()
    {
        gltfModels.Clear();
    }
}
