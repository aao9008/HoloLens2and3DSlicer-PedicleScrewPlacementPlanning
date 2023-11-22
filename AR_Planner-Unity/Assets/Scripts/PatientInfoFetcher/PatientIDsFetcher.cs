// First import libraries of interest
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

// Define the class PatientNumberFetcher
public class PatientIDsFetcher : MonoBehaviour
{
    internal static string patientIDsPath = Path.Combine("Assets", "Resources", "Models", "SpineModels"); // File Path to patient folders

    // Static method to fetch patient folder names 
    public static List<string> FetchPatientIDs()
    {
        List<string> patientIDs = new List<string>(); // This list will hold all patient files
        

        // Search for folders in the patientIDs path, will return all folders in given path along with the children of these folders.
        string[] folderGUIDs = AssetDatabase.FindAssets("t:Folder", new string[] { patientIDsPath }); // Search Unity asset database for patientID folders

        // Iterate over the folder GUIDs
        foreach (string guid in folderGUIDs)
        {
            string folderPath = AssetDatabase.GUIDToAssetPath(guid); // Get the folder path of each GUID

            // Add only folders who are direct childeren of the SpineModels path
            if (Path.GetDirectoryName(folderPath) == patientIDsPath)
            {
                string folderName = Path.GetFileName(folderPath); // Get name of the folder at the end of the folder path

                patientIDs.Add(folderName); // Add folder name to patientIDs list
            }
        }

        
       

        // Return completed list
        return patientIDs;
    }
}
