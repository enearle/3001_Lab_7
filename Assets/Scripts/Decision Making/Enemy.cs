
using UnityEngine;

public class Enemy : MonoBehaviour, IDamageable, IPickUp
{
    [SerializeField]
    private Transform player;

    private Rigidbody2D rb;

    public Transform[] waypoints;
    private int waypoint = 0;
    private int lastWaypoint = 0;

    private const float moveSpeed = 7.5f;
    private const float turnSpeed = 1080.0f;
    private const float viewDistance = 5.0f;

    private const int maxHealth = 100;
    public int health { get; private set; } = 100;
    public bool isPlayer { get; private set; } = false;

    [SerializeField] private WeaponBehaviour WB;
    public WeaponBehaviour wb { get{ return WB; } set {} }
    
    private Timer shootCooldown = new Timer();
    private Timer attackState = new Timer();
    private Timer defenceTimer = new Timer();

    private bool shotgunState = false;

    Color color = Color.cyan;

    enum State
    {
        DEFAULT,
        NEUTRAL,
        OFFENSIVE,
        DEFENSIVE,
        EQUIP
    };


    // If enemy drops below 25% health, flee and shoot!
    State state = State.DEFAULT;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        shootCooldown.total = 0.25f;
        defenceTimer.total = 20f;
        attackState.total = 5f;
        StateChange(State.NEUTRAL);
    }

    void Update()
    {
        float rotation = Steering.RotateTowardsVelocity(rb, turnSpeed, Time.deltaTime);
        rb.MoveRotation(rotation);

        bool seePlayer = false;
        if (player != null)
        {
            float playerDistance = Vector2.Distance(transform.position, player.position);
            seePlayer = playerDistance <= viewDistance;
        }

        bool noWeapons = !wb.hasShotgun && !wb.hasSniper;
        bool atleastOneWeapon = wb.hasShotgun || wb.hasSniper;
        bool weaponAvailable = WeaponAvailable();
        
        if (seePlayer)
        {
            StateChange(atleastOneWeapon ? State.OFFENSIVE : State.DEFENSIVE);
        }
        else
        {
            StateChange(noWeapons && weaponAvailable ? State.EQUIP : State.NEUTRAL);
        }

        // Repeating state-based actions:
        switch (state)
        {
            case State.NEUTRAL:
                Patrol();
                break;

            case State.OFFENSIVE:
                Attack();
                break;
            
            case State.DEFENSIVE:
                Defend();
                break;
            
            case State.EQUIP:
                Equip();
                break;
        }
        
        Debug.DrawLine(transform.position, transform.position + transform.right * viewDistance, color);
    }

    public void Damage(int damage)
    {
        health -= damage;
        
        // Begin defensive behaviour
        if(health < maxHealth / 4)
            StateChange(State.DEFENSIVE);
        
        if (health <= 0)
        {
            Destroy(gameObject);
        }
    }
    
    // Move toward nearest unequipped weapon
    private void Equip()
    {
        GameObject nearestWeapon = NearestWeapon();
        Vector3 steeringForce = Vector2.zero;
        steeringForce += Steering.Seek(rb, nearestWeapon.transform.position, moveSpeed);
        rb.AddForce(steeringForce);
    }
    
    private void Defend()
    {
        // Manage duration of defense or comment out for infinite duration
        defenceTimer.Tick(Time.deltaTime);
        if (defenceTimer.Expired())
        {
            StateChange(State.NEUTRAL);
            return;
        }
        
        // Seek the farthest waypoint from player
        Vector3 steeringForce = Vector2.zero;
        steeringForce += Steering.Seek(rb, waypoints[waypoint].transform.position, moveSpeed);
        rb.AddForce(steeringForce);
        
        // LOS to player
        Vector3 playerDirection = (player.position - transform.position).normalized;
        RaycastHit2D hit = Physics2D.Raycast(transform.position, playerDirection, viewDistance);
        bool playerHit = hit && hit.collider.CompareTag("Player");

        // Shoot player if in LOS at 1/5 the normal rate
        shootCooldown.Tick(Time.deltaTime * 0.2f);
        attackState.Tick(Time.deltaTime);
        
        if (attackState.Expired())
        {
            shotgunState = !shotgunState;
            attackState.Reset();
        }

        // Fire shotgun if all weapons are equipped and AI is in the shotgun state
        //              --or--
        // Fire shotgun if shotgun is only weapon equipped
        //
        // Else fire sniper
        if ((shotgunState && wb.hasShotgun) || !wb.hasSniper)
        {
            if((transform.position - player.position).magnitude < 3 && playerHit && shootCooldown.Expired())
            {
                shootCooldown.Reset();
                wb.FireShotgun(playerDirection);
            }
        }
        else
        {
            if((transform.position - player.position).magnitude > 3 && playerHit && shootCooldown.Expired())
            {
                shootCooldown.Reset();
                wb.FireSniper(playerDirection);
            }
        }
    }

    void Attack()
    {
        // LOS to player
        Vector3 playerDirection = (player.position - transform.position).normalized;
        RaycastHit2D hit = Physics2D.Raycast(transform.position, playerDirection, viewDistance);
        bool playerHit = hit && hit.collider.CompareTag("Player");

        float playerDist = (transform.position - player.position).magnitude;
        
        // Shoot player if in LOS
        shootCooldown.Tick(Time.deltaTime);
        attackState.Tick(Time.deltaTime);
        
        if (attackState.Expired())
        {
            shotgunState = !shotgunState;
            attackState.Reset();
        }

        // Fire shotgun if all weapons are equipped and AI is in the shotgun state
        //              --or--
        // Fire shotgun if shotgun is only weapon equipped
        //
        // Else fire sniper
        if ((shotgunState && wb.hasShotgun) || !wb.hasSniper)
        {
            if(playerDist < 3 && playerHit && shootCooldown.Expired())
            {
                shootCooldown.Reset();
                wb.FireShotgun(playerDirection);
            }
            else
            {
                // Reposition closer
                Vector3 steeringForce = Vector2.zero;
                steeringForce += Steering.Seek(rb, player.position, moveSpeed);
                rb.AddForce(steeringForce);
            }
        }
        else
        {
            if(playerDist > 3 && playerHit && shootCooldown.Expired())
            {
                shootCooldown.Reset();
                wb.FireSniper(playerDirection);
            }
            else
            {
                // Reposition further
                SetWaypointToFarthestFromPlayer();
                Vector3 steeringForce = Vector2.zero;
                steeringForce += Steering.Seek(rb, waypoints[waypoint].transform.position, moveSpeed);
                rb.AddForce(steeringForce);
            }
        }
    }

    void Patrol()
    {
        // Seek nearest waypoint
        Vector3 steeringForce = Vector2.zero;
        steeringForce += Steering.Seek(rb, waypoints[waypoint].transform.position, moveSpeed);
        rb.AddForce(steeringForce);
    }
    
    void OnTriggerEnter2D(Collider2D collision)
    {
        // State dependent waypoint selection
        if(state == State.NEUTRAL)
            SetWaypointToNearest();
        else if(state == State.DEFENSIVE)
            SetWaypointToFarthestFromPlayer();
    }
    
    private void StateChange(State newState)
    {
        // Call single use functions when entering a new state
        if (state != newState)
        {
            state = newState;
            switch (state)
            {
                case State.NEUTRAL:
                    EnterNeutralState();
                    break;
    
                case State.OFFENSIVE:
                    EnterAttackState();
                    break;
                
                case State.DEFENSIVE:
                    EnterDefenseState();
                    break;
                
                case State.EQUIP:
                    EnterEquipState();
                    break;
            }
        }
    }

    // Single execution functions on state change
    private void EnterAttackState()
    {
        Debug.Log("Attacking!");
        color = Color.red;
    }

    private void EnterDefenseState()
    {
        Debug.Log("Defending!");
        defenceTimer.Reset();
        color = Color.yellow;
        SetWaypointToFarthestFromPlayer();
    }
    
    private void EnterNeutralState()
    {
        Debug.Log("Chilling!");
        color = Color.green;
        SetWaypointToNearest();
    }

    private void EnterEquipState()
    {
        Debug.Log("Equipping!");
        color = Color.green;
    }

    // Waypoint utilities
    private void SetWaypointToFarthestFromPlayer()
    {
        float d = 0;
        int farthest = 0;
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (i != waypoint)
            {
                float n = (waypoints[i].transform.position - player.transform.position).magnitude;
                if (n > d)
                {
                    d = n;
                    farthest = i;
                }
            }
        }

        lastWaypoint = waypoint;
        waypoint = farthest;
    }
    
    private void SetWaypointToNearest()
    {
        // Sets waypoint to nearest that isn't the current or last waypoint
        int[] nextWaypoints = new int[2];
        for (int j = 0, i = 0; i < waypoints.Length; i++)
        {
            if(i != lastWaypoint && i != waypoint && j < 2)
            {
                nextWaypoints[j] = i;
                j++;
            }
        }
        
        int nearest;
        if ((waypoints[nextWaypoints[0]].transform.position - transform.position).magnitude <
            (waypoints[nextWaypoints[1]].transform.position - transform.position).magnitude)
            nearest = nextWaypoints[0];
        else
            nearest = nextWaypoints[1];
        
        lastWaypoint = waypoint;
        waypoint = nearest;
    }
    
    // Equip weapon and begin toggling behaviours if both weapons are equipped
    public void PickUpWeapon(int type)
    {
        switch (type)
        {
            case 0:
                wb.hasShotgun = true;
                break;
            case 1:
                wb.hasSniper = true;
                break;
        }

        if (wb.hasShotgun && wb.hasSniper)
            shotgunState = true;
    }

    // Return true if needed weapon is available
    bool WeaponAvailable()
    {
        if (WeaponSpawner.Instance.weaponsSpawned.Count == 0) return false;
        
        if (!wb.hasShotgun && !wb.hasSniper) return true;
        
        foreach (var weapon in WeaponSpawner.Instance.weaponsSpawned)
        {
            if (weapon.WeaponType == 1 && wb.hasShotgun)
                return true;
            if (weapon.WeaponType == 0 && wb.hasSniper)
                return true;
        }
        return false;
    }

    // Return GameObject of the nearest unequipped weapon
    GameObject NearestWeapon()
    {
        GameObject nearestSniper = null;
        GameObject nearestShotgun = null;
            
        float sniperDist = float.MaxValue;
        float shotgunDist = float.MaxValue;
        
        // Checks the distance of every weapon of every type as they will be iterated over regardless.
        for (int i = 0; i < WeaponSpawner.Instance.weaponsSpawned.Count; i++)
        {
            WeaponPickup Weapon = WeaponSpawner.Instance.weaponsSpawned[i];

            float distance;
            
            if (Weapon.WeaponType == 0 && 
                (distance = (transform.position - Weapon.transform.position).magnitude) < shotgunDist)
            {
                shotgunDist = distance;
                nearestShotgun = Weapon.gameObject;
            }
            else if (Weapon.WeaponType == 1 && 
                     (distance = (transform.position - Weapon.transform.position).magnitude) < sniperDist)
            {
                sniperDist = distance;
                nearestSniper = Weapon.gameObject;
            }
        }
        
        if (wb.hasShotgun)
            return nearestSniper;
            
        if (wb.hasSniper)
            return nearestShotgun;
            
        if (shotgunDist < sniperDist)
            return nearestShotgun;

        return nearestSniper;
    }
    
}
