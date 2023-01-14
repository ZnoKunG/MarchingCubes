using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    Rigidbody rb;
    public float velocity;
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        transform.rotation = Quaternion.Euler(Vector3.zero);
    }
    public void Update()
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");

        rb.velocity = new Vector3(horizontalInput, 0, verticalInput) * velocity;
    }
}
