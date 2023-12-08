using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;
using System.IO;

[CustomEditor(typeof(PressableButtons))]
public class PressableButtonsEditor : Editor
{
    private GUIStyle redLabelStyle;

    public override void OnInspectorGUI()
    {
        if (redLabelStyle == null)
        {
            InitializeStyles();
        }

        PressableButtons script = (PressableButtons)target;
        serializedObject.Update();

        PatientSelectionMenu(script);

        EditorGUILayout.Space();
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        ParentModelMenu(script);

        EditorGUILayout.Space();
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        TargetsModelMenu(script);

        EditorGUILayout.Space();
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        CreatePrefabButton(script);
       
        // Update the Inspector UI
        serializedObject.ApplyModifiedProperties();
    }

    ///////// Text Style Method //////
    private void InitializeStyles()
    {
        redLabelStyle = new GUIStyle(EditorStyles.label);
        redLabelStyle.normal.textColor = Color.red;
    }

    ///////// GUI Button Widgets//////
    private void PatientSelectionMenu(PressableButtons script)
    {
        EditorGUILayout.LabelField("Select Patient", EditorStyles.boldLabel);

        // Display a button in the Inspector to trigger the method
        if (GUILayout.Button("Refresh Patient List"))
        {
            PopulatePatientIDs(script);
        }

        // Display a dropdown for selecting the Patient ID in the Inspector
        if (script.patientIDs != null && script.patientIDs.Count > 0)
        {
            int selectedIndex = EditorGUILayout.Popup("Selected Patient:", script.patientIDs.IndexOf(script.patientID), script.patientIDs.ToArray());
            if (selectedIndex >= 0 && selectedIndex < script.patientIDs.Count)
            {
                string newlySelectedPatientID = script.patientIDs[selectedIndex];

                if (script.patientID != newlySelectedPatientID)
                {
                    //Update the previously selected patient ID
                    //previouslySelectedPatientID = newlySelectedPatientID;

                    // Reset Toggle Menu
                    script.target_models.Clear();
                }

                script.patientID = newlySelectedPatientID;

                // Automatically refresh model list
                PopulateModelList(script);
            }
        }
        else if (script.patientIDs == null || script.patientIDs.Count == 0)
        {
            EditorGUILayout.LabelField("Selected Patient:", "No patient folders found", redLabelStyle);
        }
    }

    private void ParentModelMenu(PressableButtons script)
    {
        EditorGUILayout.LabelField("Select Parent Model", EditorStyles.boldLabel);
        // Display a button in the Inspector to trigger the method
        if (GUILayout.Button(new GUIContent("Refresh Model List", "Selected model will be used for land marking")))
        {
            PopulateModelList(script);
        }

        if (script.patientModels != null && script.patientModels.Length > 0)
        {
            int selectedIndex = 0; // Initialize to the first index by default

            // Find the index of a model containing the word "skin" in script.patientModels
            for (int i = 0; i < script.patientModels.Length; i++)
            {
                if (script.patientModels[i].ToLower().Contains("skin"))
                {
                    selectedIndex = i;
                    break; // Stop searching after finding the first model containing "skin"
                }
            }

            // Find the index of script.parentModel in script.patientModels
            int index = Array.IndexOf(script.patientModels, script.parentModel);
            if (index != -1)
            {
                selectedIndex = index;
            }


            selectedIndex = EditorGUILayout.Popup("Parent Model:", selectedIndex, script.patientModels);
            script.parentModel = script.patientModels[selectedIndex];

            // Send parent model name to ModelImporter class
            // Parent model prefabs get processed differently from child models. 
            ModelImporter.parentModel = script.parentModel;
        }
        else
        {

            EditorGUILayout.LabelField("Parent Model:", "No models found", redLabelStyle);

        }
    }

    private void TargetsModelMenu(PressableButtons script)
    {
        // Display the patientModels array in Inspector
        EditorGUILayout.LabelField("Select Target/Targets from Patient Models", EditorStyles.boldLabel);

        if (script.patientModels.Length == 0 || script.patientModels == null)
        {
            EditorGUILayout.LabelField(" ", "No models found", redLabelStyle);
        }

        for (int i = 0; i < script.patientModels.Length; i++)
        {
            // Skip rendering the parentModel in the list
            if (script.patientModels[i] == script.parentModel)
            {
                continue;
            }

            bool isSelected = script.target_models.Contains(script.patientModels[i]);
            bool newIsSelected = EditorGUILayout.Toggle(script.patientModels[i], isSelected);

            if (newIsSelected != isSelected)
            {
                if (newIsSelected)
                {
                    script.target_models.Add(script.patientModels[i]);
                }
                else
                {
                    script.target_models.Remove(script.patientModels[i]);
                }
            }
        }
    }

    private void CreatePrefabButton(PressableButtons script)
    {
        EditorGUILayout.LabelField("Prefab Creation", EditorStyles.boldLabel);

        if (GUILayout.Button("Create Prefabs"))
        {
            CreatePrefabs(script);
        }
    }

    ///////// Button Logic //////////
    
    // Method to populate patientIDs list
    private void PopulatePatientIDs(PressableButtons script)
    {
        // Example: Populate patientIDs list
        script.patientIDs = PatientIDsFetcher.FetchPatientIDs();

        // Refresh the Inspector to reflect changes
        EditorUtility.SetDirty(script);
    }

    // Method to populate patient models list
    private void PopulateModelList(PressableButtons script)
    {
        string patientID = script.patientID;

        // Populate list of models 
        script.patientModels = PatientModelsFetcher.FetchPatientModels(patientID); // Get an array of patient model names.

        // Refresh the Inspector to reflect changes
        EditorUtility.SetDirty(script);
    }

    // Method to automatically create prefabs from OBJ models in a patients folder
    private void CreatePrefabs(PressableButtons script)
    {
        // Get the patientID from the PressableButtons script.
        string patientID = script.patientID;
        string[] patientModels = script.patientModels;
        string prefabFolderPath = Path.Combine("Assets", "Resources", "Prefabs", "SpinePrefabs", patientID);
        string modelFolderPath;

        //Clear the list
        script.gltfModels.Clear();

        Debug.Log(script.gltfModels.Count);

        // If not patietn is selected, notify the user
        if (patientID == null)
        {
            Debug.LogError("Please select a patient first.");
            return;
        }

        // Dispaly error message and exit function if no models are found.
        if (patientModels.Length == 0)
        {
            Debug.LogError("No models were found in " + patientID + "'s folder!!!");
            return;
        }

        // Delete old prefabs
        DeleteOldPrefabs(prefabFolderPath);

        // Are models form OBJ or GLTF file?
        modelFolderPath = PatientModelsFetcher.GetModelsPath(patientID);

        string[] files = Directory.GetFiles(modelFolderPath, "*.gltf");

        if (files.Length > 0)
        {
            script.gltfModels = ModelImporter.CreatePrefabsFromGLTF(patientID, files[0]);
        }
        else
        {
            ModelImporter.CreatePrefabsFromOBJ(patientID, patientModels);
        }

        EditorUtility.DisplayDialog("Prefab Creation", "Prefabs have been successfully created!", "OK");

        // Refresh the Inspector to reflect changes
        EditorUtility.SetDirty(script);
    }

    private void DeleteOldPrefabs(string prefabFolderPath)
    {
        if (Directory.Exists(prefabFolderPath))
        {
            string[] prefabs = Directory.GetFiles(prefabFolderPath);

            if (prefabs.Length > 0)
            {
                foreach (string prefab in prefabs)
                {
                    File.Delete(prefab);
                }

                Debug.Log("Old Prefabs have been deleted");
            }

            //AssetDatabase.Refresh();

        }
    }
}
