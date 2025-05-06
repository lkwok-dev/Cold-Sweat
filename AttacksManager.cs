using System.Collections;
using System.Collections.Generic;
using UnityEngine;



[System.Serializable]
public struct EnemyAttack
{
    public AttackMotion attackMotion;
    public Transform[] spawnPoiont;
}

public class AttacksManager : MonoBehaviour
{
    public BossEnemyController enemy;
    
    public float timer;

    [SerializeField] public EnemyAttack iceMeleeAttack;
    [SerializeField] public EnemyAttack fireMeleeAttack;
    [SerializeField] public EnemyAttack iceRangedAttack;
    [SerializeField] public EnemyAttack fireRangedAttack;
    [SerializeField] public EnemyAttack iceLaserAttack;
    [SerializeField] public EnemyAttack fireLaserAttack;

    [SerializeField] public AttackDecision rangedAttackDicision;
    [SerializeField] public AttackDecision[] rangedAttackDicisionMod = new AttackDecision[4];

    [SerializeField] public AttackDecision meleeAttackDicision;
    [SerializeField] public AttackDecision[] meleeAttackDicisionMod = new AttackDecision[4];

    public int leftRightHand = 0;
    private bool ableToAttack = true;

    public void Awake()
    {
        enemy = GetComponent<BossEnemyController>();
        leftRightHand = Random.Range(0, 2);

    }
    private void Update()
    {
        if (timer > 0) timer -= Time.deltaTime;
        else timer = 0;
    }

    private int ChangeHands()
    {
        if (leftRightHand == 0)
        {
            leftRightHand = 1;
        }
        else
        {
            leftRightHand = 0;
        }
        return leftRightHand;
    }

    //Decide which element for the ranged attack
    public IEnumerator RangedAttack()
    {
        
        // Decide if it fire or ice
        if (RangedAttackDicision())
        {
            //Use Ice
            return iceRangedAttack.attackMotion.AttackingPlayer(enemy, ChangeHands(), iceRangedAttack.spawnPoiont);
        }
        else
        {
            //Use Fire
            return fireRangedAttack.attackMotion.AttackingPlayer(enemy, ChangeHands(), fireRangedAttack.spawnPoiont);
        }
        
        //StartCoroutine(enemy.MovementCoroutine);
    }

    public IEnumerator LaserAttack()
    {
        if (RangedAttackDicision())
        {
            //Use Ice
            return iceLaserAttack.attackMotion.AttackingPlayer(enemy, iceLaserAttack.spawnPoiont);
        }
        else
        {
            //Use Fire
            return fireLaserAttack.attackMotion.AttackingPlayer(enemy, fireLaserAttack.spawnPoiont);
        }

    }

    //Decide which element for the melee attack
    public IEnumerator MeleeAttack()
    {
        // Decide if it fire or ice
        if (MeleeAttackDicision())
        {
            //Use Ice
            return iceMeleeAttack.attackMotion.AttackingPlayer(enemy, ChangeHands(), iceMeleeAttack.spawnPoiont);
        }
        else
        {
            //Use Fire
            return fireMeleeAttack.attackMotion.AttackingPlayer(enemy, ChangeHands(), fireMeleeAttack.spawnPoiont);
        }
    }
    
    //This output a bool (true is ice/ false is fire) by calculate the element needed to use using the decision and decision modifier during range attack.
    public bool RangedAttackDicision()
    {
        
        AttackDecision temp = new AttackDecision(rangedAttackDicision);

        // Adds up all the modifier and calculate the weight of each elements for the ranged attack.
        if (PlayerController.instance.temperature.stat >= 75)
        {
            temp.AddDicision(rangedAttackDicisionMod[0]);
        }
        if (PlayerController.instance.temperature.stat >= 60)
        {
            temp.AddDicision(rangedAttackDicisionMod[1]);
        }
        if (PlayerController.instance.temperature.stat <= 40)
        {
            temp.AddDicision(rangedAttackDicisionMod[2]);
        }
        if (PlayerController.instance.temperature.stat <= 25)
        {
            temp.AddDicision(rangedAttackDicisionMod[3]);
        }
        //Find which element for the next attack
        return temp.GiveTheNextRandomDicision(); 
        
        //return rangedAttackDicision.GiveTheNextRandomDicision();
    }

    //This output a bool (true is ice/ false is fire) by calculate the element needed to use using the decision and decision modifier during melee attack.
    public bool MeleeAttackDicision()
    {
        
        AttackDecision temp = new AttackDecision(meleeAttackDicision);

        // Adds up all the modifier and calculate the weight of each elements for the melee attack.
        if (PlayerController.instance.temperature.stat >= 75)
        {
            temp.AddDicision(meleeAttackDicisionMod[0]);
        }
        if (PlayerController.instance.temperature.stat >= 60)
        {
            temp.AddDicision(meleeAttackDicisionMod[1]);
        }
        if (PlayerController.instance.temperature.stat <= 40)
        {
            temp.AddDicision(meleeAttackDicisionMod[2]);
        }
        if (PlayerController.instance.temperature.stat <= 25)
        {
            temp.AddDicision(meleeAttackDicisionMod[3]);
        }

        //Find which element for the next attack
        return temp.GiveTheNextRandomDicision(); 
        
        //return meleeAttackDicision.GiveTheNextRandomDicision();
    }

    public void SetMeleeHitBoxActive()
    {
        if (enemy.animator.GetFloat("element") == 0)
        {
            iceMeleeAttack.spawnPoiont[leftRightHand].gameObject.SetActive(true);
            iceMeleeAttack.spawnPoiont[leftRightHand + 2].gameObject.SetActive(true);
        }
        else
        {
            fireMeleeAttack.spawnPoiont[leftRightHand].gameObject.SetActive(true);
            fireMeleeAttack.spawnPoiont[leftRightHand + 2].gameObject.SetActive(true);
        }
    }
    public void SetMeleeHitBoxInactive()
    {
        if (enemy.animator.GetFloat("element") == 0)
        {
            iceMeleeAttack.spawnPoiont[leftRightHand].gameObject.SetActive(false);
            iceMeleeAttack.spawnPoiont[leftRightHand + 2].gameObject.SetActive(false);
        }
        else
        {
            fireMeleeAttack.spawnPoiont[leftRightHand].gameObject.SetActive(false);
            fireMeleeAttack.spawnPoiont[leftRightHand + 2].gameObject.SetActive(false);
        }
    }
    public void SetMeleeVFXActive()
    {
        if (enemy.animator.GetFloat("element") == 0)
        {
            iceMeleeAttack.spawnPoiont[leftRightHand + 2].gameObject.SetActive(true);
        }
        else
        {
            fireMeleeAttack.spawnPoiont[leftRightHand + 2].gameObject.SetActive(true);
        }
    }

    public void FireAProjectile()
    {
        
        if (enemy.animator.GetFloat("element") == 0)
        {
            GameObject thisProjectile1 = SpawnManager.instance.GetGameObject(((IceProjectileAttacks)iceRangedAttack.attackMotion).getProjectile(), SpawnType.projectile);
            if (thisProjectile1.TryGetComponent<ProjectileController>(out ProjectileController projectileController))
            {
                projectileController.transform.position = iceRangedAttack.spawnPoiont[leftRightHand].transform.position;
                projectileController.transform.rotation = iceRangedAttack.spawnPoiont[leftRightHand].transform.rotation;
                projectileController.LaunchProjectile();
            }
        }
        else
        {
            GameObject thisProjectile1 = SpawnManager.instance.GetGameObject(((FireProjectileAttacks)fireRangedAttack.attackMotion).getProjectile(), SpawnType.projectile);
            if (thisProjectile1.TryGetComponent<ProjectileController>(out ProjectileController projectileController))
            {
                projectileController.transform.position = fireRangedAttack.spawnPoiont[leftRightHand].transform.position;
                projectileController.transform.rotation = fireRangedAttack.spawnPoiont[leftRightHand].transform.rotation;
                projectileController.LaunchProjectile();
            }
        }
        
    }

    public void SetLaser()
    {
        if (enemy.animator.GetFloat("element") == 0)
        {
            iceMeleeAttack.spawnPoiont[0].gameObject.SetActive(iceMeleeAttack.spawnPoiont[0].gameObject.activeSelf ? false : true);
        }
        else
        {
            fireMeleeAttack.spawnPoiont[0].gameObject.SetActive(iceMeleeAttack.spawnPoiont[0].gameObject.activeSelf ? false : true);
        }
    }


    public void ChangeLaserAniState()
    {
        if (enemy.animator.GetInteger(enemy.aniLaserState) == 3)
        {
            enemy.animator.SetInteger(enemy.aniLaserState, 0);
            ExitAttackAnimation();
        }
        else
        {
            enemy.animator.SetInteger(enemy.aniLaserState, enemy.animator.GetInteger(enemy.aniLaserState) + 1);
        }
    }
    public void ExitAttackAnimation()
    {
        enemy.IdleAni();
    }
}
