
using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.VFX;
using System.Collections.Generic;
using UnityEngine.Events;

public enum BossState
{
    idle = 0,
    taunt = 1,
    meleeAttack = 2,
    rangedAttack = 3,
    takingCover = 4,
    waitingInCover = 5,
    teleportToCover = 6,
    teleportBehindPlayer = 7,
    laserAttack = 8,
    spawnTurrets = 9,
    spawnMines = 10,
    orbWalking = 11,
    spawnMobs = 12,
    ambushed = 13,
    dead = 14,
    setArmor = 15,
}


public class BossEnemyController : EnemyController
{
    [Header("Canvas Syatem")]
    [SerializeField] private GameObject healthBarCanvasObject;

    [SerializeField] private GameObject healthBar;
    private StatSliderController healthBarController;

    [SerializeField] private GameObject iceArmorHealthBar;
    private StatSliderController iceArmorHealthBarController;

    [SerializeField] private GameObject fireArmorHealthBar;
    private StatSliderController fireArmorHealthBarController;

    //Boss activation system
    private bool isPlayerReachingBoss;
    [SerializeField] private float bossActivateDistance;


    // Boss mob spawn system
    [SerializeField] private GameObject mobSpawner;
    private MobSpawnerController mobSpawnerController;

    [Header("Boss Stats")]
    public NavMeshAgent navMeshAgent;
    [HideInInspector] public Animator animator;
    [HideInInspector] protected AttacksManager attacksManager;

    [SerializeField] public float speed = 3.5f;
    [SerializeField] public float acceleration = 8f;
    [SerializeField] public float angularSpeed = 120f;
    
    
    [SerializeField] public GameObject viewPoint; // The starting point of the enemy view point
    [SerializeField] public float viewDegreeH = 100; // The Horizontal angle where the enemy can see the player
    [SerializeField] public float viewDegreeV = 50; // The Vertical angle where the enemy can see the player
    [SerializeField] public float viewRange = 10; // The distance that the enemy can see the player


    [Header("Boss Phase Setting")]

    public MovementPhase[] movementPhase;
    private MovementPhase phase;
    [HideInInspector]
    public MovementPhase currentMovementPhase
    {
        get
        {
            return phase;
        }
        set
        {
            if (phase != value)
            {
                if (value.isArmored)
                {
                    currentArmorHealth.stat = value.armorStats.stat;
                    currentArmorHealth.maximum = value.armorStats.maximum;
                    currentArmorHealth.minimum = value.armorStats.minimum;

                    currenthPReductionPercentOnArmorBreak = value.hPReductionPercentOnArmorBreak;
                    SetArmor(value.startingArmorType);
                }
                else
                {
                    SetArmor(DamageType.nuetral);
                }
            }
            phase = value;
        }
    }

    [Header("Armor Syatem")]
    public DamageType currentArmorElementType;

    public GameObject iceArmorObject;
    public GameObject fireArmorObject;
    public IndividualStat currentArmorHealth;
    public float currenthPReductionPercentOnArmorBreak;


    public VFXGameObject iceArmorFormedVFX;
    public VFXGameObject fireArmorFormedVFX;

    public VFXGameObject iceArmorDestroyedVFX;
    public VFXGameObject fireArmorDestroyedVFX;


    [Header("Orbwalk System")]

    public float orbWalkSpeed;
    public float orbWalkAcceleration;
    public float turnSpeed;
    private Quaternion rotateGoalWithOutY;
    private Vector3 destination;

    [Range(0.1f, 10)] public float navMeshDetactionRadius;
    [Range(0.1f, 10)] public float navMeshDetactionDistance;

    public float closestStoppingDistance;
    public float farthestStoppingDistance;

    public float minEndOrbWalkingTime;
    public float maxEndOrbWalkingTime;
    private float orbWalkingTime;

    private float endOrbWalkingTimer;
    public Vector3 moveForwardVector;
    public Vector3 moveMidRangeVector;
    public Vector3 moveBackwardVector;
    public Vector3 moveVector;


    [Header("Boss Covering System")]

    [SerializeField] public float widthOfTheBoss;
    public LayerMask hidingSpotLayer;
    public LayerMask ignoreLayer;

    public float playerSpottedDistance;
    public float minWaitTimeinCover;
    public float maxWaitTimeInCover;

    [Tooltip("Will not seek cover at any points within this range of the player")] public float playerTooCloseDistanceToCover = 4f;

    [Range(.001f, 5)] [Tooltip("Interval in seconds for the enemy to check for new hiding spots")] public float coverUpdateFrequency = .75f;

    [SerializeField] public float coverSampleDistance; // Make it double the length of the longest length within the area

    private bool isCoverPointResetNeeded = true;

    private Collider nextCol;
    private Vector3 previousCoverPoint;


    [Header("Boss Teleport System")]
    public VFXGameObject teleportVFX;
    [SerializeField] public float teleportSampleDistance;
    public float playerTooCloseDistanceToTeleport = 4f;

    [Header("Boss Teleport Out At Certain Health System")]
    public bool isBossTPOutToNextScene;
    [ToggleableVarable("isBossTPOutToNextScene")] public float hPPercetageToTP;
    [ToggleableVarable("isBossTPOutToNextScene")] public GameObject bossEndSceneGameObject;
    


    [Header("Animation Setting")]
    [Range(.5f, 5)] public float orbWalkAniSpeed;
    [Range(.5f, 5)] public float runAniSpeed;
    public bool isAbleToPlayDeathAni;
    public float activationTimeAfterDeath;

    [Header("SFX")]
    public AudioSource ourAudioSource;
    public AudioClip meleeBugle;
    public AudioClip projectileBugle;
    public AudioClip laserBugle;
    public AudioClip tauntBugle;
    public AudioClip teleportBugle;


    //Animation Parameter
    [HideInInspector] public string aniDecision = "idleDecision";
    [HideInInspector] public int idleAni , runningAni, walkAni, tauntAni, throwAni, meleeAni, laserAni;

    [HideInInspector] public string aniLeftRightDecision;
    [HideInInspector] public string aniForwardBackDecision;
    [HideInInspector] public string aniElementDecision;
    [HideInInspector] public string aniLaserState;
    [HideInInspector] public string aniDeathDecision;

    public BossState state = BossState.idle;
    
    private BossState tempState;
    public BossState bossState // Public boss state variable that can be set to trigger a clean state transition
    {
        get
        {
            return state;
        }
        set
        {
            tempState = state;
            state = value;
            OnBossStateChange?.Invoke(tempState, state);

        }
    }
    public delegate void OnStateChange(BossState oldState, BossState newState);
    public OnStateChange OnBossStateChange;

    // Coroutine variables
    public IEnumerator MovementCoroutine;
    public virtual void Awake()
    {
        
        navMeshAgent = GetComponent<NavMeshAgent>();
        navMeshAgent.speed = speed;
        navMeshAgent.angularSpeed = angularSpeed;
        navMeshAgent.acceleration = acceleration;


        attacksManager = GetComponent<AttacksManager>();

        mobSpawnerController = mobSpawner.GetComponent<MobSpawnerController>();

        

        if (TryGetComponent<Animator>(out Animator thatAnimator))
        {
            animator = thatAnimator;
        }

        OnBossStateChange += HandleStateChange;

        //Health and Armror UI

        healthBarController = healthBar.GetComponent<StatSliderController>();
        iceArmorHealthBarController = iceArmorHealthBar.GetComponent<StatSliderController>();
        fireArmorHealthBarController = fireArmorHealthBar.GetComponent<StatSliderController>();


        AnimationParameter();

        isPlayerReachingBoss = true;
    }


    public void AnimationParameter()
    {
        //Animation Uses
        aniDecision = "idleDecision";
        idleAni = 0;
        runningAni = 1;
        walkAni = 2;
        tauntAni = 3;
        throwAni = 4;
        meleeAni = 5;
        laserAni = 6;

        aniLeftRightDecision = "LeftRight";
        aniForwardBackDecision = "ForwardBackward";
        aniElementDecision = "element";
        aniLaserState = "laserState";
        aniDeathDecision = "isDead";

    }


    public virtual void Start()
    {
        healthBarController.ResetAllValue(health);
        bossState = BossState.idle;

        currentMovementPhase = movementPhase[0];

        if (isBossTPOutToNextScene)
        {
            bossEndSceneGameObject.SetActive(false);
        }

    }

    public virtual void FixedUpdate()
    {
        
        if (IsPlayerWithinDistance(bossActivateDistance) && isPlayerReachingBoss)
        {
            bossState = BossState.meleeAttack;

            isPlayerReachingBoss = false;
        }
        
        BossStageInteraction();


    }
    public void BossStageInteraction()
    {
        float healthPercentage = ((health.stat - health.minimum) / health.maximum) * 100;


        for (int i = movementPhase.Length - 1; i >= 0 ; i--)
        {
            if (healthPercentage < movementPhase[i].hPPercentageToEnterPhase)
            {
                currentMovementPhase = movementPhase[i];
                break;
            }
        }
    }

    public void HandleStateChange(BossState oldState, BossState newState) // Standard handler for boss states and transitions
    {
        if (MovementCoroutine != null)
        {
            StopCoroutine(MovementCoroutine);
        }
        switch (newState)
        {

            case BossState.idle:
                break;
            case BossState.taunt:
                ;
                MovementCoroutine = TauntState();
                break;
            case BossState.meleeAttack:
                MovementCoroutine = attacksManager.MeleeAttack();
                break;
            case BossState.rangedAttack:
                MovementCoroutine = attacksManager.RangedAttack();
                break;
            case BossState.takingCover:
                MovementCoroutine = TakeCoverState(PlayerController.puppet.transform);
                break;
            case BossState.waitingInCover:
                MovementCoroutine = WaitInCoverState(UnityEngine.Random.Range(minWaitTimeinCover, maxWaitTimeInCover));
                break;
            case BossState.teleportToCover:
                MovementCoroutine = TeleportingToCoverState(PlayerController.puppet.transform);
                break;
            case BossState.teleportBehindPlayer:
                MovementCoroutine = TeleportingBehindState(PlayerController.puppet.transform);
                break;
            case BossState.laserAttack:
                MovementCoroutine = attacksManager.LaserAttack();
                break;
            case BossState.spawnTurrets:
                MovementCoroutine = SpawnTurretsState();
                break;
            case BossState.spawnMines:
                MovementCoroutine = SpawnMinesState();
                break;
            case BossState.spawnMobs:
                MovementCoroutine = SpawnMobsState();
                break;
            case BossState.orbWalking:
                MovementCoroutine = OrbWalkState();
                break;
            case BossState.dead:
                MovementCoroutine = DeadState();
                break;
            case BossState.setArmor:
                MovementCoroutine = ArmorState(currentMovementPhase.startingArmorType);
                break;
            default:
                break;
        }
        if (MovementCoroutine != null)
        {
            StartCoroutine(MovementCoroutine);
        }

        ShowRayOnCheckHidingSpot();
    }

    private void Update()
    {
        AniSpeed();
        CanvasPointAtPlayer();
        ResetTheValueInCanvas();
    }

    public void ShowRayOnCheckHidingSpot()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, coverSampleDistance, hidingSpotLayer);
        foreach (Collider i in hitColliders)
        {
            IsItAValidHidingPoint(widthOfTheBoss, i.transform.position);
        }
    }

    //Animation speed for walking and running
    private void AniSpeed()
    {
        if (animator.GetInteger(aniDecision) == runningAni)
        {
            animator.speed = runAniSpeed;
        }
        else if (animator.GetInteger(aniDecision) == walkAni)
        {
            animator.speed = orbWalkAniSpeed;
        }
        else
        {
            animator.speed = 1;
        }
    }

    //Make the canvas point at player
    public void CanvasPointAtPlayer()
    {
        Vector3 temp = new Vector3(PlayerController.puppet.cameraObj.transform.position.x, healthBarCanvasObject.transform.position.y, PlayerController.puppet.cameraObj.transform.position.z);
        healthBarCanvasObject.transform.LookAt(temp);
    }


    private void ResetTheValueInCanvas()
    {
        healthBarController.ResetValue(health.stat);
        if (currentArmorElementType == DamageType.ice && iceArmorHealthBar.activeSelf)
        {
            iceArmorHealthBarController.ResetValue(currentArmorHealth.stat);
        }
        if (currentArmorElementType == DamageType.fire && fireArmorHealthBar.activeSelf)
        {
            fireArmorHealthBarController.ResetValue(currentArmorHealth.stat);
        }
        //healthBar.value = health.stat;
    }

    public void IdleAni()
    {
        animator.SetInteger(aniDecision, idleAni);
    }
    public void RunningAni()
    {
        animator.SetInteger(aniDecision, runningAni);
    }


    public IEnumerator TauntState()
    {
        navMeshAgent.speed = 0;
        animator.SetInteger(aniDecision, tauntAni);
        while (animator.GetInteger(aniDecision) == tauntAni)
        {
            Debug.Log(animator.GetInteger(aniDecision));
            yield return null;
        }
        
        ExitTauntState();
        yield return null;
    }

    //Change the state of Taunt
    public virtual void ExitTauntState()
    {
        bossState = currentMovementPhase.tauntAttackDecision.GetTheNextRandomDicision();
    }


    public void EndTauntStateAni()
    {
        IdleAni();
    }

    public IEnumerator ArmorState(DamageType elementArmor)
    {
        SetArmor(elementArmor);
        ExitArmorState();
        yield return null;

    }

    public void SetArmor(DamageType elementArmor)
    {
        switch (elementArmor)
        {
            case DamageType.ice:
                EnterIceArmorState();
                break;

            case DamageType.fire:
                EnterFireArmorState();
                break;

            case DamageType.nuetral:
                EnterNoArmorState();
                break;
        }
    }
    public virtual void ExitArmorState()
    {
        bossState = currentMovementPhase.getArmor.GetTheNextRandomDicision();
    }

    private void ChangeRandomElementState()
    {
        if (Random.Range(0 , 2) == 0)
        {
            EnterFireArmorState();
        }
        else
        {
            EnterIceArmorState();
        }
    }
    //Change to the opposite element for the armor
    private void ChangeElementState()
    {
        if (currentArmorElementType == DamageType.ice)
        {
            EnterFireArmorState();
        }
        else if (currentArmorElementType == DamageType.fire)
        {
            EnterIceArmorState();
        }
    }
    private void EnterIceArmorState()
    {
        WearIceArmor();
        SetArmorBarActive(true, false);
        iceArmorHealthBarController.ResetAllValue(currentArmorHealth);
        currentArmorHealth.stat = currentArmorHealth.maximum;
    }

    private void WearIceArmor()
    {
        currentArmorElementType = DamageType.ice;
        iceArmorObject.SetActive(true);
        fireArmorObject.SetActive(false);
        SpawnManager.SpawnVFX(iceArmorFormedVFX.vFX, this.transform, iceArmorFormedVFX.offset);

        ChangeDamageInteraction(DamageType.ice, DamageInteraction.immune);
        ChangeDamageInteraction(DamageType.fire, DamageInteraction.nuetral);
    }

    private void EnterFireArmorState()
    {
        WearFireArmor();
        SetArmorBarActive(false, true);
        fireArmorHealthBarController.ResetAllValue(currentArmorHealth);
        currentArmorHealth.stat = currentArmorHealth.maximum;
    }

    private void WearFireArmor()
    {
        currentArmorElementType = DamageType.fire;
        fireArmorObject.SetActive(true);
        iceArmorObject.SetActive(false);
        SpawnManager.SpawnVFX(fireArmorFormedVFX.vFX, this.transform, fireArmorFormedVFX.offset);

        ChangeDamageInteraction(DamageType.ice, DamageInteraction.nuetral);
        ChangeDamageInteraction(DamageType.fire, DamageInteraction.immune);
    }

    private void EnterNoArmorState()
    {
        WearNoArmor();
        SetArmorBarActive(false, false);
        currentArmorHealth.stat = currentArmorHealth.minimum;
    }

    private void WearNoArmor()
    {
        currentArmorElementType = DamageType.nuetral;
        fireArmorObject.SetActive(false);
        iceArmorObject.SetActive(false);

        ChangeDamageInteraction(DamageType.ice, DamageInteraction.nuetral);
        ChangeDamageInteraction(DamageType.fire, DamageInteraction.nuetral);
    }

    private void SetArmorBarActive(bool iceArmor, bool fireArmor)
    {
        if (iceArmorHealthBar != null)
        {
            iceArmorHealthBar.SetActive(iceArmor);
        }
        if (fireArmorHealthBar != null)
        {
            fireArmorHealthBar.SetActive(fireArmor);
        }
    }

    private IEnumerator TakeCoverState(Transform target)
    {
        navMeshAgent.speed = speed;
        RunningAni();
        WaitForSeconds wait = new WaitForSeconds(coverUpdateFrequency);
        
        while (true)
        {
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, coverSampleDistance, hidingSpotLayer);

            if (hitColliders.Length == 0)
            {
                Debug.Log("Unable to find cover");
            }
            else
            {
                
                if (nextCol != null)
                {
                    if (isCoverPointResetNeeded)
                    {
                        previousCoverPoint = nextCol.transform.position;
                        isCoverPointResetNeeded = false;
                    }
                }
                
                //Find the right collider to hide
                nextCol = FindClosestValidHidingSpot(target, hitColliders);

                if (nextCol == null)
                {
                    Debug.Log("No valid cover spot");

                }
                else
                {
                    
                    navMeshAgent.SetDestination(nextCol.transform.position);
                    
                    if (previousCoverPoint != nextCol.transform.position)
                    {
                        isCoverPointResetNeeded = true;
                    }
                    
                }
            }


            if (transform.position.x == navMeshAgent.destination.x && transform.position.z == navMeshAgent.destination.z)
            {
                isCoverPointResetNeeded = true;
                bossState = BossState.waitingInCover;
            }

            yield return wait;
        }
    }
    //Find the closest hiding spot that is valid
    public Collider FindClosestValidHidingSpot(Transform target, Collider[] colliders)
    {
        Collider tempCol = null;
        List<Collider> colList = new List<Collider>();
        foreach (Collider thisCol in colliders)
        {
            //Ignore the previous point that is being used as hiding
            if (previousCoverPoint == thisCol.transform.position && isCoverPointResetNeeded)
            {
                continue;
            }

            //Check if the hiding spot can be hide from the player
            if (!IsItAValidHidingPoint(widthOfTheBoss, thisCol.transform.position))
            {
                continue;
            }

            //Ignore the hiding point that is too close to the player
            if (Vector3.Distance(target.position, thisCol.transform.position) < playerTooCloseDistanceToCover)
            {
                continue;
            }

            //Find which collider is the closest
            if (tempCol == null)
            {
                tempCol = thisCol;
            }
            else if (Vector3.Distance(thisCol.transform.position, target.position) < Vector3.Distance(tempCol.transform.position, target.position))
            {
                tempCol = thisCol;
            }
        }
        return tempCol;
    }

    //Find a random hiding spot that is valid
    public Collider FindRandomValidHidingSpot(Transform target, Collider[] colliders)
    {
        List<Collider> colList = new List<Collider>();
        foreach (Collider thisCol in colliders)
        {
            //Ignore the previous point that is being used as hiding
            if (previousCoverPoint == thisCol.transform.position)
            {
                continue;
            }

            //Check if the hiding spot can be hide from the player
            if (!IsItAValidHidingPoint(widthOfTheBoss, thisCol.transform.position))
            {
                continue;
            }

            //Ignore the hiding point that is too close to the player
            if (Vector3.Distance(target.position, thisCol.transform.position) < playerTooCloseDistanceToCover)
            {
                continue;
            }

            colList.Add(thisCol);

        }

        // Return the random hiding spot
        Collider outputCollider;
        if (colList.Count > 0)
        {
            int randomIndex = Random.Range(0, colList.Count);
            outputCollider = colList[randomIndex];
        }
        else
        {
            outputCollider = FindClosestValidHidingSpot(target, colliders);
        }
        
        return outputCollider;
    }


    //Check if the hiding spot can be hide from the player. Size is the current object size. Position is the current position that is tried to hide.
    public bool IsItAValidHidingPoint(float size, Vector3 position)
    {
        //Find the two points that is between the boss while also perpendicular to the player
        Vector3 vectorToColloder = Camera.main.transform.position - position;
        Vector3 perVectorToColloder = vectorToColloder;
        perVectorToColloder.y = perVectorToColloder.x;
        perVectorToColloder.x = perVectorToColloder.z;
        perVectorToColloder.z = -perVectorToColloder.y;
        perVectorToColloder.y = 0;
        perVectorToColloder = perVectorToColloder.normalized;

        Vector3 checkForPlayerPoint1 = position + (perVectorToColloder * widthOfTheBoss / 2);
        Vector3 checkForPlayerPoint2 = position - (perVectorToColloder * widthOfTheBoss / 2);

        Debug.DrawRay(checkForPlayerPoint1, Camera.main.transform.position - checkForPlayerPoint1, Color.red);
        Debug.DrawRay(checkForPlayerPoint2, Camera.main.transform.position - checkForPlayerPoint2, Color.green);

        //Fires raycast to check if both of the points hit a wall or the boss itself.
        Physics.Raycast(checkForPlayerPoint1, Camera.main.transform.position - checkForPlayerPoint1, out RaycastHit hit, Mathf.Infinity, ~ignoreLayer);
        Physics.Raycast(checkForPlayerPoint2, Camera.main.transform.position - checkForPlayerPoint2, out RaycastHit hit2, Mathf.Infinity, ~ignoreLayer);


        //If either one of the raycasts hit the player, return false
        if (hit.collider != null && hit.collider.tag.Equals("Player"))
        {
            return false;
        }

        if (hit2.collider != null && hit2.collider.tag.Equals("Player"))
        {
            return false;
        }

        return true;
    }

    private IEnumerator WaitInCoverState(float secondsToWait) // Either breakWhenSpotted should be true, or secondsToWait should be >0, or both. If not this would go forever
    {
        navMeshAgent.speed = 0;
        IdleAni();
        // Check to see if this call is actually capable of ending
        if (secondsToWait == 0)
        {
            ExitInCoverState();
            yield break;
        }

        // Assign how long to wait before breaking, if more than 0
        WaitForSeconds wait = null;
        if (secondsToWait > 0)
        {
            wait = new WaitForSeconds(secondsToWait);
        }

        while (true)
        {
            Vector3 vToPlayer = PlayerController.puppet.transform.position - transform.position;
            if (Physics.Raycast(transform.position, vToPlayer, out RaycastHit hit, playerSpottedDistance, ~hidingSpotLayer))
            {
                //If the player detected the boss
                if (hit.collider.CompareTag("Player"))
                {
                    ExitInCoverState();
                    //bossState = BossState.takingCover;
                    yield break;
                }
            }

            yield return wait;
            //bossState = BossState.takingCover;
            
            ExitInCoverState();
            //yield break;
        }
    }
    public virtual void ExitInCoverState()
    {
        bossState = currentMovementPhase.coverDecision.GetTheNextRandomDicision();
    }

    public int CoverColliderArraySortComparer(Collider A, Collider B) // Refer to documentation on System.Array.Sort
    {
        if (A == null && B != null)
        {
            return 1;
        }
        else if (A != null && B == null)
        {
            return -1;
        }
        else if (A == null && B == null)
        {
            return 0;
        }
        else
        {
            return Vector3.Distance(navMeshAgent.transform.position, A.transform.position).CompareTo(Vector3.Distance(navMeshAgent.transform.position, B.transform.position));
        }
    }

    public IEnumerator TeleportingToCoverState(Transform target)
    {
        IdleAni();

        while (true)
        {
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, teleportSampleDistance, hidingSpotLayer);

            if (hitColliders.Length == 0)
            {
                Debug.Log("Unable to find point to teleport");
            }
            else
            {
                //Find the right collider that's behind the player
                Collider tempCol = FindRandomValidHidingSpot(target, hitColliders);

                // If null, then boss is unable to find a spot to teleport behind
                if (tempCol == null)
                {
                    Debug.Log("No valid teleport spot");
                    bossState = BossState.takingCover;

                }
                else
                {
                    SpawnManager.SpawnVFX(teleportVFX.vFX, this.transform, teleportVFX.offset);

                    this.transform.position = tempCol.transform.position;

                    ExitTeleportingState();
                }
            }

            yield return null;
        }
    }
    
    public virtual void ExitTeleportingState()
    {
        bossState = currentMovementPhase.teleportDecision.GetTheNextRandomDicision();
    }

    public IEnumerator TeleportingBehindState(Transform target)
    {
        WaitForSeconds wait = new WaitForSeconds(coverUpdateFrequency);
        IdleAni();

        while (true)
        {
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, teleportSampleDistance, hidingSpotLayer);

            if (hitColliders.Length == 0)
            {
                Debug.Log("Unable to find point to teleport");
            }
            else
            {
                //Find the right collider that's behind the player
                Collider tempCol = FindValidBehindPlayerSpot(target, hitColliders);

                // If null, then boss is unable to find a spot to teleport behind
                if (tempCol == null)
                {
                    // If there's no available spot that is behind the player, then seach all the spot
                    tempCol = FindClosestValidHidingSpot(target, hitColliders);

                    // If null, then boss is unable to find a spot to teleport
                    if (tempCol == null)
                    {
                        Debug.Log("No valid teleport spot");
                        bossState = BossState.takingCover;
                    }
                    else
                    {
                        this.transform.position = tempCol.transform.position;
                        bossState = BossState.meleeAttack;
                    }

                }
                else
                {
                    this.transform.position = tempCol.transform.position;
                    bossState = BossState.meleeAttack;
                }
            }
            

            yield return null;
        }
    }

    public Collider FindValidBehindPlayerSpot(Transform target, Collider[] colliders)
    {
        Collider tempCol = null;
        foreach (Collider thisCol in colliders)
        {
            Vector3 vectorToColloder = thisCol.transform.position - target.position;

            //Check if the the spot is behind the player
            if (Vector3.Dot(vectorToColloder, target.forward) > 0)
            {
                continue;
            }

            if (!IsItAValidHidingPoint(widthOfTheBoss, thisCol.transform.position))
            {
                continue;
            }

            if (Mathf.Abs(vectorToColloder.magnitude) < playerTooCloseDistanceToCover)
            {
                continue;
            }


            if (tempCol == null)
            {
                tempCol = thisCol;
            }
            else if (Vector3.Distance(thisCol.transform.position, target.position) < Vector3.Distance(tempCol.transform.position, target.position))
            {
                tempCol = thisCol;
            }
        }
        return tempCol;
    }

    public IEnumerator OrbWalkState()
    {
        EnteringOrbWalk();
        yield return null;
        animator.SetInteger(aniDecision, walkAni);
        yield return null;
        while (true)
        {
            OrbWalkMovementSystem();
            yield return null;
        }
    }

    public void EnteringOrbWalk()
    {
        navMeshAgent.SetDestination(destination);
        navMeshAgent.stoppingDistance = 0;
        navMeshAgent.angularSpeed = 0;
        navMeshAgent.speed = orbWalkSpeed;
        navMeshAgent.acceleration = orbWalkAcceleration;

        orbWalkingTime = Random.Range(minEndOrbWalkingTime, maxEndOrbWalkingTime);
        endOrbWalkingTimer = orbWalkingTime;

        IdleAni();
        
        if (Random.Range(0, 2) == 0)
        {
            ChangeDirection();
        }
    }

    // Orb Walk is just Orbit the player while facing the player. It mantains a certain distance towards the player.
    public void OrbWalkMovementSystem()
    {
        //Timer for exit OrbWalk State
        if (endOrbWalkingTimer <= 0)
        {
            endOrbWalkingTimer = 0;
        }
        else
        {
            endOrbWalkingTimer -= Time.fixedDeltaTime;
        }

        Vector3 lookDirection;
        lookDirection = (PlayerController.puppet.transform.position - transform.position).normalized;

        lookDirection.y = 0;
        rotateGoalWithOutY = Quaternion.LookRotation(lookDirection);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, rotateGoalWithOutY, turnSpeed * Time.fixedDeltaTime);

        NavMeshHit hit;

        //Check for the player distance
        if (!IsPlayerWithinDistance(farthestStoppingDistance))
        {
            moveVector = moveForwardVector;
        }
        else if (IsPlayerWithinDistance(closestStoppingDistance))
        {
            moveVector = moveBackwardVector;
        }
        else
        {
            moveVector = moveMidRangeVector;
        }

        animator.SetFloat(aniLeftRightDecision, moveVector.normalized.x);
        animator.SetFloat(aniForwardBackDecision, moveVector.normalized.z);

        //Check if it hits a wall, if it does, change the opposite directions
        Vector3 tempMoveVector = moveVector.normalized * navMeshDetactionDistance;
        if (NavMesh.SamplePosition(transform.position + (transform.right.normalized * tempMoveVector.x) + (transform.forward.normalized * tempMoveVector.z), out hit, navMeshDetactionRadius, NavMesh.AllAreas))
        {
            navMeshAgent.SetDestination(hit.position);
        }
        else
        {
            ChangeDirection();
        }

        if (endOrbWalkingTimer <= 0)
        {
            IdleAni();
            navMeshAgent.stoppingDistance = 0;
            navMeshAgent.angularSpeed = angularSpeed;
            navMeshAgent.speed = speed;
            navMeshAgent.acceleration = acceleration;
            ExitOrbWalkState();

        }
    }

    public virtual void ExitOrbWalkState()
    {
        bossState = currentMovementPhase.orbwalkDecision.GetTheNextRandomDicision();
    }
    private void ChangeDirection()
    {
        moveForwardVector.x *= -1;
        moveMidRangeVector.x *= -1;
        moveBackwardVector.x *= -1;
    }
    public IEnumerator SpawnMinesState()
    {
        mobSpawnerController.SpawningBaseOnIndex(1);
        ExitSpawnMinesState();
        yield return null;
    }
    public virtual void ExitSpawnMinesState()
    {
        bossState = currentMovementPhase.dropMineDecision.GetTheNextRandomDicision();
    }
    public IEnumerator SpawnTurretsState()
    {
        mobSpawnerController.SpawningBaseOnIndex(0);
        ExitSpawnTurretState();
        yield return null;

    }
    public virtual void ExitSpawnTurretState()
    {
        bossState = currentMovementPhase.dropTurretDecision.GetTheNextRandomDicision();
    }

    public IEnumerator SpawnMobsState()
    {
        mobSpawnerController.SpawningBaseOnIndex(2);
        ExitSpawnMobsState();
        yield return null;

    }
    public virtual void ExitSpawnMobsState()
    {
        bossState = currentMovementPhase.spawnMobDecision.GetTheNextRandomDicision();
    }

    public void TauntSpawnMobAni()
    {
        if (bossState == BossState.spawnMines)
        {
            mobSpawnerController.SpawningBaseOnIndex(1);
        } 
        else if (bossState == BossState.spawnTurrets)
        {
            mobSpawnerController.SpawningBaseOnIndex(0);
        }
    }

    public bool IsPlayerWithinDistance(float range)
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, range);
        foreach (Collider thisCollider in colliders)
        {
            if (thisCollider.tag == "Player")
            {
                return true;
            }
        }
        return false;
    }
    //This is to check if the player is within view.
    public bool IsPlayerWithinView(float range, float degreeH, float degreeV)
    {
        RaycastHit hit;
        Vector3 veiwToPlayerMesh = PlayerController.puppet.cameraObj.transform.position - viewPoint.transform.position;
        Physics.Raycast(viewPoint.transform.position, veiwToPlayerMesh, out hit, range, ~hidingSpotLayer);
        if (hit.collider != null && hit.collider.tag == "Player")
        {
            float angleH = Vector3.Angle(new Vector3(veiwToPlayerMesh.x, 0, veiwToPlayerMesh.z), viewPoint.transform.forward);
            float angleV = Vector3.Angle(new Vector3(viewPoint.transform.forward.x, veiwToPlayerMesh.y, viewPoint.transform.forward.z), viewPoint.transform.forward);
            if (angleH < degreeH / 2 && angleV < degreeV / 2)
            {
                Debug.DrawRay(viewPoint.transform.position, veiwToPlayerMesh, Color.blue);
                return true;
            }
        }
        return false;
    }
    //Aim towards a postion at horizonl
    public virtual void AimTowards(Vector3 position, float aimSpeed)
    {
        Vector3 veiwToPlayerMesh = position - viewPoint.transform.position;
        veiwToPlayerMesh.y = 0;
        transform.forward = Vector3.RotateTowards(transform.forward, veiwToPlayerMesh, aimSpeed * Time.deltaTime, 0.0f);
        Debug.DrawRay(viewPoint.transform.position, veiwToPlayerMesh, Color.blue);
    }

    public virtual void AimTowardsWithY(GameObject gameObject, Vector3 position, float aimSpeed)
    {
        Vector3 veiwToPlayerMesh = position - gameObject.transform.position;
        gameObject.transform.forward = Vector3.RotateTowards(gameObject.transform.forward, veiwToPlayerMesh, aimSpeed * Time.deltaTime, 0.0f);
        Debug.DrawRay(gameObject.transform.position, veiwToPlayerMesh, Color.red);
    }


    public override void Damage(float damageAmount, Vector3 hitPosition, DamageType damageType = DamageType.nuetral)
    {
        float damage;
        if (currentArmorElementType != DamageType.nuetral)
        {
            if (damageImmunities.Contains(damageType))
            {
                if (usesDamageText)
                {
                    GameObject damageText = GetDamageText(damageType);
                    damageText.GetComponent<DamageText>().UpdateDamage(hitPosition, 0, damageType);
                }

                return;
            }

            damage = DamageCalculation(damageAmount, damageType);

            if (usesDamageText)
            {
                GameObject damageText = GetDamageText(damageType);
                damageText.GetComponent<DamageText>().UpdateDamage(hitPosition, damage, damageType);
            }

            currentArmorHealth.AddToStat(-damage);

            //Check for the health of armor
            if (currentArmorHealth.stat <= currentArmorHealth.minimum)
            {
                if (currentArmorElementType == DamageType.ice)
                {
                    SpawnManager.SpawnVFX(iceArmorDestroyedVFX.vFX, this.transform, iceArmorDestroyedVFX.offset);
                }
                if (currentArmorElementType == DamageType.fire)
                {
                    SpawnManager.SpawnVFX(fireArmorDestroyedVFX.vFX, this.transform, fireArmorDestroyedVFX.offset);
                }
                currentArmorHealth.stat = currentArmorHealth.minimum;
                EnterNoArmorState();
                health.AddToStat(-health.maximum * currenthPReductionPercentOnArmorBreak/100);
            }

            

            StartCoroutine(InvincibilityFrames());
            return;
        }

        Debug.Log("Health decrease");
        base.Damage(damageAmount, hitPosition, damageType);

        if (isBossTPOutToNextScene)
        {
            if (((health.stat - health.minimum) / health.maximum) * 100 <= hPPercetageToTP)
            {
                TPOutOfScene();
            }
        }
    }

    public void TPOutOfScene()
    {
        SpawnManager.SpawnVFX(teleportVFX.vFX, this.transform, teleportVFX.offset);

        bossEndSceneGameObject.SetActive(true);

        this.gameObject.SetActive(false);

    }

    public IEnumerator DeadState()
    {
        navMeshAgent.speed = 0;
        animator.SetBool(aniDeathDecision, true);
        yield return null;
        yield return new WaitForSeconds(activationTimeAfterDeath);
        animator.SetBool(aniDeathDecision, false);
        Dead();

    }

    public override void CommitDie()
    {
        
        base.CommitDie();
        if (isAbleToPlayDeathAni)
        {
            bossState = BossState.dead;
        }
        else
        {
            Dead();
        }
        
    }

    public virtual void Dead()
    {
        GeneralManager.instance.WinGame();
    }

    
}
