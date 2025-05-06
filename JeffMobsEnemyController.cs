using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class JeffMobsEnemyController : BossEnemyController
{
    [Header("Self Explode/Dead System")]
    [SerializeField] private bool isAbleToExplode;
    [ToggleableVarable("isAbleToExplode")] [SerializeField] private bool isTauntAnimationOnExlpode;
    [ToggleableVarable("isAbleToExplode")] [SerializeField] private float explosionTime;
    private float explosionTimer;

    [ToggleableVarable("isAbleToExplode")] public GameObject explodeEffectPrefab;
    //GameObject to help in case the model does not have perfect transforms

    //put an empty game object in the following variable so the explosion effect can find it - Matt
    [ToggleableVarable("isAbleToExplode")] public GameObject customTransform;

    // On fresh prefabs I set health to 10 by default, but feel free to change if we have a global damage scale
    [ToggleableVarable("isAbleToExplode")] public float explosionRadius, baseDamageDealt;//, secondsUntilParticlesAreDestroyed;

    public override void OnEnable()
    {
        base.OnEnable();
        explosionTimer = explosionTime;
    }
    public override void Awake()
    {

        navMeshAgent = GetComponent<NavMeshAgent>();
        navMeshAgent.speed = speed;
        navMeshAgent.angularSpeed = angularSpeed;
        navMeshAgent.acceleration = acceleration;


        attacksManager = GetComponent<AttacksManager>();

        //healthBar = healthBarCanvasObject.GetComponentInChildren<Slider>();

        if (TryGetComponent<Animator>(out Animator thatAnimator))
        {
            animator = thatAnimator;
        }

        //HandleStateChange(state, BossState.inCombat);

        OnBossStateChange += HandleStateChange;

        //player = PlayerController.puppet;

        AnimationParameter();
    }
    // Start is called before the first frame update
    public override void Start()
    {
        //ResetHealthBar();
        
        //bossState = BossState.idle;
        bossState = BossState.meleeAttack;
        currentMovementPhase = movementPhase[0];

        //navMeshAgent.SetDestination(PlayerController.puppet.transform.position);
    }

    // Update is called once per frame
    public override void FixedUpdate()
    {
        
    }
    private void Update()
    {
        if (isAbleToExplode && bossState != BossState.dead && bossState != BossState.taunt)
        {
            if (explosionTimer < 0)
            {
                
                if (isTauntAnimationOnExlpode)
                {
                    StopCoroutine(MovementCoroutine);
                    bossState = BossState.taunt;
                }
                else
                {
                    SelfExplode();
                }

                //SelfExplode();
            }
            else
            {
                explosionTimer -= Time.deltaTime;
            }
        }
    }
    
    public override void ExitTauntState()
    {
        Debug.Log("Boom");
        SelfExplode();
    }
    

    private void SelfExplode()
    {
        /*
        if (explosionObject != null)
        {
            Instantiate(explosionObject, transform.position, transform.rotation);
            //bossState = BossState.idle;
        }
        */
        //Debug.Log("Boom");
        Explode();
        StopCoroutine(MovementCoroutine);
        Dead();
    }

    public override void Explode()
    {
        if (explodeEffectPrefab != null)
        {
            // Checks if there is a custom transform - Matt
            if (customTransform != null)
            {
                // If there is use the custom transform to spawn the particles - Matt
                GameObject destructionParticles = SpawnManager.instance.GetGameObject(explodeEffectPrefab, SpawnType.vfx);
                destructionParticles.transform.position = customTransform.transform.position;
            }
            else
            {
                GameObject destructionParticles = SpawnManager.instance.GetGameObject(explodeEffectPrefab, SpawnType.vfx);
                destructionParticles.transform.position = transform.transform.position;
            }
        }


        Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (var hC in hitColliders)
        {
            if (hC.gameObject.tag == "Player" && hC.GetComponent<PlayerPuppet>() != null)
            {
                Debug.Log(hC.gameObject.name);
                
                hC.GetComponent<PlayerPuppet>().ChangeTemperature(AbsoluteTempurature(baseDamageDealt));
                return;
            }

        }
    }

    //This is a function that deal tamperature damage on the player depend on the player's tamperature.
    //If player's tempuratrue lower than 0, it decrease player's tempuratrue.
    public float AbsoluteTempurature(float damage)
    {
        if (PlayerController.instance.temperature.stat < 0)
        {
            return -Mathf.Abs(damage);
        }
        else
        {
            return Mathf.Abs(damage);
        }
    }

    //public override void EneterNextPhase()
    //{
        
    //}
    public override void Dead()
    {
        MobSpawnerController.instance.DestroyObjectInMobSpawner(this.gameObject);
        Destroy(this.gameObject);
    }
}
