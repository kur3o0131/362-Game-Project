using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed;
    private bool isMoving;
    public Vector2 input;
    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (!isMoving)
        {
            input.x = Input.GetAxisRaw("Horizontal");
            input.y = Input.GetAxisRaw("Vertical");
            Debug.Log("This is inputX" + input.x);
            Debug.Log("This is inputY" + input.y);




            if (input.x != 0) input.y = 0;
            if (input != Vector2.zero)
            {
                animator.SetFloat("moveX", input.x);
                animator.SetFloat("moveY", input.y);
                var TargetPos = transform.position;
                TargetPos.x += input.x;
                TargetPos.y += input.y;

                StartCoroutine(Move(TargetPos));
            }
        }
        animator.SetBool("isMoving", isMoving);
    }
    IEnumerator Move(Vector3 TargetPos)
    {
        isMoving = true;
        while ((TargetPos - transform.position).sqrMagnitude > Mathf.Epsilon)
        {
            transform.position = Vector3.MoveTowards(transform.position, TargetPos, moveSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = TargetPos;
        isMoving = false;
    }

}