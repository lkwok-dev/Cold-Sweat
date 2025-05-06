using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


// The component for the Laser hurtbox. It can set as infinite speed for the laser.
public class Laser : MonoBehaviour
{
    [HideInInspector] public float laserFrequency;
    [HideInInspector] public float laserDamage;
    [HideInInspector] private float timer;
    [SerializeField] public float maxlaserLength;
    [SerializeField] bool isInfiniteSpeed;
    [SerializeField] public float laserSpeed;
    [SerializeField] public float laserSize = 1;
    [SerializeField] public GameObject[] laserRayCastPoints;
    [SerializeField] public LayerMask layer;
    private float currentLaserLegth;

    private void Awake()
    {
        laserSize = laserSize * transform.localScale.z;
    }
    void OnEnable()
    {
        this.transform.localScale = new Vector3(laserSize, laserSize, .1f);
    }

    void Update()
    {
        if (isInfiniteSpeed)
        {
            //The array of raycasts hit something and snap to that distance of that game object
            if (PointsThatHit(this.transform.localScale.z, layer) > 0)
            {
                this.transform.localScale = new Vector3(laserSize, laserSize, GetLongestDistanceFromPointsHit(this.transform.localScale.z, layer));
                return;
            }
            //The array of raycasts hit nothing and snap to the max distance
            if (PointsThatHit(this.transform.localScale.z, layer) == 0)
            {
                this.transform.localScale = new Vector3(laserSize, laserSize, maxlaserLength);
                return;
            }

        }
        else
        {
            //The array of raycasts hit something and snap to that distance of that game object
            if (PointsThatHit(this.transform.localScale.z, layer) > 0)
            {
                float temp = GetLongestDistanceFromPointsHit(maxlaserLength, layer);
                if (temp < this.transform.localScale.z)
                {
                    this.transform.localScale = new Vector3(laserSize, laserSize, temp);
                }
                currentLaserLegth = temp;
            }
            //The array of raycasts hit nothing and snap to the max distance
            if (PointsThatHit(this.transform.localScale.z, layer) == 0)
            {
                currentLaserLegth = maxlaserLength;
            }

            //Increase laser hurt box base on speed
            if (currentLaserLegth > this.transform.localScale.z)
            {
                this.transform.localScale += new Vector3(0, 0, laserSpeed * Time.deltaTime);
            }
        }

    }


    //Damage value is been change in the attacks
    private void OnTriggerStay(Collider other)
    {
        timer -= Time.deltaTime;
        if (timer <= 0)
        {
            
            if (other.tag.Equals("Player"))
            {
                
                PlayerController.puppet.ChangeTemperature(laserDamage);
                timer = laserFrequency;
            }
        }
        else
        {
            timer = 0;
        }
        
    }

    //Return the number of raycasts that hit game objects
    private int PointsThatHit(float rayCastDistance, LayerMask thatLayer)
    {
        int temp = 0;
        foreach (GameObject thisPoint in laserRayCastPoints)
        {
            Physics.Raycast(thisPoint.transform.position, thisPoint.transform.forward, out RaycastHit hit, isInfiniteSpeed ? maxlaserLength : rayCastDistance, thatLayer);
            if (hit.collider != null)
            {
                temp++;
            }
        }
        return temp;
    }

    //Return the the longest distance that hit a game object.
    //If the mutiple raycasts hit on mutiple game objects, it will find which game object got hit the most by the array of raycasts and return the longest distance of that game object.
    private float GetLongestDistanceFromPointsHit(float rayCastDistance, LayerMask thatLayer)
    {
        
        RaycastHit[] rayCastThatHit = new RaycastHit[laserRayCastPoints.Length];
        //This is just to set up
        for (int i = 0; i < laserRayCastPoints.Length; i++)
        {
            Physics.Raycast(laserRayCastPoints[i].transform.position, laserRayCastPoints[i].transform.forward, out RaycastHit hit,isInfiniteSpeed? maxlaserLength : rayCastDistance, thatLayer);
            rayCastThatHit[i] = hit;
            
        }

        return FindTheLongestDistanceOnTheSameObject(rayCastThatHit);
    }

    private float FindTheLongestDistanceOnTheSameObject(RaycastHit[] rayCastThatHit)
    {
        GameObject[] gameObjectsThatHited = new GameObject[laserRayCastPoints.Length];
        int[] countsRaysThatHitOnTheSameObject = new int[laserRayCastPoints.Length];


        //Find The object that get hit the most by the raycast into currentGameObject
        for (int i = 0; i < laserRayCastPoints.Length; i++)
        {
            if (rayCastThatHit[i].collider == null)
            {
                goto OuterLoop;
            }
            for (int j = 0; j <= i; j++)
            {
                if (gameObjectsThatHited[j] == null)
                {
                    gameObjectsThatHited[j] = rayCastThatHit[i].collider.gameObject;
                    countsRaysThatHitOnTheSameObject[j]++;
                    goto OuterLoop;
                }

                if (gameObjectsThatHited[j] == rayCastThatHit[i].collider.gameObject)
                {
                    countsRaysThatHitOnTheSameObject[j]++;
                    goto OuterLoop;
                }

            }
        OuterLoop:
            continue;

        }

        //Find the index that the raycast that got hit the most on the same game object
        int index = 0;
        float count = 0f;
        for (int i = 0; i < countsRaysThatHitOnTheSameObject.Length; i++)
        {
            if (count < countsRaysThatHitOnTheSameObject[i])
            {
                count = countsRaysThatHitOnTheSameObject[i];
                index = i;
            }

        }

        GameObject currentGameObject = gameObjectsThatHited[index];

        //Find the distance of the game object that got hit the most by raycast
        float tempDistance = 0;
        for (int i = 0; i < laserRayCastPoints.Length; i++)
        {
            //Check for null for raycast collider
            if (rayCastThatHit[i].collider == null)
            {
                continue;
            }
            //Not the same GameObjectm so skip
            if (currentGameObject != rayCastThatHit[i].collider.gameObject)
            {
                continue;
            }
            //Distance have found
            if (tempDistance < rayCastThatHit[i].distance)
            {
                tempDistance = rayCastThatHit[i].distance;
            }

        }

        return tempDistance;
    }
}
