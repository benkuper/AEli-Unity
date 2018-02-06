using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TouchHandler : Controllable {

    public static TouchHandler instance;

    [OSCProperty]
    public bool isTouched;

    [OSCProperty]
    [Range(.05f,1f)]
    public float shortTouchTime;
    bool lastTouched;
    float timeAtLastTouched;

	// Use this for initialization
	void Start () {
        instance = this;
	}
	
	// Update is called once per frame
	override public void Update () {
        base.Update();

		if(isTouched != lastTouched)
        {
            DataFeedback.sendTouch(isTouched ? 1 : 0);
            if(isTouched) timeAtLastTouched = Time.time;
            else
            {
                if (Time.time < timeAtLastTouched + shortTouchTime) DataFeedback.sendShortTouch();
            }
        }

        lastTouched = isTouched;
	}
}
