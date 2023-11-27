using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(PressableButtons))]
public class PressableButtonsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        PressableButtons script = (PressableButtons)target;

        // Display a button in the Inspector to trigger the method
        if (GUILayout.Button("Refresh Patient List"))
        {
            PopulatePatientIDs(script);
        }

        // Display a dropdown for selecting the Patient ID in the Inspector
        if (script.patientIDs != null && script.patientIDs.Count > 0)
        {
            int selectedIndex = EditorGUILayout.Popup("Patient ID", script.patientIDs.IndexOf(script.patientID), script.patientIDs.ToArray());
            if (selectedIndex >= 0 && selectedIndex < script.patientIDs.Count)
            {
                script.patientID = script.patientIDs[selectedIndex];
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.Space();

        if (GUILayout.Button("Create Prefabs"))
        {
            CreatePrefabs(script);
        }

        // Update the Inspector UI
        serializedObject.ApplyModifiedProperties();
    }

    // Method to populate patientIDs list
    private void PopulatePatientIDs(PressableButtons script)
    {
        // Example: Populate patientIDs list
        script.patientIDs = PatientIDsFetcher.FetchPatientIDs();
       
        // Refresh the Inspector to reflect changes
        EditorUtility.SetDirty(script);
    }

    // Method to automatically create prefabs from OBJ models in a patients folder
    private void CreatePrefabs(PressableButtons script)
    {
        // Get the patientID from the PressableButtons script.
        string patientID = script.patientID;

        Debug.Log("The button says we have " + patientID);

        // If not patietn is selected, notify the user
        if (patientID == null)
        {
            Debug.LogError("Please select a patient first.");
            return;
        }

        // Get an array of patient model names. 
        string[]patientModels = PatientModelsFetcher.FetchPatientModels(patientID);

        // Dispaly error message and exit function if no models are found.
        if (patientModels.Length == 0)
        {
            Debug.LogError("No models were found in " + patientID + "'s folder!!!");
            return;
        }

        // Iterate over models list and crate a prefab
        foreach(string model in patientModels)
        {
            ModelImporter.CreatePrefab(patientID, model);
        }

        Debug.Log("Prefabs have been successfully created!");

        foreach(string model in patientModels)
        {
            Debug.Log("Model Name: " + model);
        }

        // Refresh the Inspector to reflect changes
        EditorUtility.SetDirty(script);
    }
}
