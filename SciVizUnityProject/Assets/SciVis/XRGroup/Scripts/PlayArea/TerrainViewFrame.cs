using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainViewFrame : MonoBehaviour
{
    GameObject viz;
    // Start is called before the first frame update
    void Awake()
    {
        viz = GameObject.Find("Viz");
    }
    void Start()
    {
        ScaleFrame();
    }

    // Update is called once per frame
    void Update()
    {
        ScaleFrame();
    }

    private void ScaleFrame(){
        Vector3 sizeCalculated = viz.GetComponentInChildren<Renderer>().bounds.center;
        transform.position = viz.GetComponentInChildren<Renderer>().bounds.center;
        transform.localScale = sizeCalculated;
    }
}
