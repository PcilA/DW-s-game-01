using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBall : MonoBehaviour
{
    public float jumpPower;
    bool isJump;
    Rigidbody rigid;
    
    void Awake()
    {  
        rigid = GetComponent<Rigidbody>(); }

    private void FixedUpdate()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        rigid.AddForce(new Vector3(h, 0, v), ForceMode.Impulse);
    }

    private void Update()
    {
        if (Input.GetButtonDown("Jump"))
        { rigid.AddForce(new Vector3(0, jumpPower, 0), ForceMode.Impulse); }
    }


}
