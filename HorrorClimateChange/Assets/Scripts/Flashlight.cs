using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using DG.DemiLib;

public class Flashlight : MonoBehaviour
{
    public Light flashLight;
    public Color normColor;
    public Color burnColor;

    public float normRange;
    public float burnRange;
    public float normAngle;
    public float burnAngle;
    public float burnIntensity;
    public float normIntensity;

    // Start is called before the first frame update
    void Start()
    {
        flashLight.color = normColor;
        flashLight.intensity = normIntensity;
        flashLight.range = normRange;
        flashLight.spotAngle = normAngle;
    }

    public void BurnLight()
    {
        flashLight.DOColor(burnColor, .7f);
        flashLight.DOIntensity(burnIntensity, .7f);
        DOTween.To(() => flashLight.range, x => flashLight.range = x, burnRange, .7f);
        DOTween.To(() => flashLight.spotAngle, x => flashLight.spotAngle = x, burnAngle, .7f);

        //DOTween.To()
        //flashLight.color = burnColor;

    }

    public void NormalLight()
    {
        flashLight.DOColor(normColor, .7f);
        flashLight.DOIntensity(normIntensity, .7f);
        DOTween.To(() => flashLight.range, x => flashLight.range = x, normRange, .7f);

        DOTween.To(() => flashLight.spotAngle, x => flashLight.spotAngle = x, normAngle, .7f);

    }
}
