using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Swipe : MonoBehaviour
{

    private bool tap, swipeUp, swipeDown, swipeLeft, swipeRight;
    private bool isDraging = false;
    private Vector2 startTouch, swipeDelta;

    private void Update()
    {

        tap = swipeDown = swipeLeft = swipeRight = swipeUp = false;

        if (Input.touches.Length > 0)
        {

            if (Input.touches[0].phase == TouchPhase.Began)
            {
                tap = true;
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
                    swipeLeft = true;
                }
                else
                {
                    swipeRight = true;
                }

            }
            else
            {
                //Up or Down
                if (y < 0)
                {
                    swipeDown = true;
                }
                else
                {
                    swipeUp = true;
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
    public bool Tap { get { return tap; } }
    public bool SwipeUp { get { return swipeUp; } }
    public bool SwipeDown { get { return swipeDown; } }
    public bool SwipeLeft { get { return swipeLeft; } }
    public bool SwipeRight { get { return swipeRight; } }





}
