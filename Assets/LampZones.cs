using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Zone
{
    public float distance;
    public Color color;
}

public class LampZones : MonoBehaviour {

    public Zone[] zones;

	void Start () {
	    	
	}
	
	void Update () {
		
	}

    int getZone(Vector3 p)
    {
        int result = -1;
        Vector2 p2 = new Vector2(p.x, p.z);
        for (int i = zones.Length - 1;i >= 0;i--)
        {
            float dist = Vector2.Distance(transform.position, p);
            if (dist < zones[i].distance) result = i;
        }

        return result;
    }
}
