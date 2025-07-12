using NUnit.Framework;
using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SimulatorOrchestrator : MonoBehaviour
{

    [HideInInspector]
    public List<CelestialObject> celestialObjects;
    [HideInInspector]
    public List<CustomRigidBody> customObjects;
    [Header("Simulation Variables")]
    public bool runSimulation = false;
    public float timeDelta = 0.01f;
    public float GravitationalConstant;
    public StarController starController;

    [Header("Center of the Universe")]
    public CustomRigidBody centerOfTheUniverse;
    public float centerOfTheUniverseThreshold = 10.0f;

    public static float staticGC;
    public static float staticTimeDelta;
    public static SimulatorOrchestrator instance;

    private float accumulatedTime = 0f;

    public void Start()
    {
        if (instance == null) 
            instance = this;
        celestialObjects = new List<CelestialObject>();
        foreach (CelestialObject body in GameObject.FindObjectsByType<CelestialObject>(FindObjectsSortMode.None))
        {
            celestialObjects.Add(body); 
        }
        customObjects = new List<CustomRigidBody>();
        foreach (CustomRigidBody body in GameObject.FindObjectsByType<CustomRigidBody>(FindObjectsSortMode.None))
        {
            customObjects.Add(body); 
        }

        staticGC = GravitationalConstant;
    }

    public void OnValidate()
    {
        staticGC = GravitationalConstant;
        staticTimeDelta = timeDelta;
    }

    public void CalculateNewVelocities()
    {
       foreach (var body in celestialObjects)
        {
            body.UpdateVelocity(celestialObjects, timeDelta);
        }

       foreach (var body in customObjects)
        {
            body.calculateVelocity(timeDelta);
        }
    }

    public void checkCenterOfTheUniverseOffset()
    {

        if (centerOfTheUniverse == null) return;
        if (centerOfTheUniverse.transform.position.magnitude < centerOfTheUniverseThreshold) return;

        Vector3 offset = centerOfTheUniverse.transform.position;

        foreach (var obj in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            obj.transform.position -= offset;
        }

        return;

    }

    public void Update()
    {
        if (!runSimulation) return;

        accumulatedTime += Time.unscaledDeltaTime;
        if (accumulatedTime < timeDelta)
            return;

        CalculateNewVelocities();

        foreach (var body in celestialObjects)
         {
             body.UpdatePosition(timeDelta);
         }

        foreach (var body in customObjects)
         {
             body.UpdatePosition();
         }

        checkCenterOfTheUniverseOffset();
        Physics.Simulate(timeDelta);
        if (starController != null)
            starController.UpdatePosition();
        accumulatedTime -= timeDelta;

        
    }

}