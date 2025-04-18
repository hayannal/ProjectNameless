﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlendShape : MonoBehaviour
{
    public bool autoSwitch = false;
    [Range(1.0f, 10.0f)]
    public float duration = 3.0f;
    float lastDuration = 3.0f;

    int blendShapeCount;
    int currentID;
    int currentWeight = 0;
    SkinnedMeshRenderer skinnedMeshRenderer;
    Mesh skinnedMesh;
    float BlinkAmount = 0;
    bool isBlinking;
    int switchSpeed = 4;
    void Awake()
    {
        skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
        skinnedMesh = GetComponent<SkinnedMeshRenderer>().sharedMesh;
    }

    void Start()
    {
        blendShapeCount = skinnedMesh.blendShapeCount;
        InvokeRepeating("blink",1.0f,3.0f);
    }

    void Update()
    {
        if (isBlinking == true)
        {
            skinnedMeshRenderer.SetBlendShapeWeight(14, BlinkAmount);
            BlinkAmount = BlinkAmount + 25;

            if (BlinkAmount > 180)
            {
                BlinkAmount = 0;
                skinnedMeshRenderer.SetBlendShapeWeight(14, BlinkAmount);
                isBlinking = false;
            }

        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            skinnedMeshRenderer.SetBlendShapeWeight(currentID, 0);
            currentID = (int)Random.Range(0, blendShapeCount);
            skinnedMeshRenderer.SetBlendShapeWeight(currentID, 100);
        }

        lastDuration -= Time.deltaTime;
        if (lastDuration <= 0.0f)
        {
            lastDuration = duration;
            if (autoSwitch) swithBlendShape();
        }

        currentWeight = (int)(100.0 * (1.0 - (lastDuration / duration))) * switchSpeed;
        if(currentWeight<=100)
            skinnedMeshRenderer.SetBlendShapeWeight(currentID, currentWeight);
    }

    void swithBlendShape()
    {
        //Debug.Log("switchBlendShape");
        currentWeight = 0;
        skinnedMeshRenderer.SetBlendShapeWeight(currentID, currentWeight);
        currentID = (int)Random.Range(0, blendShapeCount);
        skinnedMeshRenderer.SetBlendShapeWeight(currentID, currentWeight);
    }

    void blink()
    {
        // while (isPlaying)
        // {
            //float waitTime = Random.Range(1, 5);
            //yield return new WaitForSeconds(waitTime);
            isBlinking = true;

        }

    // }
}