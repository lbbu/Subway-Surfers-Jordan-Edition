using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Swipe : MonoBehaviour
{

    public event EventHandler OnSwipeUp, OnSwipeDown, OnSwipeLeft, OnSwipeRight;
    private bool isDraging = false;
    private Vector2 startTouch, swipeDelta;

    public static Swipe Instance;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {


        if (Input.touches.Length > 0)
        {

            if (Input.touches[0].phase == TouchPhase.Began)
            {
                isDraging = true;

                startTouch = Input.touches[0].position;

            }
            else if (Input.touches[0].phase == TouchPhase.Ended ||
                Input.touches[0].phase == TouchPhase.Canceled)
            {
                Reset();
            }

        }

        //Calculate the distance

        swipeDelta = Vector2.zero;

        if (isDraging)
        {

            if (Input.touches.Length > 0)
            {
                swipeDelta = Input.touches[0].position - startTouch;
            }

        }


        // Did we cross the deadZone

        if (swipeDelta.magnitude > 125)
        {

            //Which Direction??
            float x = swipeDelta.x;
            float y = swipeDelta.y;

            if (Mathf.Abs(x) > Mathf.Abs(y))
            {
                // left or right
                if (x < 0)
                {
                    OnSwipeLeft?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    OnSwipeRight?.Invoke(this, EventArgs.Empty);
                }

            }
            else
            {
                //Up or Down
                if (y < 0)
                {
                    OnSwipeDown?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    OnSwipeUp?.Invoke(this, EventArgs.Empty);
                }

            }

            Reset();

        }


    }


    private void Reset()
    {
        startTouch = swipeDelta = Vector2.zero;
        isDraging = false;
    }




    public Vector2 StartTouch { get { return startTouch; } }


}
