using System.ComponentModel;
using UnityEngine;

public class StarController : MonoBehaviour
{
    public GameObject followObject;
    void Start()
    {

        if (followObject == null)
            throw new System.Exception("Need to set a follow object");
        
    }

    // Update is called once per frame
    public void UpdatePosition()
    {
        transform.position = followObject.transform.position;
        
    }
}
