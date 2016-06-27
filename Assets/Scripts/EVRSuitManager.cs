﻿using UnityEngine;
using UnityEngine.UI;
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using EnflxStructs;

public class EVRSuitManager : MonoBehaviour
{
    private ComPorts availablePorts;
    private AttachedPort attachedPort;
    public List<string> ports { get { return availablePorts._ports; } }
    public List<string> connectedDevices;
    private State operatingState = State.NONE;

    private enum State
    {
        NONE,
        ATTACHED,
        DETACHED,
        CONNECTED,
        DISCONNECTED,
        CALIBRATING,
        STREAMING,
    }
    
    void Awake()
    {
        //Get available COM ports
        availablePorts = new ComPorts();
        EnfluxVRSuit.startScanPorts(availablePorts);
    } 
    
    void Start()
    {
        //todo: add in start up of socket server
    }  

    /**
     * parse friendly name to find COM port 
     * pass COM port in to connect
     * */
    public void attachPort(string friendlyName)
    {
        if(operatingState == State.NONE || operatingState == State.DETACHED)
        {
            System.Text.RegularExpressions.Regex toComPort =
            new System.Text.RegularExpressions.Regex(@".? \((COM\d+)\)$");
            if (toComPort.IsMatch(friendlyName.ToString()))
            {
                StringBuilder comName = new StringBuilder()
                    .Append(toComPort.Match(friendlyName.ToString()).Groups[1].Value);
                Debug.Log(comName);
                attachedPort = new AttachedPort();
                if (EnfluxVRSuit.attachSelectedPort(comName, attachedPort) < 1)
                {
                    operatingState = State.ATTACHED;
                }else
                {
                    Debug.Log("Error while trying to attach to port: " + comName);
                }
            }
        }else
        {
            Debug.Log("Unable to attach, program is in wrong state "  
                + Enum.GetName(typeof(State), operatingState));
        }
    }

    //api expects input of all address to connect to, seperated by comma
    //example format: XX:XX:XX:XX:XX:XX,YY:YY:YY:YY:YY:YY
    public void connectEnflux(List<string> devices)
    {
        StringBuilder apiArg = new StringBuilder();
        for (int device = 0; device < devices.Count; device++)
        {
            apiArg.Append(devices[device]);
            if (device < (devices.Count - 1))
            {
                apiArg.Append(",");
            }
        }

        if(operatingState == State.ATTACHED || operatingState == State.DISCONNECTED)
        {
            if (EnfluxVRSuit.connect(apiArg, devices.Count) < 1)
            {
                connectedDevices = devices;
                operatingState = State.CONNECTED;
                Debug.Log("Devices connected");
            }
            else
            {
                Debug.Log("Problem connecting");
            }
        }else
        {
            Debug.Log("Unable to connect to devices, program is in wrong state "
                + Enum.GetName(typeof(State), operatingState));
        }
    }

    public void disconnectEnflux()
    {
        if(operatingState == State.CONNECTED)
        {
            if (EnfluxVRSuit.disconnect(connectedDevices.Count) < 1)
            {
                Debug.Log("Devices disconnected");
                operatingState = State.DISCONNECTED;
            }
            else
            {
                Debug.Log("Problem disconnecting");
            }
        }else
        {
            Debug.Log("Unable to disconnect, program is in wrong state "
                + Enum.GetName(typeof(State), operatingState));
        }
    }

    public void calibrateDevices()
    {
        if(operatingState == State.CONNECTED)
        {
            if (EnfluxVRSuit.performCalibration(connectedDevices.Count) < 1)
            {
                operatingState = State.CALIBRATING;
            }
            else
            {
                Debug.Log("Problem running calibration");
            }
        }else
        {
            Debug.Log("Unable to calibrate, program is in wrong state "
                + Enum.GetName(typeof(State), operatingState));
        }
    }

    public void finishCalibration()
    {
        if(operatingState == State.CALIBRATING)
        {
            if (EnfluxVRSuit.finishCalibration(connectedDevices.Count) < 1)
            {
                operatingState = State.CONNECTED;
            }
            else
            {
                Debug.Log("Problem occured during calibration");
            }
        }else
        {
            Debug.Log("Unable to stop calibration, program is in wrong state "
                + Enum.GetName(typeof(State), operatingState));
        }
    }

    public void enableAnimate()
    {
        if(operatingState == State.CONNECTED)
        {
            if (EnfluxVRSuit.streamRealTime(connectedDevices.Count) < 1)
            {
                operatingState = State.STREAMING;
            }
            else
            {
                Debug.Log("Error, no devices to animate");
            }
        }else
        {
            Debug.Log("Unable to stream, program is in wrong state "
                + Enum.GetName(typeof(State), operatingState));
        }
    }

    public void disableAnimate()
    {
        if(operatingState == State.STREAMING)
        {
            if (EnfluxVRSuit.stopRealTime(connectedDevices.Count) > 1)
            {
                operatingState = State.CONNECTED;
            }
            else
            {
                Debug.Log("Problem occured while stopping stream");
            }
        }else
        {
            Debug.Log("Unable to stop stream, program is in wrong state "
                + Enum.GetName(typeof(State), operatingState));
        }
    }

    public void detachPort()
    {
        if(operatingState == State.ATTACHED ||  operatingState == State.DISCONNECTED)
        {
            if (EnfluxVRSuit.detachPort() < 1)
            {
                operatingState = State.DETACHED;
            }
            else
            {
                Debug.Log("Error occured while detaching");
            }
        }else
        {
            Debug.Log("Unable to detach from port, program is in wrong state "
                + Enum.GetName(typeof(State), operatingState));
        }
    }   

    //called on thread created by native dll
    private class ComPorts : EnfluxVRSuit.IFindPortCallback
    {
        public List<string> _ports = new List<string>();

        public void findportCallback(StringBuilder name)
        {
            if (!_ports.Contains(name.ToString()))
            {
                _ports.Add(name.ToString());
            }
        }       
    }

    //called on thread created by native dll
    private class AttachedPort : EnfluxVRSuit.IOperationCallbacks
    {   
        public void messageCallback(sysmsg msgresult)
        {
            Debug.Log(msgresult.msg);
        }

        public void scanCallback(scandata scanresult)
        {
            ThreadDispatch.instance.AddScanItem(scanresult);
        }

        public static void poop(scandata result)
        {
            Debug.Log(result.addr);
        }

        public void streamCallback(streamdata streamresult)
        {
            Debug.Log(streamresult.data);
        }
    }
}
