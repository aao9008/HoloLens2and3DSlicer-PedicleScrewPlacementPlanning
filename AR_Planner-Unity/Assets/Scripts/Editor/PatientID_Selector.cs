using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(PressableButtons))]
public class PatientIDSelector : Editor
{
    public override void OnInspectorGUI()
    {
        PressableButtons script = (PressableButtons)target;

        // Display a button in the Inspector to trigger the method
        if (GUILayout.Button("Populate Patient IDs"))
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
}
