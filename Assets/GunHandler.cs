using UnityEngine;
using MilanUtils;
using System.Linq;
using System.Collections.Generic;
using System;

public class GunHandler : MonoBehaviour
{
    [Tooltip("If raycasts should be used for hit detection or physical bullets")]public bool useRaycast;
    [Tooltip("The prefab of the bullet"), ShowIf("!useRaycast")]public GameObject bulletPrefab;
    [Tooltip("The velocity of the bullet"), ShowIf("!useRaycast")] public float shootVelocity;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            Transform obj = Instantiate(bulletPrefab).transform;
            obj.position = transform.position;
            obj.GetComponent<Rigidbody2D>().linearVelocity = Angle2D.GetAngleFromPos<Vector3, Vector2>(transform.position, UI.WorldMousePos()) * shootVelocity;
        }
    }
}
