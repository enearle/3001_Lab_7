
using System.Collections.Generic;
using UnityEngine;

// Singleton
// Spawns items into the world and stores references to all spawned items
public class WeaponSpawner : MonoBehaviour
{
    public static WeaponSpawner Instance;
    
    [SerializeField] private WeaponBehaviour playerWB;
    [SerializeField] private WeaponBehaviour enemyWB;
    [SerializeField] private GameObject shotgunPrefab;
    [SerializeField] private GameObject sniperPrefab;
    [SerializeField] private Transform blBound;
    [SerializeField] private Transform trBound;

    public List<WeaponPickup> weaponsSpawned = new List<WeaponPickup>();

    private float timerDuration;
    private float timer;

    private bool needShotguns = true;
    private bool needSnipers = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        
        timerDuration = Random.Range(2.0f, 6.0f);
        timer = 0.0f;
    }

    private void Update()
    {
        if(UpdateWeaponState() && UpdateSpawnTimer())
            SpawnWeapons();
    }

    // Determines if weapons are needed and which ones.
    // Returns true if weapons are needed.
    private bool UpdateWeaponState()
    {
        needShotguns = !playerWB.hasShotgun || !enemyWB.hasShotgun;
        needSnipers = !playerWB.hasSniper || !enemyWB.hasSniper;
        
        return needShotguns || needSnipers;
    }
    
    // Ticks the timer for spawning new weapons.
    // Returns true on the frame that the timer resets.
    private bool UpdateSpawnTimer()
    {
        timer += Time.deltaTime;
        if (timer > timerDuration)
        {
            timer -= timerDuration;
            timerDuration = Random.Range(2.0f, 6.0f);
            return true;
        }

        return false;
    }

    // Spawns two weapons at two locations within a box defined by the positions of empty game objects:
    // blBound(bottom-left) and trBound(top-right). 
    private void SpawnWeapons()
    {
        Vector3 positionA = new Vector3(Random.Range(blBound.position.x, trBound.position.x),
            Random.Range(blBound.position.y, trBound.position.y));
        Vector3 positionB = new Vector3(Random.Range(blBound.position.x, trBound.position.x),
            Random.Range(blBound.position.y, trBound.position.y));

        GameObject weaponA;
        GameObject weaponB;
       
        if (needShotguns && needSnipers)
        {
            weaponA = Random.value < 0.5f
                ? Instantiate(shotgunPrefab, positionA, Quaternion.identity)
                : Instantiate(sniperPrefab, positionA, Quaternion.identity);
            weaponB = Random.value < 0.5f
                ? Instantiate(shotgunPrefab, positionB, Quaternion.identity)
                : Instantiate(sniperPrefab, positionB, Quaternion.identity);
        }
        else if (needShotguns)
        {
            weaponA = Instantiate(shotgunPrefab, positionA, Quaternion.identity);
            weaponB = Instantiate(shotgunPrefab, positionB, Quaternion.identity);
        }
        else
        {
            weaponA = Instantiate(sniperPrefab, positionA, Quaternion.identity);
            weaponB = Instantiate(sniperPrefab, positionB, Quaternion.identity);
        }
        
        weaponsSpawned.Add(weaponA.GetComponent<WeaponPickup>());
        weaponsSpawned.Add(weaponB.GetComponent<WeaponPickup>());
    }
}
