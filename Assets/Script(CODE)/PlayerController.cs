using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed;
    public LayerMask solidObjectsLayer; // 新增：用來設定什麼是"牆壁"

    private bool isMoving;
    private Vector2 input;
    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void Update()
    {
        if (!isMoving)
        {
            input.x = Input.GetAxisRaw("Horizontal");
            input.y = Input.GetAxisRaw("Vertical");

            // 禁止斜向移動 (保留原本邏輯)
            if (input.x != 0) input.y = 0;

            if (input != Vector2.zero)
            {
                animator.SetFloat("moveX", input.x);
                animator.SetFloat("moveY", input.y);

                var targetPos = transform.position;
                targetPos.x += input.x;
                targetPos.y += input.y;

                // === 修改重點：移動前先檢查目標位置有沒有障礙物 ===
                if (IsWalkable(targetPos))
                {
                    StartCoroutine(Move(targetPos));
                }
            }
        }
        animator.SetBool("isMoving", isMoving);
    }

    // 新增：檢查目標位置是否可以行走
    private bool IsWalkable(Vector3 targetPos)
    {
        // 發射一個小小的圓形偵測，看有沒有撞到設定為 solidObjectsLayer 的東西
        if (Physics2D.OverlapCircle(targetPos, 0.2f, solidObjectsLayer) != null)
        {
            return false; // 有撞到東西，不能走
        }
        return true; // 沒撞到東西，可以走
    }

    IEnumerator Move(Vector3 targetPos)
    {
        isMoving = true;

        while ((targetPos - transform.position).sqrMagnitude > Mathf.Epsilon)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = targetPos;

        isMoving = false;
    }
}