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

    public float durationBurn = .7f;

    bool doShake;
    float shakeSpeed = 90f;
    float shakeAmount = 0.1f;
    float x;
    float y;

    public float rayRadius;
    public float rayDistance;

    Vector3 startingPos;

    // Start is called before the first frame update
    void Start()
    {
        flashLight.color = normColor;
        flashLight.intensity = normIntensity;
        flashLight.range = normRange;
        flashLight.spotAngle = normAngle;
        startingPos = gameObject.transform.position;
    }

    public void BurnLight()
    {
        flashLight.DOColor(burnColor, durationBurn);
        flashLight.DOIntensity(burnIntensity, durationBurn);
        DOTween.To(() => flashLight.range, x => flashLight.range = x, burnRange, durationBurn);
        DOTween.To(() => flashLight.spotAngle, x => flashLight.spotAngle = x, burnAngle, durationBurn).OnComplete(StartBurning);

        //DOTween.To()
        //flashLight.color = burnColor;

    }

    public void NormalLight()
    {
        doShake = false;
        flashLight.DOColor(normColor, durationBurn);
        flashLight.DOIntensity(normIntensity, durationBurn);
        DOTween.To(() => flashLight.range, x => flashLight.range = x, normRange, durationBurn);

        DOTween.To(() => flashLight.spotAngle, x => flashLight.spotAngle = x, normAngle, durationBurn);

    }

    void StartBurning() 
    {
        doShake = true;   
    }

    //IEnumerator CheckForTarget() 
    //{

    //}

    //GameObject[] ReturnTargets()
    //{
    //    Ray[] rays;
    //    RaycastHit[] hits;
    //    hits = Physics.SphereCastAll(transform.position, rayRadius, transform.forward, rayDistance);



    //}



    private void Update()
    {
        if (doShake)
        {

            x = startingPos.x + Mathf.Sin(Time.time * shakeSpeed * Random.Range(0f, 1f)) * shakeAmount;

            y = startingPos.y + (Mathf.Sin(Time.time * shakeSpeed * Random.Range(0f,1f)) * shakeAmount);
            gameObject.transform.localPosition = new Vector3(x, transform.localPosition.y, transform.localPosition.z);
        }
    }
}
