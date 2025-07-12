
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

[ExecuteAlways]
public class CelestialBodyController : MonoBehaviour
{

    public List<CelestialObject> celestialObjects;

    public void Start()
    {
        celestialObjects = new List<CelestialObject>();
        foreach (CelestialObject body in GameObject.FindObjectsByType<CelestialObject>(FindObjectsSortMode.None))
        {
            celestialObjects.Add(body);
        }
    }

    public void OnValidate()
    {
        CalculateOrbitsOfChildren();
    }

    public void Update()
    {
        CalculateOrbitsOfChildren();
    }

    public void CalculateOrbitsOfChildren()
    {
        if (Application.isPlaying)
            return;
        celestialObjects.Sort((a, b) => a.mass.CompareTo(b.mass));
        foreach (var body in celestialObjects)
        {
            if (body.orbitParent)
            {
                if (body.transform.parent == null) continue;
                var parentBody = body.transform.parent.GetComponent<CelestialObject>();
                if (parentBody == null)
                    continue;
                float distance = (body.transform.position  - parentBody.transform.position).magnitude;
                float requiredVelocity = Mathf.Sqrt((SimulatorOrchestrator.staticGC * parentBody.mass)/distance);
                body.SetNextVelocity((Vector3.Cross(body.transform.up, (body.transform.position - parentBody.transform.position).normalized) * requiredVelocity) + parentBody.nextVelocity);
                EditorUtility.SetDirty(body);

            }
        }
    }


}