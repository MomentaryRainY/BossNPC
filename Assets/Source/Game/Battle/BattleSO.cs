using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Battle/Battle Config")]
public class BattleConfig : ScriptableObject
{
    public Unit PlayerPrefab;
    public Vector2Int PlayerStartPos;
    public int PlayerMaxHealth;
    public int PlayerMoveRange;
    public float PlayerMoveSpeed = 2f;
    public int MaxStamina;
    public int MaxHandCount;

    public List<EnemySpawnConfig> Enemies;

    public bool UseTransitionAfterBattle;
    public bool ShowLevelUpPage;
    public bool ShowRewardCardPage;
    public CardData RewardCard;
}

[System.Serializable]
public class EnemySpawnConfig
{
    public EnemyConfig Config;
    public Vector2Int GridPos;

}