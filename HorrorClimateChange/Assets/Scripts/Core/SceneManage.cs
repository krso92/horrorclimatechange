using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneManage: TGlobalSingleton<SceneManage>
{

    private Scene activeScene;


    public void LoadSceneString(string scene, float waitTime = 0f)
    {
        StartCoroutine(LoadScene(scene, waitTime));
    }

    public void LoadSceneInt(int scene, float waitTime = 0f)
    {
        StartCoroutine(LoadScene(scene, waitTime));
    }
    public void UnloadGameScene()
    {
        StartCoroutine(UnloadScene());
    }

    private IEnumerator LoadScene(int i, float waitTime = 0f)
    {
        yield return new WaitForSeconds(waitTime);
        AsyncOperation async = new AsyncOperation();
        async = SceneManager.LoadSceneAsync(i, LoadSceneMode.Additive);
        while (!async.isDone)
        {
            yield return null;
        }
        activeScene = SceneManager.GetSceneByBuildIndex(i);
        SceneManager.SetActiveScene(activeScene);
    }

    private IEnumerator LoadScene(string i, float waitTime = 0f)
    {
        yield return new WaitForSeconds(waitTime);
        AsyncOperation async = new AsyncOperation();
        async = SceneManager.LoadSceneAsync(i, LoadSceneMode.Additive);
        while (!async.isDone)
        {
            yield return null;
        }
        activeScene = SceneManager.GetSceneByName(i);
        SceneManager.SetActiveScene(activeScene);
    }

    public IEnumerator UnloadScene()
    {
        if (activeScene.name != "")
        {
            AsyncOperation async = new AsyncOperation();
            int i = activeScene.buildIndex;
            async = SceneManager.UnloadSceneAsync(i);
            while (!async.isDone)
            {
                yield return null;
            }
            activeScene = SceneManager.GetSceneByBuildIndex(0);
            SceneManager.SetActiveScene(activeScene);
        }
    }
}
