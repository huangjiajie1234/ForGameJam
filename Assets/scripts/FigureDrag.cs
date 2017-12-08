using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FigureDrag : MonoBehaviour
{
    private float distOffset = 0;
    private float curTouchDist = 0;
    private float lastTouchDist = 0;
    void Start()
    {
        Input.multiTouchEnabled = true;
    }

    void Update()
    {
        if (Input.touchCount <= 0)
        {
            return;
        }

        if (Input.GetTouch(0).phase == TouchPhase.Moved || Input.GetTouch(1).phase == TouchPhase.Moved)
        {
            var touch1 = Input.GetTouch(0);
            var touch2 = Input.GetTouch(1);

            curTouchDist = (touch1.position - touch2.position).magnitude;

            distOffset = curTouchDist - lastTouchDist;

            if (curTouchDist != lastTouchDist)
            {
                lastTouchDist = curTouchDist;
            }

			//this.gameObject.transform.localScale += Vector3.one * distOffset * Time.deltaTime ;
        }
    }
}
