using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityOSC;

public class DataFeedback : Controllable {

    public static DataFeedback instance;

    [OSCProperty]
    public bool sendFeedback;
    [OSCProperty]
    public string remoteHost;
    [OSCProperty]
    public int remotePort;

    override public void Awake()
    {
        base.Awake();
        instance = this;
    }
	
	// Update is called once per frame
	public override void Update () {
        base.Update();
	}

    public static void sendMessage(OSCMessage m)
    {
        if (!instance.sendFeedback) return;
        OSCMaster.sendMessage(m, instance.remoteHost, instance.remotePort);
    }


    //Helpers
    public static void sendValue(string msg, object value)
    {
        OSCMessage m = new OSCMessage("/" + msg, value);
        sendMessage(m);
    }

    public static void sendTrigger(string msg)
    {
        OSCMessage m = new OSCMessage("/" + msg);
        sendMessage(m);
    }

    public static void sendZone(int intimity)
    {
        sendValue("zone", intimity);
    }

    public static void sendSpeed(string jointID, float speed)
    {
        sendValue(jointID+"/speed", speed);
    }

    public static void sendDistance(string jointID, float distance)
    {
        sendValue(jointID+"/distance", distance);
    }

    public static void sendAngle(float angle)
    {
        sendValue("angle",angle);
    }

    public static void sendIdleTime(float value)
    {
        sendValue("idleTime", value);
    }

    public static void sendTouch(int touch)
    {
        sendValue("touch", touch);
    }

    public static void sendShortTouch()
    {
        sendTrigger("shortTouch");
    }

    public static void sendIsMoving(bool isMoving)
    {
        sendValue("isMoving", isMoving?1:0);
    }

    public static void sendTracked(bool bodyIsTracked)
    {
        sendValue("bodyIsTracked", bodyIsTracked ? 1 : 0);
    }
}
