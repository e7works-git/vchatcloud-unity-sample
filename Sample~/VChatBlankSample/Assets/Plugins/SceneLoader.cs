using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{

    [SerializeField]
    private GameObject mainThreadExecutorPrefab;

    private static bool isInitialized = false;

    void Awake()
    {
        if (!isInitialized)
        {
            // 씬 변경 시 호출되는 이벤트에 핸들러 추가
            SceneManager.sceneLoaded += OnSceneLoaded;
            isInitialized = true;

            // 현재 씬에 MainThreadExecutor 프리팹 추가
            AddMainThreadExecutorIfNeeded();
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        AddMainThreadExecutorIfNeeded();
    }

    private void AddMainThreadExecutorIfNeeded()
    {
        if (!UnityMainThreadDispatcher.Exists())
        {
            Instantiate(mainThreadExecutorPrefab);
        }
    }

    void OnDestroy()
    {
        if (isInitialized)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            isInitialized = false;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
