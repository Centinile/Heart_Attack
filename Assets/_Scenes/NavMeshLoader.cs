using System.Collections;
using NavMeshPlus.Components;
using UnityEngine;

public class NavMeshLoader : MonoBehaviour
{
    public static NavMeshLoader Instance { get; private set; }

    [SerializeField] private NavMeshSurface surface;
    public bool IsReady { get; private set; } = false;

    private void Awake()
    {
        Instance = this;
    }

    private IEnumerator Start()
    {
        // Wait for LateUpdate so Physics2D colliders are synced
        yield return new WaitForEndOfFrame();
        Physics2D.SyncTransforms();

        // AsyncOperation can be yielded directly in a coroutine
        yield return surface.BuildNavMeshAsync();

        IsReady = true;
        Debug.Log("[NavMeshLoader] NavMesh built and ready.");
    }
}