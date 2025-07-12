using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

[ExecuteInEditMode]
public class SimulatorPreview : MonoBehaviour
{

    public int iterations = 10;
    public float deltaTime = 0.5f;
    private InEditorBodySim[] bodies;
    private Vector3[][] futurePositions;

    public class InEditorBodySim
    {

        public float mass;
        public Vector3 nextVelocity;
        public Vector3 nextPosition;
        public Color color;
        public InEditorBodySim(CelestialObject body)
        {
            mass = body.mass;
            nextVelocity = body.GetVelocity();
            nextPosition = body.transform.position;
            var mr = body.GetComponent<MeshRenderer>();
            if (mr == null)
                mr = body.GetComponentInChildren<MeshRenderer>();
            color = mr.sharedMaterial.color;
        }
    }
    public void addBodies()
    {

        var celestialBodies = GameObject.FindObjectsByType<CelestialObject>(FindObjectsSortMode.None);
        bodies = new InEditorBodySim[celestialBodies.Count()];
        for (int i = 0; i < celestialBodies.Count(); i++)
        {
            bodies[i] = new InEditorBodySim(celestialBodies.ElementAt(i)); 
        }
    }

    public void CalculateVelocities(float deltaTime)
    {
        foreach (var body in bodies)
        {
            Vector3 calculatedNextVelocity = Vector3.zero;
            foreach (var secondBody in bodies)
            {
                if (body == secondBody)
                {
                    calculatedNextVelocity += body.nextVelocity;
                    continue;
                }

                Vector3 direction = (secondBody.nextPosition - body.nextPosition).normalized;
                float distance = Mathf.Pow((secondBody.nextPosition - body.nextPosition).magnitude, 2);
                float gravForce = (SimulatorOrchestrator.staticGC * secondBody.mass * body.mass) / distance;

                float acceleration = gravForce / body.mass;
                Vector3 additionVelocity = direction * (acceleration * deltaTime);
                calculatedNextVelocity += additionVelocity;
            }
            body.nextVelocity = calculatedNextVelocity;
        }
    }

    public void CalculatePositions(float deltaTime)
    {
        foreach (var body in bodies)
        {
            body.nextPosition += body.nextVelocity * deltaTime;
        }
    }

    public void Start()
    {
    }

    public void OnDrawGizmos()
    {
        addBodies();
        CalculateEstimates(SimulatorOrchestrator.staticTimeDelta);
        for (int i = 1; i < iterations; i++)
        {
            for (int j = 0; j < bodies.Count(); j++) 
            {
                Gizmos.color = bodies[j].color;
                Gizmos.DrawLine(futurePositions[i-1][j], futurePositions[i][j]);
            }
        }
    }

    public void CalculateEstimates(float timeDelta)
    {
        futurePositions = new Vector3[iterations][];
        for (var i = 0; i < iterations; i++)
        {
            var iterationPosition = new Vector3[bodies.Length];
            CalculateVelocities(timeDelta);
            CalculatePositions(timeDelta);
            for(int v = 0; v < bodies.Length; v++)
            {
                iterationPosition[v] = bodies[v].nextPosition;
            }
            futurePositions[i] = iterationPosition;
        }
    }
}