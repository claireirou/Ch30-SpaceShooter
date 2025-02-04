﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Part is another serializable data storage class just like WeaponDefinition
/// </summary>
[System.Serializable]
public class Part
{
    // These three fields need to be defined in the Inspector pane
    public string name;     // The name of this part
    public float health;    // The amount of health this part has
    public string[] protectedBy;    // The other parts that protect this

    [HideInInspector]
    public GameObject go;   // The GameObject of this part
    [HideInInspector]
    public Material mat;    // The Material to show damage
}

/// <summary>
/// Enemy_4 will start off screen and then pick a random point on screen to
/// move to. Once it has arrived, it will pick another random point and 
/// continue until the player has shot it down
/// </summary>
public class Enemy_4 : Enemy
{
    [Header("Set in Inspector: Enemy_4")]
    public Part[] parts;    // The array of ship parts

    private Vector3 p0, p1;     // The two points to interpolate
    private float timeStart;    // Birth time for this Enemy_4
    private float duration = 4; // Duration of movement

    // Start is called before the first frame update
    void Start()
    {
        // Add initial position from Main.SpawnEnemy() to points as initial p0 & p1
        p0 = p1 = pos;

        InitMovement();

        // Cache GameObject & Material of each Part in parts
        Transform t;
        foreach (Part prt in parts)
        {
            t = transform.Find(prt.name);
            if(t != null)
            {
                prt.go = t.gameObject;
                prt.mat = prt.go.GetComponent<Renderer>().material;
            }
        }
    }

    void InitMovement()
    {
        p0 = p1;    // Set p0 to the old p1
        float widMinRad = bndCheck.camWidth - bndCheck.radius;
        float hgtMinRad = bndCheck.camHeight - bndCheck.radius;
        p1.x = Random.Range(-widMinRad, widMinRad);
        p1.y = Random.Range(-hgtMinRad, hgtMinRad);

        // Reset the time
        timeStart = Time.time;
    }

    public override void Move()
    {
        // This completely overrides Enemy_Move() with a linear interpolation
        float u = (Time.time - timeStart) / duration;

        if(u>=1)
        {
            InitMovement();
            u = 0;
        }

        u = 1 - Mathf.Pow(1 - u, 2);    // Apply Ease Out easting to u
        pos = (1 - u) * p0 + u * p1;    // Simple linear interpolation
    }

    // These two functions find a Part in parts based on name or GameObject
    Part FindPart(string n)
    {
        foreach(Part prt in parts)
        {
            if(prt.name == n)
            {
                return (prt);
            }
        }
        return (null);
    }
    Part FindPart(GameObject go)
    {
        foreach(Part prt in parts)
        {
            if(prt.go == go)
            {
                return (prt);
            }
        }
        return (null);
    }

    // These functions return true if the Part has been destroyed
    bool Destroyed(GameObject go)
    {
        return (Destroyed(FindPart(go)));
    }
    bool Destroyed(string n)
    {
        return (Destroyed(FindPart(n)));
    }
    bool Destroyed(Part prt)
    {
        if(prt == null)
        {
            // If no real ph passed in
            return (true);
        }
        // Returns true if comparison prt.health <= 0
        return (prt.health <= 0);
    }

    // This changes the color of just on Part to red instead of the whole ship.
    void ShowLocalizedDamage(Material m)
    {
        m.color = Color.red;
        damageDoneTime = Time.time + showDamageDuration;
        showingDamage = true;
    }

    // This will override the OnCollisionEnter that is part of Enemy.cs
    private void OnCollisionEnter(Collision coll)
    {
        GameObject other = coll.gameObject;
        switch(other.tag)
        {
            case "ProjectileHero":
                Projectile p = other.GetComponent<Projectile>();
                // If this Enemy is off screen, don't damage it
                if(!bndCheck.isOnScreen)
                {
                    Destroy(other);
                    break;
                }

                //Hurt this Enemy
                GameObject goHit = coll.contacts[0].thisCollider.gameObject;
                Part prtHit = FindPart(goHit);
                if(prtHit == null)
                {
                    // IF prtHit wasn't found...
                    goHit = coll.contacts[0].otherCollider.gameObject;
                    prtHit = FindPart(goHit);
                }
                // Check whether this par tis still protected
                if(prtHit.protectedBy != null)
                {
                    foreach(string s in prtHit.protectedBy)
                    {
                        // If one of the protecting parts hasn't been destroyed...
                        if(!Destroyed(s))
                        {
                            //...then don't damage this part yet
                            Destroy(other);     // Destroy the ProjectileHero
                            return;     // return before damaging Enemy_4
                        }
                    }
                }
                // It's not protected, so damage it
                // Get damage amount from Projectile.type and Main.W_DEFS
                prtHit.health -= Main.GetWeaponDefinition(p.type).damageOnHit;
                // Show damage
                ShowLocalizedDamage(prtHit.mat);
                if(prtHit.health <= 0)
                {
                    // Disable the damaged part
                    prtHit.go.SetActive(false);
                }
                // Check if whole ship is destroyed
                bool allDestroyed = true;
                foreach(Part prt in parts)
                {
                    if(!Destroyed(prt))
                    {
                        // Part still exists
                        allDestroyed = false;
                        break;
                    }
                }
                if(allDestroyed )
                {
                    // Tell Main singleton that this ship is completely destroyed
                    Main.S.shipDestroyed(this);
                    // Destroy this Enemy
                    Destroy(this.gameObject);
                }
                Destroy(other);     // Destroy ProjectileHero
                break;
        }
    }
}
