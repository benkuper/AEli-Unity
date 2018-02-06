using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityOSC;

public class LampMoodController : Controllable {

    public const string OFF = "off";
    public const string RELAXED = "relaxed";
    public const string HAPPY = "happy";
    public const string ANGER = "anger";
    public const string ALARMED = "alarmed";
    public const string DELIGHTED = "delighted";

    [OSCProperty]
    public bool enableSend;
    [OSCProperty]
    public string remoteHost = "127.0.0.1";
    [OSCProperty]
    public int remotePort = 11450;

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	public override void Update () {
        base.Update();
	}


    public void sendMood(string mood, float value)
    {
        if (!enableSend) return;
        OSCMessage m = new OSCMessage("/attraction/" + mood);
        m.Append(value);
        OSCMaster.sendMessage(m, remoteHost, remotePort);
    }
}
