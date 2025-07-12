using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class CelestialObject : MonoBehaviour
{
    public bool debug = false;
    public float mass = 10f;
    public bool orbitParent;
    public CelestialObject parentObject;

    private Rigidbody rb;

    public Vector3 nextVelocity;
    public Vector3 initialAngularVelocity = new Vector3(0,0,0);
    [HideInInspector]
    public Vector3 nextPosition = Vector3.zero;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
            rb = GetComponentInChildren<Rigidbody>();
        if (rb == null)
            throw new Exception("Object needs a rigid body");
        rb.mass = mass;
        rb.linearVelocity = nextVelocity;
        rb.angularVelocity = initialAngularVelocity;
    }

    public void Update()
    {
    }

    public Vector3 GetVelocity()
    {
        return nextVelocity;
    }

    public void SetNextVelocity(Vector3 nextVel)
    {
        if (Application.isPlaying)
            Debug.Log("use this");
        nextVelocity = nextVel;
    }

    public Vector3 UpdateVelocity(List<CelestialObject> celestialObjects, float timeDelta)
    {

        return UpdateVelocity(celestialObjects, transform.position, timeDelta);
    }

    // Update is called once per frame
    public Vector3 UpdateVelocity(List<CelestialObject> celestialObjects, Vector3 relativePos, float timeDelta)
    {
        Vector3 calculatedNextVelocity = Vector3.zero;
        foreach (var body in celestialObjects)
        {
            if (body == this)
            {
                calculatedNextVelocity += body.nextVelocity;
                continue;
            }

            Vector3 direction = (body.transform.position - relativePos).normalized;
            float distance = Mathf.Pow((body.transform.position - relativePos).magnitude, 2);
            float gravForce = (SimulatorOrchestrator.staticGC * body.mass * this.mass) / distance;

            float acceleration = gravForce / this.mass;
            Vector3 additionVelocity = direction * (acceleration * timeDelta);
            calculatedNextVelocity += additionVelocity;
        }
        
        SetNextVelocity(calculatedNextVelocity);
        return nextVelocity;
    }

    public void UpdatePosition(float timeDelta)
    {
        rb.linearVelocity = nextVelocity;
        //rb.position = transform.position + (nextVelocity * timeDelta);
        //transform.position += nextVelocity * timeDelta;
    }

}
