using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Utility.Utility;


public class DampedTrack : MonoBehaviour{

    [HideInInspector] public Transform target;
    [SerializeField] private string m_TargetName;
    [SerializeField] public float smoothTime = 1f;
    [SerializeField] public bool snapToPixles = true;
    
    private Vector3 velocity = Vector3.zero;
    private Vector3 position;

    /* yes, i did take this straight from the example on Unity's documentation. I think I've changed
    it changed it enough for this to be okay though. */

    void Start(){
        target = GameObject.Find(m_TargetName).transform;
        // initialize position:
        position = transform.position;
    }

    void Update(){
        // Define a target position above and behind the target transform
        Vector3 targetPosition = target.TransformPoint(new Vector3(0,0,0));

        // Smoothly move towards that target position
        position = Vector3.SmoothDamp(position, targetPosition, ref velocity, smoothTime);
        transform.position = snapToPixles? TransformToPixels(position) : position;
        transform.rotation = target.transform.rotation;
    }
}

