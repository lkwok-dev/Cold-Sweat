using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/*
// This allows us to have a single variable with both a StatType and a float for a value
[System.Serializable] public class EnemyAttackValue
{
    public int damage;
    public float coolDown;
    public float timeTaken;
    public float MinDistanceTowardPlayer;
    public float MaxDistanceTowardPlayer;
}
*/

[System.Serializable]
[CreateAssetMenu(menuName = "Enemy Attack Value")]
public class AttackMotion : ScriptableObject
{

    public int damage;
    protected Transform [] SP;

    public virtual IEnumerator AttackingPlayer(BossEnemyController enemyController, Transform[] SP)
    {
        yield return null;
    }

    public virtual IEnumerator AttackingPlayer(BossEnemyController enemyController, int i, Transform[] SP)
    {
        yield return null;
    }

    public void ExitMeleeAttack(BossEnemyController enemy)
    {
        enemy.bossState = enemy.currentMovementPhase.meleeAttackDecision.GetTheNextRandomDicision();
    }
    public void ExitRangedAttack(BossEnemyController enemy)
    {
        enemy.bossState = enemy.currentMovementPhase.rangedAttackDecision.GetTheNextRandomDicision();
    }

    public void ExitLaserAttack(BossEnemyController enemy)
    {
        enemy.bossState = enemy.currentMovementPhase.laserAttackDecision.GetTheNextRandomDicision();
    }

}


