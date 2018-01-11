using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Windows.Kinect;
using UnityOSC;

public class BodyTracker : Controllable
{
    [OSCProperty]
    public float smoothing;

    [OSCProperty]
    public string remoteHost;
    [OSCProperty]
    public int remotePort;

    [Header("Cleaning")]
    public float minRealHeadY;
    public float minRealHeadDist;

    BodySourceManager bsm;

    Dictionary<ulong, Body> bodies;
    public Body lastBody;

    Transform headT;
    Transform bodyT;
    Transform leftHandT;
    Transform rightHandT;
    Transform[] joints;
    Vector3 spineShoulder;

    LineRenderer[] lr;

    public bool sendOSC;
    public Transform origin;
    
    public bool bodyIsTracked;
    bool lastBodyIsTracked;

    [OSCProperty]
    public bool simulateTouch;
    bool lastSimulateTouch;

    [OSCProperty]
    public bool simulateBodyIsTracked;
    public bool lastSimulateBody;

    public Transform bodyTarget;
    Vector3 relTargetHead;
    Vector3 relTargetBody;
    Vector3 relTargetLH;
    Vector3 relTargetRH;

    

    void Start()
    {
        bsm = GetComponent<BodySourceManager>();
        bodies = new Dictionary<ulong, Body>();

        headT = transform.Find("Head");
        bodyT = transform.Find("Body");
        leftHandT = transform.Find("LeftHand");
        rightHandT = transform.Find("RightHand");

        joints = new Transform[] { headT, bodyT, leftHandT, rightHandT };
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

        if(lastBody != null)
        {
            bodyIsTracked = true;
            headT.transform.localPosition = getLocalJointPos(lastBody, JointType.Head);
            bodyT.transform.localPosition = getLocalJointPos(lastBody, JointType.SpineBase);
            leftHandT.transform.localPosition = getLocalJointPos(lastBody, JointType.HandLeft);
            rightHandT.transform.localPosition = getLocalJointPos(lastBody, JointType.HandRight);
            spineShoulder = getAbsoluteJointPos(lastBody, JointType.SpineShoulder);
            bodyTarget.transform.position = new Vector3(bodyT.transform.position.x, 0.01f, bodyT.transform.position.z);
            if (sendOSC) sendFeedback();

        }
        else
        {
            bodyIsTracked = simulateBodyIsTracked;
            spineShoulder = Vector3.Lerp(headT.position, bodyT.position, .3f);

            if(Input.GetMouseButton(0))
            {
                Ray r = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                if(Physics.Raycast(r,out hit))
                {
                    bodyTarget.position = new Vector3(hit.point.x, bodyTarget.position.y, hit.point.z);
                    bodyTarget.LookAt(new Vector3(origin.position.x, bodyTarget.position.y, origin.position.z));

                    bodyT.position = bodyTarget.TransformPoint(relTargetBody);
                    headT.position = bodyTarget.TransformPoint(relTargetHead);
                    leftHandT.position = bodyTarget.TransformPoint(relTargetLH);
                    rightHandT.position = bodyTarget.TransformPoint(relTargetRH);
                    if (sendOSC && bodyIsTracked) sendFeedback();
                }
            }

        }


        if(lastBodyIsTracked != bodyIsTracked)
        {
            lastBodyIsTracked = bodyIsTracked;
            if(sendOSC) sendTrackFeedback();
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
            bodyTarget.GetComponent<Renderer>().material.color = simulateBodyIsTracked ? (simulateTouch ? Color.blue : Color.green) : Color.red;
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
        lastBody = b;
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
        if(lastBody.TrackingId == trackingId)
        {
            lastBody = null;
        }
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
            Debug.Log(newBodies.Length);
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
