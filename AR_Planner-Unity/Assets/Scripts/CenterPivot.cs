using UnityEngine;

public class CenterPivot : MonoBehaviour
{
    public void CenterModelPivot()
    {
        // Assuming this script is attached to the empty parent GameObject
        GameObject parentObject = this.gameObject;

        // Find the model inside the parent object
        MeshRenderer[] meshRenderers = parentObject.GetComponentsInChildren<MeshRenderer>();

        if (meshRenderers.Length == 0)
        {
            Debug.LogError("No MeshRenderer found in the child objects.");
            return;
        }

        // Calculate the bounds of the model
        Bounds modelBounds = CalculateBounds(meshRenderers);

        // Adjust the pivot by moving the model within the parent
        foreach (MeshRenderer meshRenderer in meshRenderers)
        {
            meshRenderer.transform.localPosition -= modelBounds.center;
        }
    }

    Bounds CalculateBounds(MeshRenderer[] meshRenderers)
    {
        Bounds bounds = meshRenderers[0].bounds;

        // Encapsulate all MeshRenderers' bounds
        foreach (MeshRenderer meshRenderer in meshRenderers)
        {
            bounds.Encapsulate(meshRenderer.bounds);
        }

        // Convert world bounds center to local bounds center
        bounds.center = meshRenderers[0].transform.InverseTransformPoint(bounds.center);

        return bounds;
    }
}
