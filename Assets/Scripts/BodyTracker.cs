using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Windows.Kinect;
using UnityOSC;

public class BodyTracker : Controllable
{
    [OSCProperty]
    public bool sendOSC;
    [OSCProperty]
    [Range(1, 60)]
    public int sendFreq;
    [OSCProperty]
    public string remoteHost;
    [OSCProperty]
    public int remotePort;

    float lastUpdateTime;



    [OSCProperty]
    [Range(0,1)]
    public float smoothing;

    [Header("Cleaning")]
    public float minRealHeadY;
    public float minRealHeadDist;

    BodySourceManager bsm;

    Dictionary<ulong, Body> bodies;
    public Body closestBody;

    TransformSender headT;
    TransformSender bodyT;
    TransformSender leftHandT;
    TransformSender rightHandT;
    Transform[] joints;

    [OSCProperty]
    [Range(0,10)]
    public float handSpeedThreshold;
    [OSCProperty]
    [Range(0, 10)]
    public float bodySpeedThreshold;

    Vector3 lastHandPos;
    Vector3 lastBodyPos;
    float handSpeeds;
    float bodySpeed;

    Vector3 spineShoulder;

    LineRenderer[] lr;

    public Transform origin;
    
    [OSCProperty]
    public bool bodyIsTracked;
    bool lastBodyIsTracked;


    [OSCProperty]
    public bool simulateTouch;
    public bool lastSimulateTouch;

    [OSCProperty]
    public bool simulateBodyIsTracked;
    public bool lastSimulateBody;

    public Transform bodyTarget;
    Vector3 relTargetHead;
    Vector3 relTargetBody;
    Vector3 relTargetLH;
    Vector3 relTargetRH;

    float angle;

    public LampMoodController mood;

    void Start()
    {
        bsm = GetComponent<BodySourceManager>();
        bodies = new Dictionary<ulong, Body>();

        headT = transform.Find("Head").GetComponent<TransformSender>();
        bodyT = transform.Find("Body").GetComponent<TransformSender>();
        leftHandT = transform.Find("LeftHand").GetComponent<TransformSender>();
        rightHandT = transform.Find("RightHand").GetComponent<TransformSender>();

        joints = new Transform[] { headT.transform, bodyT.transform, leftHandT.transform, rightHandT.transform };
        lr = new LineRenderer[joints.Length];
        for (int i = 0; i < joints.Length; i++) lr[i] = joints[i].GetComponent<LineRenderer>();

        //initial transforms
        bodyTarget.transform.position = new Vector3(bodyT.transform.position.x, 0.01f, bodyT.transform.position.z);
        relTargetHead = bodyTarget.InverseTransformPoint(headT.transform.position);
        relTargetBody = bodyTarget.InverseTransformPoint(bodyT.transform.position);
        relTargetLH = bodyTarget.InverseTransformPoint(leftHandT.transform.position);
        relTargetRH = bodyTarget.InverseTransformPoint(rightHandT.transform.position);

        lastSimulateBody = true; //force refresh;
        sendTrackFeedback();
    }

    public override void Update()
    {
        base.Update();

        processBodies();

        if (closestBody != null && !closestBody.IsTracked)
        {
            Debug.LogWarning("Force body null");
            closestBody = null;
        }

        closestBody = null;
        float dist = 20;
        foreach (KeyValuePair<ulong,Body> b in bodies)
        {
            float d = getLocalJointPos(b.Value, JointType.SpineBase).magnitude;
            if (d < dist)
            {
                closestBody = b.Value;
                dist = d;
            }
        }

        bool shouldSend = false;
        float deltaUpdateTime = Time.time - lastUpdateTime;

        if (Time.time > lastUpdateTime + 1.0f / sendFreq)
        {
            lastUpdateTime = Time.time;
            shouldSend = true;
        }

        if (closestBody != null)
        {
            headT.targetLocalPosition = getLocalJointPos(closestBody, JointType.Head);
            bodyT.targetLocalPosition = getLocalJointPos(closestBody, JointType.SpineBase);
            leftHandT.targetLocalPosition = getLocalJointPos(closestBody, JointType.HandLeft);
            rightHandT.targetLocalPosition = getLocalJointPos(closestBody, JointType.HandRight);
            spineShoulder = getAbsoluteJointPos(closestBody, JointType.SpineShoulder);
            bodyTarget.transform.position = new Vector3(bodyT.transform.position.x, 0.01f, bodyT.transform.position.z);

            if (sendOSC && shouldSend) sendFeedback();

            if (bodyIsTracked && shouldSend)
            {

                bodySpeed = Vector3.Distance(bodyT.transform.localPosition, lastBodyPos) / deltaUpdateTime;
                handSpeeds = Vector3.Distance(rightHandT.transform.localPosition, lastHandPos) / deltaUpdateTime;


                if (bodySpeed > bodySpeedThreshold) mood.sendMood(LampMoodController.DELIGHTED, .4f);
                if (handSpeeds > handSpeedThreshold) mood.sendMood(LampMoodController.HAPPY, .4f);
            }

            lastBodyPos = bodyT.transform.localPosition;
            lastHandPos = rightHandT.transform.localPosition;

            Vector2 groundBody = new Vector2(bodyTarget.position.x, bodyTarget.position.z);
            Vector2 groundOrigin = new Vector2(origin.position.x+1, origin.position.z);
            angle = Vector2.SignedAngle(groundOrigin, groundBody);
            DataFeedback.sendAngle(angle);

            bodyIsTracked = true;
        }
        else
        {
            spineShoulder = Vector3.Lerp(headT.transform.position, bodyT.transform.position, .3f);

            if (Input.GetMouseButton(0))
            {
                Ray r = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                if (Physics.Raycast(r, out hit))
                {
                    bodyTarget.position = new Vector3(hit.point.x, bodyTarget.position.y, hit.point.z);
                    bodyTarget.LookAt(new Vector3(origin.position.x, bodyTarget.position.y, origin.position.z));

                    bodyT.targetLocalPosition = transform.InverseTransformPoint(bodyTarget.TransformPoint(relTargetBody));
                    headT.targetLocalPosition = transform.InverseTransformPoint(bodyTarget.TransformPoint(relTargetHead));
                    leftHandT.targetLocalPosition = transform.InverseTransformPoint(bodyTarget.TransformPoint(relTargetLH));
                    rightHandT.targetLocalPosition = transform.InverseTransformPoint(bodyTarget.TransformPoint(relTargetRH));

                    if (sendOSC && bodyIsTracked && shouldSend) sendFeedback();

                    
                    Vector2 groundBody = new Vector2(bodyTarget.position.x, bodyTarget.position.z);
                    Vector2 groundOrigin = new Vector2(origin.position.x+1, origin.position.z);
                    angle = Vector2.SignedAngle(groundOrigin, groundBody);
                    DataFeedback.sendAngle(angle);
                }
            }

            if (bodyIsTracked && shouldSend)
            {

                bodySpeed = Vector3.Distance(bodyT.transform.localPosition, lastBodyPos) / deltaUpdateTime;
                handSpeeds = Vector3.Distance(rightHandT.transform.localPosition, lastHandPos) / deltaUpdateTime  ;

                if (bodySpeed > bodySpeedThreshold) mood.sendMood(LampMoodController.DELIGHTED, .4f);
                if (handSpeeds > handSpeedThreshold) mood.sendMood(LampMoodController.HAPPY, .4f);
            }

            lastBodyPos = bodyT.transform.localPosition;
            lastHandPos = rightHandT.transform.localPosition;

            bodyIsTracked = simulateBodyIsTracked;
        }
       

        if(lastBodyIsTracked != bodyIsTracked)
        {
            lastBodyIsTracked = bodyIsTracked;
            if (bodyIsTracked) mood.sendMood(LampMoodController.RELAXED, .5f);
            else mood.sendMood(LampMoodController.OFF, .7f);
            bodyTarget.GetComponent<Renderer>().material.color = bodyIsTracked ? (simulateTouch ? Color.blue : Color.green) : Color.red;

            if (sendOSC) sendTrackFeedback();
            DataFeedback.sendTracked(bodyIsTracked);
        }

        if(!bodyIsTracked)
        {
            mood.sendMood(LampMoodController.OFF, .5f);
        }else
        {
            mood.sendMood(LampMoodController.RELAXED, .15f);
        }

        if (Input.GetKeyDown(KeyCode.T)) simulateTouch = !simulateTouch;
        if (Input.GetKeyDown(KeyCode.B)) simulateBodyIsTracked = !simulateBodyIsTracked;

        if(lastSimulateBody != simulateBodyIsTracked)
        {
            lastSimulateBody = simulateBodyIsTracked;
            bodyTarget.GetComponent<Renderer>().material.color = simulateBodyIsTracked ? (simulateTouch?Color.blue:Color.green) : Color.red;
        }

        if(lastSimulateTouch != simulateTouch)
        {
            lastSimulateTouch = simulateTouch;
            if (sendOSC) sendTouchFeedback();
            TouchHandler.instance.isTouched = simulateTouch;
            mood.sendMood(LampMoodController.DELIGHTED, .8f);
            bodyTarget.GetComponent<Renderer>().material.color = bodyIsTracked ? (simulateTouch ? Color.blue : Color.green) : Color.red;

        }

        foreach (LineRenderer r in lr) r.SetPositions(new Vector3[] { spineShoulder, r.transform.position });

    }

    void sendTouchFeedback()
    {
        OSCMessage m = new OSCMessage("/touch");
        m.Append<int>(simulateTouch?1:0);
        m.Append(Time.time);
        OSCMaster.sendMessage(m, remoteHost, remotePort);

    }

    void sendTrackFeedback()
    {
        OSCMessage m = new OSCMessage("/bodyIsTracked");
        m.Append<int>(bodyIsTracked ? 1 : 0);
        m.Append(Time.time);
        OSCMaster.sendMessage(m, remoteHost, remotePort);

    }

    void sendFeedback()
    {
        Vector3 o = new Vector3(origin.position.x, 0, origin.position.z);
        foreach (Transform t in joints) t.GetComponent<TransformSender>().sendOSC(remoteHost, remotePort);
    }


    void addBody(Body b)
    {
        Debug.Log("Add Body "+b.TrackingId);
        bodies.Add(b.TrackingId, b);
        //lastBody = b;
        Debug.Log(" > New body count " + bodies.Count);
    }

    void updateBody(Body b)
    {
        bodies[b.TrackingId] = b;
    }

    void removeBody(ulong trackingId)
    {
        Debug.Log("Remove Body "+ trackingId);
        bodies.Remove(trackingId);
       // if(lastBody.TrackingId == trackingId)
        //{
        //    lastBody = null;
       // }
        Debug.Log(" > New body count : " + bodies.Count);
    }

    bool isBodyValid(Body b)
    {
        if (!b.IsTracked || b.TrackingId == 0) return false;

        Vector3 h = getAbsoluteJointPos(b, JointType.Head);
        Vector2 hXZ = new Vector2(h.x, h.z);
        if (b.Joints[JointType.Head].Position.Y < minRealHeadY && hXZ.magnitude < minRealHeadDist) return false;
        return true;
    }

    Vector3 getAbsoluteJointPos(Body b, JointType j)
    {
        return transform.TransformPoint(getLocalJointPos(b,j));
    }

    Vector3 getLocalJointPos(Body b, JointType j)
    {
        return new Vector3(b.Joints[j].Position.X, b.Joints[j].Position.Y, b.Joints[j].Position.Z);
    }

    void processBodies()
    {
        Body[] newBodies = bsm.GetData();
        if (newBodies != null)
        {
            List<Body> validBodies = new List<Body>();

            foreach(Body b in newBodies)
            {
                if (isBodyValid(b)) validBodies.Add(b);
            }

            List<ulong> bodiesToRemove = new List<ulong>();
            foreach (KeyValuePair<ulong, Body> b in bodies)
            {
                bool found = false;
                Debug.Log("Check for existence  : " + b.Key);
                for (int i = 0; i < validBodies.Count; i++)
                {
                    if (b.Key == validBodies[i].TrackingId)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found) bodiesToRemove.Add(b.Key);
            }

            foreach (ulong id in bodiesToRemove) removeBody(id);

            List<Body> bodiesToAdd = new List<Body>();
            foreach (Body b in validBodies)
            {
                if (bodies.ContainsKey(b.TrackingId)) updateBody(b);
                else bodiesToAdd.Add(b);
            }

            foreach (Body b in bodiesToAdd) addBody(b);

        }
    }
}
