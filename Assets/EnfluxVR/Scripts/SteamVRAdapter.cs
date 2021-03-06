﻿//========= Copyright 2016, Enflux Inc. All rights reserved. ===========
//
// Purpose: Demo adapter for use with SteamVR and HTC Vive
//
//======================================================================

using System.Collections;
using UnityEngine;

[RequireComponent(typeof(EVRHumanoidLimbMap))]
public class SteamVRAdapter : MonoBehaviour
{
    public GameObject hmd;
    public GameObject eyeLocation;
    public GameObject humanoid;    

    // Use this for initialization
    void Start()
    {
        SnapHeadset();
    }

    void Update()
    {
        //if (Input.GetKeyDown("h"))
        //{
        //    SnapHeadset();
        //}
    }

    public void SnapHeadset()
    {
        transform.localRotation = Quaternion
            .AngleAxis(hmd.transform.rotation.eulerAngles.y, Vector3.up);
    }

    public IEnumerator WaitandSnap(float delay = 3.0f)
    {
        yield return new WaitForSeconds(delay);
        SnapHeadset();
    }

    void LateUpdate()
    {
        Vector3 difference = hmd.transform.position - eyeLocation.transform.position;
        transform.Translate(difference);
    }
}