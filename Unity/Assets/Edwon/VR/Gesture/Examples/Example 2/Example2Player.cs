﻿using UnityEngine;
using System.Collections;

namespace Edwon.VR.Gesture.Examples
{
    public class Example2Player : MonoBehaviour
    {

        public GameObject circle;
        public GameObject triangle;
        public GameObject square;
        public GameObject push;
        public GameObject pushLeft;
        public GameObject pushRight;
        public GameObject pull;
        public GameObject nullGO;

        void Start()
        {

        }

        void Update()
        {

        }

        void OnEnable()
        {
            VRGestureManager.GestureDetectedEvent += OnGestureDetected;
            VRGestureManager.GestureNullEvent += OnGestureNull;
        }

        void OnDisable()
        {
            VRGestureManager.GestureDetectedEvent -= OnGestureDetected;
            VRGestureManager.GestureNullEvent -= OnGestureNull;
        }

        void OnGestureDetected(string gestureName, double confidence)
        {
            //string confidenceString = confidence.ToString().Substring(0, 4);
            //Debug.Log("detected gesture: " + gestureName + " with confidence: " + confidenceString);

            switch (gestureName)
            {
                case "Circle":
                    StartCoroutine(AnimateShape(circle));
                    break;             
                case "Triangle":
                    StartCoroutine(AnimateShape(triangle));
                    break;
                case "Push":
                    StartCoroutine(AnimateShape(push));
                    break;
                case "Pull":
                    StartCoroutine(AnimateShape(pull));
                    break;
            }
        }

        void OnGestureNull(string error, string gestureName = null, double confidenceValue = 0)
        {
            StartCoroutine(AnimateShape(nullGO));
        }

        IEnumerator AnimateShape(GameObject shape)
        {
            Renderer[] renderers = shape.GetComponentsInChildren<Renderer>();
            foreach (Renderer r in renderers)
            {
                r.material.color = Color.red;
            }

            yield return new WaitForSeconds(.6f);

            foreach (Renderer r in renderers)
            {
                r.material.color = Color.white;
            }
        }

    }
}