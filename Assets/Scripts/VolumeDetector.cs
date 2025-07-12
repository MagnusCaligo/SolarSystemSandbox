using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;

public class VolumeDetector : MonoBehaviour
{
    public float upRightDuration = 1.0f;
    public float upRightSpeed = 0.05f;

    private CustomRigidBody body;

    private List<Collider> trackedGravityVolumes;
    private float entryTime = 0.0f;
    private Vector3 entryDirection;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

        body = GetComponent<CustomRigidBody>();
        if(body == null )
            body = GetComponentInParent<CustomRigidBody>();
        if (body == null)
            throw new System.Exception("Volume Detector not connected to any RigidBody");

        trackedGravityVolumes = new List<Collider>();
    }

    // Update is called once per frame
    void Update()
    {
        if (trackedGravityVolumes == null) return;
        if (trackedGravityVolumes.Count > 0)
        {
            Vector3 pos = trackedGravityVolumes.ElementAt(trackedGravityVolumes.Count - 1).transform.position;
            Vector3 direction = transform.position - pos;
            Vector3 cross = Vector3.Cross(transform.right, direction);

            if (entryTime == 0.0)
            {
                entryTime = Time.time;
                entryDirection = transform.root.eulerAngles;
            }

            Quaternion lookDirection = Quaternion.LookRotation(cross, direction);

            transform.rotation = Quaternion.RotateTowards(transform.rotation, lookDirection, upRightSpeed);
        }
        
    }

    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("GravityField"))
        {
            if (!trackedGravityVolumes.Contains(other))
                trackedGravityVolumes.Add(other);
            if (trackedGravityVolumes.Count == 0)
                gameObject.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
        }
    }

    public void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("GravityField")) 
        { 
            if (trackedGravityVolumes.Contains(other))
                trackedGravityVolumes.Remove(other);
        }
        
    }
}
