﻿//========= Copyright 2016, Enflux Inc. All rights reserved. ===========
//
// Purpose: Lower body mapping and operation with EnfluxVR suit
//
//======================================================================

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class EVRLowerLimbMap : EVRHumanoidLimbMap, ILimbAnimator
{

    public bool useCore = false;
    private EVRUpperLimbMap upperReference;
    private JointRotations jointRotations = new JointRotations();
    private float[] initWaist = new float[] { 0, 0, 0 };
    private float[] initLeftThigh = new float[] { 0, 0, 0 };
    private float[] initLeftShin = new float[] { 0, 0, 0 };
    private float[] initRightThigh = new float[] { 0, 0, 0 };
    private float[] initRightShin = new float[] { 0, 0, 0 };
    private Quaternion chain;
    private Quaternion initWaistPose = new Quaternion();
    private Queue<Quaternion> waistPose = new Queue<Quaternion>();
    private Queue<Quaternion> rightThighPose = new Queue<Quaternion>();
    private Queue<Quaternion> rightShinPose = new Queue<Quaternion>();
    private Queue<Quaternion> leftThighPose = new Queue<Quaternion>();
    private Queue<Quaternion> leftShinPose = new Queue<Quaternion>();

    // Correct the values based on a known starting pose.
    // Can be set by a direct event or delay called by DoCorrection();
    public bool correction { get; set; }
    private Quaternion waistBase;
    private Quaternion rThighBase;
    private Quaternion rShinBase;
    private Quaternion lThighBase;
    private Quaternion lShinBase;

    private Quaternion waistCorrection = Quaternion.identity;
    private Quaternion rThighCorrection = Quaternion.identity;
    private Quaternion rShinCorrection = Quaternion.identity;
    private Quaternion lThighCorrection = Quaternion.identity;
    private Quaternion lShinCorrection = Quaternion.identity;

    void Start()
    {

        refCoord = GameObject.Find("ReferenceCoord").transform;

        waistBase = waist.localRotation;
        rThighBase = rightThigh.localRotation;
        rShinBase = rightShin.localRotation;
        lThighBase = leftThigh.localRotation;
        lShinBase = leftShin.localRotation;

        if (useCore)
        {
            upperReference = GameObject.Find("EVRUpperLimbMap").GetComponent<EVRUpperLimbMap>();
            core = upperReference.core;
        }
    }

    public void setInit()
    {
        //initState = InitState.INIT;
        //StartCoroutine(setPoses());
    }

    public void resetInit()
    {
        initState = InitState.PREINIT;
        initWaist = new float[] { 0, 0, 0 };
        StopAllCoroutines();
    }

    private void setInitRot()
    {
        initWaistPose = jointRotations.rotateWaist(new float[] { 0, 0, 0 },
            new float[] { 0, 0, 0 }, refCoord.localRotation);

        waist.localRotation = initWaistPose;

        initState = InitState.INIT;
        StartCoroutine(setPoses());
    }

    private IEnumerator setPoses()
    {
        while (true)
        {

            if (waistPose.Count == 0 || rightThighPose.Count == 0 ||
                rightShinPose.Count == 0 || leftThighPose.Count == 0 || leftShinPose.Count == 0)
            {
                // Poll until we have poses for all joints
            }
            else if (correction)
            {
                Debug.Log("Correcting Lower Angles");
                waistCorrection = Quaternion.Inverse(waistPose.Dequeue()) * waistBase;
                rThighCorrection = Quaternion.Inverse(rightThighPose.Dequeue()) * rThighBase;
                rShinCorrection = Quaternion.Inverse(rightShinPose.Dequeue()) * rShinBase;
                lThighCorrection = Quaternion.Inverse(leftThighPose.Dequeue()) * lThighBase;
                lShinCorrection = Quaternion.Inverse(leftShinPose.Dequeue()) * lShinBase;
                correction = false;
            }
            else
            {
                waist.localRotation = waistPose.Dequeue() * waistCorrection;
                rightThigh.localRotation = rightThighPose.Dequeue() * rThighCorrection;
                rightShin.localRotation = rightShinPose.Dequeue() * rShinCorrection;
                leftThigh.localRotation = leftThighPose.Dequeue() * lThighCorrection;
                leftShin.localRotation = leftShinPose.Dequeue() * lShinCorrection;
            }
            yield return null;
        }
    }

    public void operate(float[] angles)
    {
        if (angles != null)
        {
            if (initState == InitState.PREINIT && angles != null)
            {
                if (!useCore)
                {
                    Buffer.BlockCopy((float[])angles.Clone(), 1 * sizeof(float), initWaist, 0, 3 * sizeof(float));
                }
                else
                {
                    initWaist = upperReference.getCoreInit();
                }

                float initSum = initWaist[0] + initWaist[1] + initWaist[2];

                if (!Mathf.Approximately(initSum, 0))
                {
                    setInitRot();
                }

            }
            else if (initState == InitState.INIT)
            {
                if (!useCore)
                {
                    //core node 1
                    float[] waistAngles = new float[] { angles[1], angles[2], angles[3] - initWaist[2] };
                    chain = jointRotations.rotateWaist(waistAngles, initWaist, refCoord.localRotation);
                }
                else
                {
                    chain = core.localRotation;
                }

                waistPose.Enqueue(chain);

                //Left Thigh
                float[] ltAngles = new float[] { angles[5], angles[6], angles[7] - initWaist[2] };
                chain = jointRotations.rotateLeftLeg(ltAngles, waist.localRotation,
                    refCoord.localRotation);

                leftThighPose.Enqueue(chain);

                //Left shin
                float[] lsAngles = new float[] { angles[9], angles[10], angles[11] - initWaist[2] };
                chain = jointRotations.rotateLeftShin(lsAngles, waist.localRotation,
                    leftThigh.localRotation, refCoord.localRotation);

                leftShinPose.Enqueue(chain);

                //Right Thigh
                float[] rtAngles = new float[] { angles[13], angles[14], angles[15] - initWaist[2] };
                chain = jointRotations.rotateRightLeg(rtAngles, waist.localRotation,
                    refCoord.localRotation);

                rightThighPose.Enqueue(chain);

                //Right shin
                float[] rsAngles = new float[] { angles[17], angles[18], angles[19] - initWaist[2] };
                chain = jointRotations.rotateRightShin(rsAngles, waist.localRotation,
                    rightThigh.localRotation, refCoord.localRotation);

                rightShinPose.Enqueue(chain);
            }
        }
    }

    public void NoCorrection()
    {
        waistCorrection = Quaternion.identity;
        rThighCorrection = Quaternion.identity;
        rShinCorrection = Quaternion.identity;
        lThighCorrection = Quaternion.identity;
        lShinCorrection = Quaternion.identity;
    }

    public void DoCorrection(float delay = 3.0f)
    {
        StartCoroutine(WaitandUpdate(delay));
    }

    public IEnumerator WaitandUpdate(float delay = 3.0f)
    {
        yield return new WaitForSeconds(delay);
        correction = true;
    }

}
