using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public class Zone
{
    public int intimity;
    public float distance;
    public Color color;
    public Transform transform;
}

public class LampZones : Controllable {

    public Transform bodyTransform;
    public LampMoodController mood;

    public Zone[] zones;
    public Zone zone;


    [OSCProperty]
    [Range(.3f,4)]
    public float distance1;
    [OSCProperty]
    [Range(.3f, 4)]
    public float distance2;
    [OSCProperty]
    [Range(.3f, 4)]
    public float distance3;


    void Start () {
	    	
	}
	
	public override void Update () {
        base.Update();

        zones[0].distance = distance1;
        zones[1].distance = distance2;
        zones[2].distance = distance3;

        foreach(Zone z in zones)
        {
            z.transform.localScale = new Vector3(z.distance, 1, z.distance);
        }

        Zone newZone = getZone(bodyTransform.transform.position);

        if(zone != newZone)
        {
            zone = newZone;

            if(zone != null)
            {
                Debug.Log("Change zone " + zone.intimity);

                switch (zone.intimity)
                {
                case 0:
                    mood.sendMood(LampMoodController.RELAXED, .5f);
                    break;

                case 1:
                    mood.sendMood(LampMoodController.ALARMED, .8f);
                    break;

                case 2:
                    mood.sendMood(LampMoodController.ANGER, .8f);
                    break;

                }

                DataFeedback.sendZone(zone.intimity);
            }
            else
            {
                mood.sendMood(LampMoodController.RELAXED, .5f);
                DataFeedback.sendZone(-1);
            }

            
        }
	}

    Zone getZone(Vector3 p)
    {
        Zone result = null;
        Vector2 p2 = new Vector2(p.x, p.z);
        
        float dist = Vector2.Distance(new Vector2(transform.position.x,transform.position.z), p2);

        for (int i = zones.Length - 1;i >= 0;i--)
        {
            if (dist < zones[i].distance) result = zones[i];
        }

        return result;
    }


#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        foreach(Zone z in zones)
        {
            Handles.color = z.color;
            Handles.DrawWireArc(transform.position+Vector3.up*(.1f-z.distance*.01f), Vector3.up, Vector3.right, 360, z.distance);
        }
    }
#endif
}
