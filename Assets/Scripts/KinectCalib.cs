using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Windows.Kinect;

public class KinectCalib : Controllable {

    [OSCProperty]
    [Range(0,360)]
    public float offsetYaw;
    [OSCProperty]
    [Range(-3,3)]
    public float offsetX;
    [OSCProperty]
    [Range(-3,3)]
    public float offsetZ;
    [OSCProperty]
    public bool viewMesh;
    [OSCProperty]
    public bool updateMesh;

    public Transform rotateTarget;

    KinectSensor sensor;
    BodyFrameReader bodyFrameReader;

    public Vector3 normal;

    Transform kinect;
    Transform rotT;
    Transform planeT;
    KinectMesh km;

	// Use this for initialization
	void Start () {
        sensor = KinectSensor.GetDefault();
        sensor.Open();
        if(sensor.IsOpen)
        {
            bodyFrameReader = sensor.BodyFrameSource.OpenReader();
        }

        kinect = transform.Find("Kinect");
        rotT = kinect.Find("PlaneNormal");
        planeT = rotT.Find("Plane");
        km = kinect.Find("Mesh").GetComponent<KinectMesh>();
    }
    
    // Update is called once per frame
    public override void Update () {
        base.Update();
        BodyFrame f = bodyFrameReader.AcquireLatestFrame();
        if(f != null)
        {
            //Debug.Log("New body frame "+f.BodyCount);
            normal = new Vector3(f.FloorClipPlane.X, f.FloorClipPlane.Y, f.FloorClipPlane.Z);
            rotT.localRotation = Quaternion.LookRotation(normal);
            rotT.Rotate(90, 0, 0,Space.Self);
            planeT.localPosition = Vector3.down*f.FloorClipPlane.W;

            kinect.transform.localRotation = Quaternion.Inverse(rotT.localRotation);
            kinect.transform.localPosition = new Vector3(offsetX, f.FloorClipPlane.W, offsetZ);

            transform.localRotation = Quaternion.Euler(0, offsetYaw, 0);

            f.Dispose();

        }

        km.enabled = updateMesh && viewMesh;
        km.GetComponent<Renderer>().enabled = viewMesh;

	}

    private void OnGUI()
    {
        
    }

    private void OnDrawGizmos()
    {
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
        Gizmos.DrawFrustum(Vector3.zero, 84.1f, 6, .5f, 1.563f);
    }
}
