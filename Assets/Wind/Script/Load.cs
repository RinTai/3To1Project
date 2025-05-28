using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Load : MonoBehaviour
{

    void Update()
    {
        Renderer renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.enabled = true;
        }
    }

}
