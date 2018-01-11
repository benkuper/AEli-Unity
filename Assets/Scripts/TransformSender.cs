using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityOSC;

public class TransformSender : MonoBehaviour {

    public string jointName;
    public Transform origin;
    float speed;
    Vector3 lastPos;
    public Vector3 relPos;

    public bool includeTimeStamp;
    bool hasChanged;

	void Start () {
		
	}
	
	void Update () {
        Vector3 zOrigin = new Vector3(origin.position.x, 0, origin.position.z);
        relPos = transform.position - zOrigin;
        speed = Vector3.Distance(relPos,lastPos) / Time.deltaTime;

        hasChanged = relPos != lastPos;

        lastPos = new Vector3(relPos.x,relPos.y, relPos.z);


	}

    public void sendOSC(string host, int port)
    {
        if (!hasChanged) return;

        OSCMessage m = new OSCMessage("/joint/"+jointName);
        m.Append(relPos.x);
        m.Append(relPos.y);
        m.Append(relPos.z);
        m.Append(speed);
        if (includeTimeStamp) m.Append(Time.time);

        OSCMaster.sendMessage(m, host, port);
    }
}
