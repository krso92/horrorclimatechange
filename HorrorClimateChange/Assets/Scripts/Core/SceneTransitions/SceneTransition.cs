using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneTransition : MonoBehaviour
{
    public enum PlaceToGo
    {
        None,
        PreviousScene,
        NextScene,
        SpecialRoom
    }

    [SerializeField]
    private PlaceToGo placeToGo;

    private void OnTriggerEnter(Collider other)
    {
        if (placeToGo == PlaceToGo.NextScene)
        {
            SceneManage.Instance.LoadNextGameplayScene();
        }
        else if (placeToGo == PlaceToGo.PreviousScene)
        {
            Debug.Log("No turning back!");
        }
        else if (placeToGo == PlaceToGo.SpecialRoom)
        {
            Debug.Log("Special room not implemented!");
        }
    }
}
