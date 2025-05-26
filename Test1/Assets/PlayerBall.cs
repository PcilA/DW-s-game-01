using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class PlayerBall : MonoBehaviour
{
    private float jumpPower = 3f;
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
        transform.Translate(Vector3.right * Time.deltaTime);
        transform.Translate(Vector3.left * Time.deltaTime);
        
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
        if (collision.gameObject.name == "Cube")
        { isJump = false; }
    }

}
