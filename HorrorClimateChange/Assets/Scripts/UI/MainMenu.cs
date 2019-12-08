using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class MainMenu : MonoBehaviour
{
    [SerializeField]
    private CameraFilterPack_Distortion_BlackHole distortion;

    public void PlayGame()
    {
        SceneManage.Instance.LoadGameplay();
    }

    private void Start()
    {
        DOTween.To(() => distortion.Size, x => distortion.Size = x, 1f, 2f)
            .SetEase(Ease.InOutBounce)
            .SetLoops(-1, LoopType.Yoyo);
    }
}
