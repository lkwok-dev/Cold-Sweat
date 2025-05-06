using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Enemy Attack Value/Fire Projectile Attack")]
public class FireProjectileAttacks : AttackMotion
{
    [SerializeField] GameObject projectile;
    [SerializeField] float fireDistance = 1;
    [SerializeField] float aimSpeed = 0.5f;
    /*
    public FireProjectileAttacks(BossEnemyController enemyController, Transform[] SP)
    {
        //enemy = enemyController;
        this.SP = SP;
    }
    */
    public override IEnumerator AttackingPlayer(BossEnemyController enemy, int leftRightHand, Transform[] SP)
    {

        while (true)
        {
            enemy.navMeshAgent.speed = enemy.speed;
            enemy.RunningAni();
            //yield return new WaitForSeconds(2f);
            //yield return null;
            Physics.Raycast(enemy.transform.position, PlayerController.puppet.cameraObj.transform.position - enemy.transform.position, out RaycastHit hit, fireDistance, ~LayerMask.GetMask("Enemy"));
            Debug.DrawRay(enemy.transform.position, PlayerController.puppet.cameraObj.transform.position - enemy.transform.position, Color.red);
            enemy.navMeshAgent.SetDestination(PlayerController.puppet.transform.position);
            if (hit.collider != null && hit.collider.tag.Equals("Player"))
            {
                //yield return null;
                break;
            }
            yield return null;
        }


        enemy.animator.SetFloat(enemy.aniLeftRightDecision, leftRightHand);
        enemy.animator.SetInteger(enemy.aniDecision, enemy.throwAni);
        enemy.animator.SetFloat("element", 1);


        //yield return new WaitForSeconds(2f);
        enemy.navMeshAgent.isStopped = true;
        //yield return new WaitForSeconds(1f);
        while (enemy.animator.GetInteger(enemy.aniDecision) == enemy.throwAni)
        {

            while (!enemy.IsPlayerWithinView(100f, 4f, 100f))
            {
                enemy.AimTowards(PlayerController.puppet.transform.position, aimSpeed);
                SP[leftRightHand].LookAt(PlayerController.puppet.cameraObj.transform.position);
                yield return null;
            }

            yield return null;
        }

        yield return null;

        enemy.navMeshAgent.isStopped = false;
        enemy.navMeshAgent.speed = enemy.speed;
        //enemy.bossState = BossState.inCombat;
        //yield return null;

        ExitRangedAttack(enemy);
    }

    public GameObject getProjectile()
    {
        return projectile;
    }
}
