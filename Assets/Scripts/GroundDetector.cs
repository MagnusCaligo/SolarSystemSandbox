using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GroundDetector : MonoBehaviour
{

    public onGroundTouched OnGrounded;
    public onGroundUntouched OnUngrounded;

    public Collider colliderDetector;
    private List<string> ignoredTags = new List<string>() {"Player", "GravityField"};

    // Start is called before the first frame update
    void Start()
    {
        colliderDetector = gameObject.GetComponent<Collider>();
    }

    private void OnTriggerEnter(Collider collider)
    {
        // Check to make sure we aren't colliding with the player
        if (ignoredTags.Contains(collider.tag))
            return;
        Debug.Log($"Touched Ground: {collider.name}");
        OnGrounded.Invoke();
    }

    private void OnTriggerExit(Collider collider)
    {
        if (ignoredTags.Contains(collider.tag))
            return;
        Debug.Log("UnTouched Ground");
        OnUngrounded.Invoke();
    }

    public delegate void onGroundTouched();
    public delegate void onGroundUntouched();
}
