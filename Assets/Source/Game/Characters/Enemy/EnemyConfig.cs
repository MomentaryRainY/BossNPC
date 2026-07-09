using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Battle/Enemy Config")]
public class EnemyConfig : ScriptableObject
{
    public Enemy EnemyPrefab;
    public int MaxHealth;
    public int MoveRange;
    public float MoveSpeed = 4f;
}
