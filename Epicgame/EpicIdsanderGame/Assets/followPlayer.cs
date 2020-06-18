using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class followPlayer : MonoBehaviour
{
    public Transform player;
    public Transform cameralocation;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 hello = new Vector3(0.0f, 0.0f, -1f);
        cameralocation.position = player.position + hello;
    }
}
