﻿//========= Copyright 2016, Enflux Inc. All rights reserved. ===========
//
// Purpose: Upperbody mapping and operation of EnfluxVR suit
//
//======================================================================

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class EVRUpperLimbMap : EVRHumanoidLimbMap, ILimbAnimator
{
    private JointRotations jointRotations = new JointRotations();
    private float[] initCore = new float[] { 0, 0, 0 };
    private float[] initLeftUpper = new float[] { 0, 0, 0 };
    private float[] initLeftFore = new float[] { 0, 0, 0 };
    private float[] initRightUpper = new float[] { 0, 0, 0 };
    private float[] initRightFore = new float[] { 0, 0, 0 };
    private Quaternion chain;
    private Quaternion initHeadPose = new Quaternion();
    private Quaternion initCorePose = new Quaternion();
    private Queue<Quaternion> corePose = new Queue<Quaternion>();
    private Queue<Quaternion> rightUpperPose = new Queue<Quaternion>();
    private Queue<Quaternion> rightForePose = new Queue<Quaternion>();
    private Queue<Quaternion> leftUpperPose = new Queue<Quaternion>();
    private Queue<Quaternion> leftForePose = new Queue<Quaternion>();


    // Correct the values based on a known starting pose.
    // Can be set by a direct event or delay called by DoCorrection();
    public bool correction { get; set; }
    private Quaternion coreBase;
    private Quaternion rightUpperBase;
    private Quaternion rightForeBase;
    private Quaternion leftUpperBase;
    private Quaternion leftForeBase;

    private Quaternion coreCorrection = Quaternion.identity;
    private Quaternion rightUpperCorrection = Quaternion.identity;
    private Quaternion rightForeCorrection = Quaternion.identity;
    private Quaternion leftUpperCorrection = Quaternion.identity;
    private Quaternion leftForeCorrection = Quaternion.identity;

    void Start()
    {
        refCoord = GameObject.Find("ReferenceCoord").transform;
        //grab all the original transforms

        coreBase = core.localRotation;
        rightUpperBase = rightUpper.localRotation;
        rightForeBase = rightFore.localRotation;
        leftUpperBase = leftUpper.localRotation;
        leftForeBase = leftFore.localRotation;
    }

    public float[] getCoreInit()
    {
        return initCore;
    }

    public void setInit()
    {
        //initState = InitState.INIT;
        //StartCoroutine(setPoses());
    }

    public void resetInit()
    {
        initState = InitState.PREINIT;
        initCore = new float[] { 0, 0, 0 };
        StopAllCoroutines();
    }

    private void setInitRot()
    {
        initCorePose = jointRotations.rotateCore(new float[] { 0, 0, 0 },
            new float[] { 0, 0, 0 }, refCoord.localRotation);

        initHeadPose = head.rotation;

        initState = InitState.INIT;
        StartCoroutine(setPoses());
    }

    private IEnumerator setPoses()
    {
        while (true)
        {
            if (corePose.Count == 0 || rightUpperPose.Count == 0 ||
                rightForePose.Count == 0 || leftUpperPose.Count == 0 || leftForePose.Count == 0)
            {
                // Poll until we have poses for all joints
            }
            else if (correction)
            {
                Debug.Log("Correcting Upper Angles");
                coreCorrection = Quaternion.Inverse(corePose.Dequeue()) * coreBase;
                rightUpperCorrection = Quaternion.Inverse(rightUpperPose.Dequeue()) * rightUpperBase;
                rightForeCorrection = Quaternion.Inverse(rightForePose.Dequeue()) * rightForeBase;
                leftUpperCorrection = Quaternion.Inverse(leftUpperPose.Dequeue()) * leftUpperBase;
                leftForeCorrection = Quaternion.Inverse(leftForePose.Dequeue()) * leftForeBase;
                correction = false;
            }
            else
            {
                //only animate the head if there is a hmd
                if (hmdObject != null)
                {
                    head.rotation = hmdObject.transform.rotation;
                }
            
                core.localRotation = corePose.Dequeue() * coreCorrection;
                rightUpper.localRotation = rightUpperPose.Dequeue() * rightUpperCorrection;
                rightFore.localRotation = rightForePose.Dequeue() * rightForeCorrection;
                leftUpper.localRotation = leftUpperPose.Dequeue() * leftUpperCorrection;
                leftFore.localRotation = leftForePose.Dequeue() * leftForeCorrection;
            }
            yield return null;
        }
    }

    //interface method
    public void operate(float[] angles)
    {
        
        //parse angles
        //apply to upper        
        if (initState == InitState.PREINIT && angles != null)
        {
            //do initialization            
            Buffer.BlockCopy((float[])angles.Clone(), 1 * sizeof(float), initCore, 0, 3 * sizeof(float));

            float initSum = initCore[0] + initCore[1] + initCore[2];
            if (!Mathf.Approximately(initSum, 0))
            {
                setInitRot();
            }
        }
        else if (initState == InitState.INIT && angles != null)
        {
            //core node 1
            float[] coreAngles = new float[] { angles[1], angles[2], angles[3] - initCore[2] };
            chain = jointRotations.rotateCore(coreAngles, initCore, refCoord.localRotation);

            corePose.Enqueue(chain);

            //Left Upper user node 2
            //90 deg transform puts sensor in correct orientation
            float[] luAngles = new float[] { angles[5], angles[6], angles[7] - initCore[2] };
            chain = jointRotations.rotateLeftArm(luAngles, core.localRotation, refCoord.localRotation);

            leftUpperPose.Enqueue(chain);

            //Left Fore node 4
            float[] lfAngles = new float[] { angles[9], angles[10], angles[11] - initCore[2] };
            chain = jointRotations.rotateLeftForearm(lfAngles, core.localRotation,
                leftUpper.localRotation, refCoord.localRotation);

            leftForePose.Enqueue(chain);

            //Right Upper node 3
            float[] ruAngles = new float[] { angles[13], angles[14], angles[15] - initCore[2] };
            chain = jointRotations.rotateRightArm(ruAngles, core.localRotation, refCoord.localRotation);

            rightUpperPose.Enqueue(chain);

            //Right Fore (Animation) Right Fore (User) node 5
            float[] rfAngles = new float[] { angles[17], angles[18], angles[19] - initCore[2] };
            chain = jointRotations.rotateRightForearm(rfAngles, core.localRotation,
                rightUpper.localRotation, refCoord.localRotation);

            rightForePose.Enqueue(chain);
        }
    }

    public void NoCorrection()
    {
        coreCorrection = Quaternion.identity;
        rightUpperCorrection = Quaternion.identity;
        rightForeCorrection = Quaternion.identity;
        leftUpperCorrection = Quaternion.identity;
        leftForeCorrection = Quaternion.identity;
    }

    public void DoCorrection(float delay = 3.0f)
    {
        StartCoroutine(WaitandUpdate(delay));
    }

    public IEnumerator WaitandUpdate(float delay = 3.0f)
    {
        if(delay > 0f)
            yield return new WaitForSeconds(delay);
        correction = true;
    }
}