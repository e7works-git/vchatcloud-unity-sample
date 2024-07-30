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
            // �� ���� �� ȣ��Ǵ� �̺�Ʈ�� �ڵ鷯 �߰�
            SceneManager.sceneLoaded += OnSceneLoaded;
            isInitialized = true;

            // ���� ���� MainThreadExecutor ������ �߰�
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
