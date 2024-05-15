using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;
using System.IO;

[CustomEditor(typeof(PressableButtons))]
public class PressableButtonsEditor : Editor
{
    private GUIStyle redLabelStyle;
    private bool modelsFetched = false;
    private string previousPatientID = null;
    private bool checkAll = false; // Add a variable to track the check all state

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

        serializedObject.ApplyModifiedProperties();

        if (previousPatientID != script.patientID)
        {
            previousPatientID = script.patientID;
            PopulateModelList(script);
        }
    }

    private void InitializeStyles()
    {
        redLabelStyle = new GUIStyle(EditorStyles.label)
        {
            normal = { textColor = Color.red }
        };
    }

    private void PatientSelectionMenu(PressableButtons script)
    {
        EditorGUILayout.LabelField("Select Patient", EditorStyles.boldLabel);

        if (GUILayout.Button("Refresh Patient List"))
        {
            PopulatePatientIDs(script);
            modelsFetched = false;
        }

        if (script.patientIDs != null && script.patientIDs.Count > 0)
        {
            int selectedIndex = EditorGUILayout.Popup("Selected Patient:", script.patientIDs.IndexOf(script.patientID), script.patientIDs.ToArray());
            if (selectedIndex >= 0 && selectedIndex < script.patientIDs.Count)
            {
                string newlySelectedPatientID = script.patientIDs[selectedIndex];
                if (script.patientID != newlySelectedPatientID)
                {
                    script.target_models.Clear();
                    script.patientID = newlySelectedPatientID;
                }
            }
        }
        else
        {
            EditorGUILayout.LabelField("Selected Patient:", "No patient folders found", redLabelStyle);
        }
    }

    private void ParentModelMenu(PressableButtons script)
    {
        EditorGUILayout.LabelField("Select Parent Model", EditorStyles.boldLabel);

        if (GUILayout.Button(new GUIContent("Refresh Model List", "Selected model will be used for land marking")))
        {
            PopulateModelList(script);
        }

        if (modelsFetched && script.patientModels != null && script.patientModels.Length > 0)
        {
            int selectedIndex = 0;
            for (int i = 0; i < script.patientModels.Length; i++)
            {
                if (script.patientModels[i].ToLower().Contains("skin"))
                {
                    selectedIndex = i;
                    break;
                }
            }

            int index = Array.IndexOf(script.patientModels, script.parentModel);
            if (index != -1)
            {
                selectedIndex = index;
            }

            selectedIndex = EditorGUILayout.Popup("Parent Model:", selectedIndex, script.patientModels);
            script.parentModel = script.patientModels[selectedIndex];

            ModelImporter.parentModel = script.parentModel;
        }
        else
        {
            EditorGUILayout.LabelField("Parent Model:", "No models found", redLabelStyle);
        }
    }

    private void TargetsModelMenu(PressableButtons script)
    {
        EditorGUILayout.LabelField("Select Target/Targets from Patient Models", EditorStyles.boldLabel);

        if (script.patientModels == null || script.patientModels.Length == 0)
        {
            EditorGUILayout.LabelField(" ", "No models found", redLabelStyle);
            return;
        }

        // Add Check All/Uncheck All button
        bool newCheckAll = GUILayout.Toggle(checkAll, "Check All");
        if (newCheckAll != checkAll)
        {
            checkAll = newCheckAll;
            if (checkAll)
            {
                foreach (var model in script.patientModels)
                {
                    if (model != script.parentModel && !script.target_models.Contains(model))
                    {
                        script.target_models.Add(model);
                    }
                }
            }
            else
            {
                script.target_models.Clear();
            }
        }

        for (int i = 0; i < script.patientModels.Length; i++)
        {
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

    private void PopulatePatientIDs(PressableButtons script)
    {
        script.patientIDs = PatientIDsFetcher.FetchPatientIDs();
        EditorUtility.SetDirty(script);
    }

    private void PopulateModelList(PressableButtons script)
    {
        string patientID = script.patientID;
        script.patientModels = PatientModelsFetcher.FetchPatientModels(patientID);
        modelsFetched = true;
        EditorUtility.SetDirty(script);
    }

    private void CreatePrefabs(PressableButtons script)
    {
        string patientID = script.patientID;
        string[] patientModels = script.patientModels;
        string prefabFolderPath = Path.Combine("Assets", "Resources", "Prefabs", "SpinePrefabs", patientID);
        string modelFolderPath;

        script.gltfModels.Clear();

        if (string.IsNullOrEmpty(patientID))
        {
            Debug.LogError("Please select a patient first.");
            return;
        }

        if (patientModels == null || patientModels.Length == 0)
        {
            Debug.LogError($"No models were found in {patientID}'s folder!!!");
            return;
        }

        DeleteOldPrefabs(prefabFolderPath);
        modelFolderPath = PatientModelsFetcher.GetModelsPath(patientID);

        string[] files = Directory.GetFiles(modelFolderPath, "*.gltf");

        if (files.Length > 0)
        {
            Debug.Log("Will create prefab at runtime.");
        }
        else
        {
            ModelImporter.CreatePrefabsFromOBJ(patientID, patientModels);
        }

        EditorUtility.DisplayDialog("Prefab Creation", "Prefabs have been successfully created!", "OK");
        EditorUtility.SetDirty(script);
    }

    private void DeleteOldPrefabs(string prefabFolderPath)
    {
        if (Directory.Exists(prefabFolderPath))
        {
            string[] prefabs = Directory.GetFiles(prefabFolderPath);

            foreach (string prefab in prefabs)
            {
                File.Delete(prefab);
            }

            Debug.Log("Old Prefabs have been deleted");
        }
    }
}

