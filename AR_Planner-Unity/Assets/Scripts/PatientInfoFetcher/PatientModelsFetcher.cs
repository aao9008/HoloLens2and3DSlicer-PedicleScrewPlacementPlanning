using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

public class PatientModelsFetcher : MonoBehaviour
{
    public static string[] FetchPatientModels(string patientID)
    {
        string modelsFolderPath; // This will hold path to a patients folder, each patient folder holds a patients OBJ models
        string[] patientModelsArray; //This array will hold name of all model files in the PatientID folder

      
        modelsFolderPath = GetModelsPath(patientID); // Returns full path to patients folder (EX: Assets/Models/Patient 01)

        patientModelsArray = Directory.GetFiles(modelsFolderPath, "*.obj");
        patientModelsArray = patientModelsArray.Select(model => Path.GetFileNameWithoutExtension(model)).Distinct().ToArray();
 
        return patientModelsArray;
    }

    internal static string GetModelsPath(string patientID)
    {
        string modelsFolderPath;
        string modelsFolderName = patientID;

        modelsFolderPath = Path.Combine(PatientIDsFetcher.patientIDsPath, modelsFolderName);

        return modelsFolderPath;
    }

    internal static string GetMTLPath(string patientID, string modelName)
    {
        string modelMTLPath;
        string modelsFolderName = patientID;

        modelMTLPath = Path.Combine(PatientIDsFetcher.patientIDsPath, modelsFolderName, modelName + ".mtl");

        Debug.Log(modelMTLPath);

        return modelMTLPath;
    }
}
