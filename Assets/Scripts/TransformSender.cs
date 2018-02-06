using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityOSC;

public class TransformSender : MonoBehaviour {

    public string jointName;
    public Transform origin;
    Vector3 lastSendPos;
    Vector3 lastRealUpdatePos;
    public Vector3 relPos;
    public Vector3 groundPos;

    public bool includeTimeStamp;
    bool hasChangedSinceLastUpdate;
    float lastSendTime;

    //
    public bool sendDistAndSpeed;
    int sendUpdateFPS = 50;
    float lastSendUpdateTime;
    public bool sendIdleTime;
    public float idleSpeedThreshold;
    float idleTime;

    public float currentSpeed;
    public float speedSmooth;
    public float refSpeed;

    BodyTracker tracker;

    //Smoothing
    public Vector3 targetLocalPosition;
    Vector3 velocity;

    bool isMoving;

    void Start () {
        tracker = GetComponentInParent<BodyTracker>();
	}
	
	void Update () {
        transform.localPosition = Vector3.SmoothDamp(transform.localPosition, targetLocalPosition, ref velocity, tracker.smoothing,10,Time.deltaTime);

        Vector3 zOrigin = new Vector3(origin.position.x, 0, origin.position.z);
        relPos = transform.position - zOrigin;
        groundPos = new Vector3(transform.position.x, 0, transform.position.z) - zOrigin;

        if (relPos != lastSendPos) hasChangedSinceLastUpdate = true;


        if (Time.time > lastSendUpdateTime + 1.0f / sendUpdateFPS)
        {
            float deltaUpdateTime = (Time.time - lastSendUpdateTime);
            float targetSpeed = Vector3.Distance(relPos, lastRealUpdatePos) / deltaUpdateTime;
            if (speedSmooth == 0) currentSpeed = targetSpeed;
            else currentSpeed = Mathf.SmoothDamp(currentSpeed, targetSpeed, ref refSpeed, speedSmooth);

            if (currentSpeed < idleSpeedThreshold) idleTime += deltaUpdateTime;
            else idleTime = 0;

            bool nowIsMoving = idleTime > 0;

            if (sendIdleTime)
            {
                DataFeedback.sendIdleTime(idleTime);
                if (isMoving != nowIsMoving)
                {
                    DataFeedback.sendIsMoving(nowIsMoving);
                }
            }

            isMoving = nowIsMoving;


            if (sendDistAndSpeed)
            {
                DataFeedback.sendDistance(jointName, groundPos.magnitude);
                DataFeedback.sendSpeed(jointName, currentSpeed);
            }

            lastRealUpdatePos = new Vector3(relPos.x, relPos.y, relPos.z);
            lastSendUpdateTime = Time.time;
        }
    }

    public void sendOSC(string host, int port)
    {
        if (!hasChangedSinceLastUpdate) return;

        OSCMessage m = new OSCMessage("/joint/"+jointName);
        m.Append(relPos.x);
        m.Append(relPos.y);
        m.Append(relPos.z);

        float deltaUpdateTime = Time.time - lastSendTime;
        float speed = Vector3.Distance(relPos, lastSendPos) / deltaUpdateTime;
        m.Append(speed);

        lastSendTime = Time.time;
        lastSendPos = new Vector3(relPos.x, relPos.y, relPos.z);


        if (includeTimeStamp) m.Append(Time.time);

        OSCMaster.sendMessage(m, host, port);

        hasChangedSinceLastUpdate = false;

    }
}
