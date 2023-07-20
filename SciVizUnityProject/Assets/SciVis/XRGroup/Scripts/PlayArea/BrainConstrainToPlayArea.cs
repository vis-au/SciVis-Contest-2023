using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BrainConstrainToPlayArea : MonoBehaviour
{
    // Start is called before the first frame update
    // Y is Up, Z is back, X is side
  
    void Start()
    {

    }

    //Detect collisions between the GameObjects with Colliders attached
    void OnCollisionEnter(Collision collision)
    {
        //Debug.Log("Do something here");
        //Check for a match with the specified name on any GameObject that collides with your GameObject
        if (collision.gameObject.name == "PlayArea")
        {
            //If the GameObject's name matches the one you suggest, output this message in the console
            //Debug.Log("Do something here");
            ;
        }

        //Check for a match with the specific tag on any GameObject that collides with your GameObject
        if (collision.gameObject.tag == "PlayArea")
        {
            //If the GameObject has the same tag as specified, output this message in the console
            //Debug.Log("Do something else here");
            ;
        }
    }
}
