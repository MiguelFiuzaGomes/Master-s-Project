using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class ViewerMovement : MonoBehaviour
{
    // Camera
    public float sensitivity;
    public Transform orientation;
    float xRotation;
    float yRotation;
    
    // Player
    public float moveSpeed;
    private float horizontalInput;
    private float verticalInput;
    private float upInput;

    private Rigidbody rb;
    private Vector3 moveDirection;
    
    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * Time.deltaTime * sensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * Time.deltaTime * sensitivity;
        
        yRotation += mouseX;
        xRotation -= mouseY;
        
        xRotation = Mathf.Clamp(xRotation, -90, 90);
        
        transform.rotation = Quaternion.Euler(xRotation, yRotation, 0f);
        orientation.rotation = Quaternion.Euler(0f, yRotation, 0f); 
        
        MyInput();
    }

    private void FixedUpdate()
    {
        MovePlayer();
    }

    private void MyInput()
    {
        horizontalInput = Input.GetAxis("Horizontal");
        verticalInput = Input.GetAxis("Vertical");
        upInput = Input.GetAxis("Jump");
    }

    private void MovePlayer()
    {
        moveDirection = orientation.forward * verticalInput * moveSpeed + orientation.right * horizontalInput * moveSpeed + orientation.up * upInput * moveSpeed;
        rb.AddForce(moveDirection, ForceMode.Force);
    }


 

}
