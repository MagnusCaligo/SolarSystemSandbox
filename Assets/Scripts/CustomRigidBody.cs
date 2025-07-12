using System.Collections;
using System.Collections.Generic;
using UnityEditor.PackageManager;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CustomRigidBody : MonoBehaviour
{

    private Rigidbody body;
    public float groundDrag = 5;
    public float airDrag = 1;
    public CelestialObject initialMatchVeloicty;

    // Grounded Logic
    public GroundDetector groundDetector;
    private int groundCount = 0;

    private Rigidbody rb;
    private Vector3 nextVelocity;

    // Start is called before the first frame update
    void Start()
    {

        // Ground Logic
        //groundDetector.OnGrounded += onGroundDetected;
        //groundDetector.OnUngrounded += onGroundUndetected;
        rb = gameObject.GetComponent<Rigidbody>();
        if (initialMatchVeloicty != null)
        {
            rb.linearVelocity = initialMatchVeloicty.nextVelocity;
        }
    }

    public void calculateVelocity(float timeDelta)
    {
        Vector3 calculatedNextVelocity = Vector3.zero;
        foreach (var body in SimulatorOrchestrator.instance.celestialObjects)
        {
            if (body == this)
            {
                calculatedNextVelocity += body.nextVelocity;
                continue;
            }

            Vector3 direction = (body.transform.position - transform.position).normalized;
            float distance = Mathf.Pow((body.transform.position - transform.position).magnitude, 2);
            float gravForce = (SimulatorOrchestrator.staticGC * body.mass * rb.mass) / distance;

            float acceleration = gravForce / rb.mass;
            Vector3 additionVelocity = direction * (acceleration * timeDelta);
            calculatedNextVelocity += additionVelocity;
        }
        nextVelocity = calculatedNextVelocity;
    }

    public void UpdatePosition()
    {
        rb.linearVelocity += nextVelocity;
    }

    private void onGroundDetected()
    {
        groundCount++;
        if (groundCount > 0)
            body.linearDamping = groundDrag;
    }

    private void onGroundUndetected()
    {
        if (groundCount > 0)
            groundCount--;
        if (groundCount == 0)
            body.linearDamping = airDrag;
    }
}
