using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponPickup : MonoBehaviour
{
    [SerializeField] private int weaponType = 0;

    public int WeaponType { get { return weaponType; } }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        {
            IPickUp o = other.gameObject.GetComponent<IPickUp>();
            if (o != null)
            {
                o.PickUpWeapon(weaponType);
                WeaponSpawner.Instance.weaponsSpawned.Remove(this);
                Destroy(gameObject);
            }
        }
    }
}
