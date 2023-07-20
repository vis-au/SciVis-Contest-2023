using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnapToTarget : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Vector3 targetPos = gameObject.transform.Find("Target").position;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
}
