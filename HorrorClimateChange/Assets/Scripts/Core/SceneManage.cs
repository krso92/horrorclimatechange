using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneManage: TGlobalSingleton<SceneManage>
{
    private Scene activeScene;

    private Scene baseScene;

    private const int GAMEPLAY_SCENES_FROM = 2;

    private const int BASE_SCENE = 1;

    public void LoadSceneString(string scene, float waitTime = 0f)
    {
        StartCoroutine(LoadScene(scene, waitTime));
    }

    public void LoadSceneInt(int scene, float waitTime = 0f, LoadSceneMode mode = LoadSceneMode.Additive)
    {
        StartCoroutine(LoadScene(scene, waitTime, mode));
    }
    public void UnloadGameScene()
    {
        StartCoroutine(UnloadScene());
    }

    private IEnumerator LoadScene(int i, float waitTime = 0f, LoadSceneMode mode = LoadSceneMode.Additive)
    {
        yield return new WaitForSeconds(waitTime);
        AsyncOperation async = new AsyncOperation();
        async = SceneManager.LoadSceneAsync(i, mode);
        while (!async.isDone)
        {
            yield return null;
        }
        activeScene = SceneManager.GetSceneByBuildIndex(i);
        if (i == BASE_SCENE)
        {
            baseScene = activeScene;
        }
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
        if (activeScene.buildIndex == BASE_SCENE)
        {
            baseScene = activeScene;   
        }
        SceneManager.SetActiveScene(activeScene);
    }

    private IEnumerator UnloadScene()
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
            activeScene = baseScene;
            SceneManager.SetActiveScene(activeScene);
        }
    }

    // use this
    public void LoadNextGameplayScene(float wait = 0f)
    {
        StartCoroutine(NextScene(wait));
    }

    private IEnumerator NextScene(float wait)
    {
        int nextIndex = activeScene.buildIndex + 1;
        yield return StartCoroutine(UnloadScene());
        yield return StartCoroutine(LoadScene(nextIndex, wait));
    }

    // and this
    public void LoadGameplay()
    {
        // TODO
        // init gameplay scene with lightning
        // transitions
        StartCoroutine(Gameplay());
    }

    private IEnumerator Gameplay()
    {
        int index = GAMEPLAY_SCENES_FROM;
        yield return StartCoroutine(LoadScene(BASE_SCENE, 0f, LoadSceneMode.Single));
        // will be additive
        yield return StartCoroutine(LoadScene(index));
    }
}
