using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    [Header("Loading Screen Settings")]
    [SerializeField] private string loadingSceneName = "LoadingScreen";
    private static string targetSceneName;

    // Removed static Instance, DontDestroyOnLoad, and Awake singleton logic

    /// <summary>
    /// Public method to switch scenes via loading screen.
    /// </summary>
    public void SceneChange(string sceneName)
    {
        targetSceneName = sceneName;
        SceneManager.LoadScene(loadingSceneName);  // Load loading screen first
        Time.timeScale = 1;
    }

    /// <summary>
    /// Load the target scene synchronously after preload (called by LoadingScreenManager).
    /// </summary>
    public void LoadTargetScene()
    {
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogError("SceneController: targetSceneName is null or empty!");
            return;
        }

        Debug.Log($"SceneController: Loading target scene '{targetSceneName}' synchronously.");
        SceneManager.LoadScene(targetSceneName);  // Synchronous load (unloads loading scene, loads game scene)
        Debug.Log($"SceneController: Target scene '{targetSceneName}' loaded.");
    }
}