using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

public class PatientModelsFetcher : MonoBehaviour
{
    public static string[] FetchPatientModels(string patientID)
    {
        string modelsPath;
        string[] patientModels; //This list will hold name of all model files in the PatientID folder

      
        modelsPath = GetModelsPath(patientID);

        patientModels = Directory.GetFiles(modelsPath, "*.obj");
        patientModels = patientModels.Select(model => Path.GetFileNameWithoutExtension(model)).Distinct().ToArray();

        foreach (string file in patientModels)
        {
            Debug.Log(file);
            Debug.Log(patientModels.Length);
        }

        return patientModels;
    }

    internal static string GetModelsPath(string patientID)
    {
        string modelsPath;
        string modelsFolderName = patientID;

        modelsPath = Path.Combine(PatientIDsFetcher.patientIDsPath, modelsFolderName);

        Debug.Log(modelsPath);

        return modelsPath;
    }
}
