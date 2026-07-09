using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum UnitState
{
    Idle,
    Moving,
    Attacking,
    Dead
}

public class UnitConfig
{
    public EnemyType type;

    public UnitState DefaultState = UnitState.Idle;

    public int MoveRange;

    public float MaxHealth;

    public float MoveSpeed = 2f;
}

public class UnitRuntime
{
    public UnitConfig Config;

    public float CurrentHP;

    public Vector2Int GridPos;

    public Unit Prefab;
}

public abstract class Unit : MonoBehaviour
{
    private Vector2Int GridPos;

    public Vector2Int CurrentPos => GridPos;

    private HPBarView HPBar;

    private Animator Animator;

    public int MoveRange => runtime.Config.MoveRange;

    protected UnitRuntime runtime;

    public UnitState State{get; private set;}

    private bool attackHitNotified;

    public void OnAttackHit()
    {
        attackHitNotified = true;
    }

    public virtual void Init(Board board, UnitRuntime unitRuntime) { 
        runtime = unitRuntime;
        GridPos = runtime.GridPos;
        if (!board.TryPlaceUnit(this, GridPos)) {
            Debug.LogError($"Place unit at ({GridPos.x}, {GridPos.y}) failed");
            return;
        };
        transform.position = board.GridToWorld(runtime.GridPos);
        Animator = GetComponent<Animator>();
        snowPainter = FindFirstObjectByType<SnowDeformPainter>();
    }

    public void BindHealthBar(HPBarView view)
    {
        HPBar = view;
        HPBar.SetHealth(runtime.CurrentHP, runtime.Config.MaxHealth);
    }

    private SnowDeformPainter snowPainter;
    private Vector3 lastSnowStampPos;

    public void TryStampSnow()
    {
        if (snowPainter == null)
        {
            Debug.Log("Empty snowpainter");
            return;
        }

        if (Vector3.Distance(transform.position, lastSnowStampPos) < 0.2f)
        {
            return;
        }

        snowPainter.Stamp(transform.position, 0.02f, 0.05f);
        lastSnowStampPos = transform.position;
    }

    public IEnumerator MoveTo(Board board, Vector2Int targetCell)
    {
        State = UnitState.Moving;
        
        if (Animator != null)
        {
            Animator.SetBool("IsMoving", true);
        }

        Vector3 start = transform.position;
        Vector2Int from = board.WorldToGrid(start);

        Vector3 target = board.GridToWorld(targetCell);
        target.y = transform.position.y;

        float moveSpeed = Mathf.Max(0.1f, runtime.Config.MoveSpeed);

        while (Vector2.Distance(new Vector2(transform.position.x, transform.position.z),
            new Vector2(target.x, target.z)) > 0.05f) {
            transform.position = Vector3.MoveTowards(
                transform.position,
                target,
                moveSpeed * Time.deltaTime
            );
            TryStampSnow();
            yield return null;
        }
        
        transform.position = new Vector3(target.x, transform.position.y, target.z);

        GridPos = targetCell;
        board.MoveUnit(this, from, targetCell);

        if (Animator != null)
        {
            Animator.SetBool("IsMoving", false);
        }

        State = UnitState.Idle;
    }

    public IEnumerator PlayAttackAnimation()
    {
        if (Animator == null)
        {
            Debug.LogWarning("Animator is null.");
            yield break;
        }

        attackHitNotified = false;

        State = UnitState.Attacking;

        Animator.ResetTrigger("Attack");
        Animator.SetTrigger("Attack");

        yield return WaitForAnimatorStateEnter("Attack");

        float elapsed = 0f;
        float fallbackHitTime = 1f;

        while (!attackHitNotified && elapsed < fallbackHitTime)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    public IEnumerator WaitAttackEnd()
    {
        yield return WaitForAnimatorStateExit("Attack");
        State = UnitState.Idle;
    }

    public IEnumerator TakeDamage(float damage)
    {
        // tbd damage effect
        runtime.CurrentHP -= damage;
        HPBar.SetHealth(runtime.CurrentHP, runtime.Config.MaxHealth);

        if (Animator == null)
        {
            Debug.LogWarning("Animator is null.");
            yield break;
        }

        if (runtime.CurrentHP <= 0)
        {
            State = UnitState.Dead;

            Animator.SetTrigger("Die");
            //yield return WaitForAnimatorStateEnter("Death");
            yield return new WaitForSeconds(0.7f);

        }
        else
        {
            Animator.SetTrigger("Damaged");
            yield return WaitForAnimatorStateEnter("Damage");
            yield return new WaitForSeconds(0.2f);
        }

        yield return null;
    }

    public void TeleportTo(Board board, Vector2Int targetCell)
    {
        GridPos = targetCell;
        Vector2Int from = board.WorldToGrid(transform.position);
        transform.position = board.GridToWorld(targetCell);
        State = UnitState.Idle;
        board.MoveUnit(this, from, targetCell);
    }

    private float FacingYawOffset = 0f;
    private float TurnSpeed = 720f;

    public IEnumerator FaceCell(Vector2Int targetCell)
    {
        Vector2Int delta = targetCell - GridPos;

        if (delta == Vector2Int.zero)
        {
            yield break;
        }

        Vector3 dir = new Vector3(delta.x, 0f, delta.y).normalized;

        Quaternion targetRotation = Quaternion.LookRotation(dir, Vector3.up);
        targetRotation *= Quaternion.Euler(0f, FacingYawOffset, 0f);

        while (Quaternion.Angle(transform.rotation, targetRotation) > 1f)
        {
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                TurnSpeed * Time.deltaTime
            );

            yield return null;
        }

        transform.rotation = targetRotation;
    }

    private IEnumerator WaitForAnimatorStateEnter(string stateName, int layer = 0)
    {
        while (true)
        {
            AnimatorStateInfo current = Animator.GetCurrentAnimatorStateInfo(layer);

            if (current.IsName(stateName) && !Animator.IsInTransition(layer))
            {
                yield break;
            }

            yield return null;
        }
    }

    private IEnumerator WaitForAnimatorStateComplete(string stateName, int layer = 0)
    {
        while (true)
        {
            AnimatorStateInfo current = Animator.GetCurrentAnimatorStateInfo(layer);

            if (current.IsName(stateName) && current.normalizedTime >= 0.95f && !Animator.IsInTransition(layer))
            {
                yield break;
            }

            yield return null;
        }
    }
    private IEnumerator WaitForAnimatorStateExit(string stateName, int layer = 0)
    {
        while (true)
        {
            AnimatorStateInfo current = Animator.GetCurrentAnimatorStateInfo(layer);

            if (!current.IsName(stateName) && !Animator.IsInTransition(layer))
            {
                yield break;
            }

            yield return null;
        }
    }
}

