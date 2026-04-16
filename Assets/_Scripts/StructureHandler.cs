using UnityEngine;

public class StructureHandler : MonoBehaviour
{
    // Change 'ScriptableObject' to 'StructureData'
    public StructureData data;

    void OnMouseDown()
    {
        if (UIManager.instance != null)
        {
            UIManager.instance.DisplayInfoSO(data);
        }
    }
}