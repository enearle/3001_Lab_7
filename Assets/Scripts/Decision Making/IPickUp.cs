using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IPickUp
{
    public void PickUpWeapon(int type);

    public WeaponBehaviour wb { get; }

}
