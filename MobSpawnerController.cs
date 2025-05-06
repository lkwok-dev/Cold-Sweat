using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.VFX;
using UnityEngine;



public class MobSpawnerController : MonoBehaviour
{
    [System.Serializable]
    public struct SpawnData
    {
        public GameObject gameObjectToSpawn;
        public Transform gameObjectSPParent;
        public int numOfGameObjectSpawnEachTime;
        public int maxNumOfGameObjectSpawn;
        public VFXGameObject spawningVFX;
        public bool isStationary;
        [HideInInspector] public List<GameObject> spawnedObject;
    }

    [HideInInspector] public static MobSpawnerController instance;
    [SerializeField] SpawnData[] spawnData;

    private void Awake()
    {
        instance = GetComponent<MobSpawnerController>();
    }

    public void SpawningThings()
    {
        for (int i = 0; i < spawnData.Length; i++)
        {
            SpawnItemBaseOnData(spawnData[i]);
        }
    }
    public void SpawningBaseOnIndex(int i)
    {
        SpawnItemBaseOnData(spawnData[i]);
    }

    public void DestroyObjectInMobSpawner(GameObject gameObj)
    {
        for (int i = 0; i < spawnData.Length; i++)
        {
            if (spawnData[i].spawnedObject.Remove(gameObj))
            {
                Destroy(gameObj);
            }
        }
    }

    public void SpawnItemBaseOnData(SpawnData sD)
    {

        // This is to create a pot that can retieve a random number ticket that is inside the pot
        TicketPot pot = new TicketPot(sD.gameObjectSPParent.childCount);

        //The count variable is to make sure it wont spawn the object more than it needs to
        for (int i = 0, count = 0; i < sD.gameObjectSPParent.childCount && count < sD.numOfGameObjectSpawnEachTime ; i++)
        {
            if (sD.maxNumOfGameObjectSpawn <= sD.spawnedObject.Count)
            {
                return;
            }

            int spawnIndex = pot.PopRandomTicket();

            //Check if the spawn is empty
            if (sD.gameObjectSPParent.GetChild(spawnIndex).transform.childCount == 0)
            {
                SpawnManager.SpawnVFX(sD.spawningVFX.vFX, sD.gameObjectSPParent.GetChild(spawnIndex).transform, sD.spawningVFX.offset);
                //Spawn the Object and increase the count
                if (sD.isStationary)
                {
                    sD.spawnedObject.Add(Instantiate(sD.gameObjectToSpawn, sD.gameObjectSPParent.GetChild(spawnIndex).transform));
                }
                else
                {

                    sD.spawnedObject.Add(Instantiate(sD.gameObjectToSpawn, sD.gameObjectSPParent.GetChild(spawnIndex).transform.position, sD.gameObjectSPParent.GetChild(spawnIndex).transform.rotation));
                }
                count++;
            }
        }
    }
}



// A class that store number tickets into a pot, and can retrieve a random number ticket that is inside of the pot.
public class TicketPot
{
    List<int> pot = new List<int>();
    public TicketPot(int size)
    {
        FillThePot(size);
    }
    public void FillThePot(int size)
    {
        
        for (int i = 0; i < size; i++)
        {
            pot.Add(i);
        }
        
    }
    //Get a random number ticket from the pot
    public int PopRandomTicket()
    {
        if (pot.Count > 0)
        {
            int index = Random.Range(0, pot.Count - 1);
            int temp = pot[index];
            pot.RemoveAt(index);
            return temp;
        }
        else
        {
            Debug.Log("Pot is empty");
            return -1;
        }
    }
    public int Count()
    {
        return pot.Count;
    }
}
