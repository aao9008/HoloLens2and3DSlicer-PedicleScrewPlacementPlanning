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

        if (patientModelsArray.Length < 1)
        {
           patientModelsArray = Directory.GetFiles(modelsFolderPath, "*.gltf");

            // Check if a GLTF file exists
            if (patientModelsArray.Length < 1)
            {
                return patientModelsArray;
            }

            string gltfPath = patientModelsArray[0];

            patientModelsArray = GetChildNamesFromGLTF(gltfPath);
        }

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

        return modelMTLPath;
    }

    private static string[] GetChildNamesFromGLTF(string gltfPath)
    {
        string jsonText = File.ReadAllText(gltfPath);

        // Parse the JSON text into an object
        GLTFData gltfData = JsonUtility.FromJson<GLTFData>(jsonText);

        // Get node names
        List<string> modelNames = GetModelNames(gltfData.nodes);

        // Convert list to array and return model names
        return modelNames.ToArray();
    }

    private static List<string> GetModelNames(List<GLTFNode> nodes)
    {
        List<string> modelNames = new List<string>();

        foreach (var node in nodes)
        {
            
             if (node.mesh != null)
            {
                modelNames.Add(node.name);
            }
 
        }

        return modelNames;
    }
}

[System.Serializable]
public class GLTFData
{
    public List<GLTFNode> nodes;
}

[System.Serializable]
public class GLTFNode
{
    public string mesh;
    public string name;
}

    
