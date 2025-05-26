using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBall : MonoBehaviour
{
    public int afterbringbanbang;
    private int modee;
    public float jumpPower;
    bool isJump;
    Rigidbody rigid;
    
    void Awake()
    {  
        isJump = false;
        rigid = GetComponent<Rigidbody>(); }

    private void FixedUpdate()
    {
        float h = Input.GetAxisRaw("Horizontal") / 2;
        float v = Input.GetAxisRaw("Vertical") / 2;
        rigid.AddForce(new Vector3(h, 0, v), ForceMode.Impulse);
    }

    private void Update()
    {
        if (Input.GetButtonDown("Jump") && !isJump )
        { isJump = true;
            rigid.AddForce(new Vector3(0, jumpPower, 0), ForceMode.Impulse); }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.name == "Plane")
        { isJump=false;}
    }

}
