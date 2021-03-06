﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GunController : MonoBehaviour {
    public int gunDamage = 1;                                           // Set the number of hitpoints that this gun will take away from shot objects with a health script
    public float fireRate = 0.25f;                                      // Number in seconds which controls how often the player can fire
    public float weaponRange = 50f;                                     // Distance in Unity units over which the player can fire
    public float hitForce = 100f;                                       // Amount of force which will be added to objects with a rigidbody shot by the player
    public Transform gunEnd;                                            // Holds a reference to the gun end object, marking the muzzle location of the gun
    public GameObject reticule;

    //public float speed = 4f;

    private WaitForSeconds shotDuration = new WaitForSeconds(0.07f);    // WaitForSeconds object used by our ShotEffect coroutine, determines time laser line will remain visible
    private AudioSource gunAudio;                                       // Reference to the audio source which will play our shooting sound effect
    private LineRenderer laserLine;                                     // Reference to the LineRenderer component which will display our laserline
    private float nextFire;												// Float to store the time the player will be allowed to fire again, after firing
    private Vector3 originalReticuleScale;
    private Player player;
    private EnemyMaster enemyMaster;


    private void Awake()
    {
        // Get and store a reference to our LineRenderer component
        laserLine = GetComponent<LineRenderer>();

        // Get and store a reference to our AudioSource component
        gunAudio = GetComponent<AudioSource>();

        player = GameObject.FindGameObjectWithTag("Player").GetComponent<Player>();
        enemyMaster = GameObject.FindGameObjectWithTag("EnemyMaster").GetComponent<EnemyMaster>();

        originalReticuleScale = reticule.transform.localScale;
    }

    // Use this for initialization
    void Start () {
        
    }
    
    protected static int raycastLayerMask = ~(1 << 9 | 1 << 11); // raycast ignores projectiles and triggers
    // Update is called once per frame
    void Update()
    {

        // Create a vector at the gun end
        Vector3 rayOrigin = gunEnd.transform.position;
        // Declare a raycast hit to store information about what our raycast has hit
        RaycastHit hit;
        bool didHit = Physics.Raycast(rayOrigin, gunEnd.transform.forward, out hit, weaponRange, raycastLayerMask);
        Enemy hitEnemy = null;

        // Check if the player has pressed the fire button and if enough time has elapsed since they last fired
        if ((Input.GetButtonDown("Fire1") || SixenseInput.Controllers[1].GetButton(SixenseButtons.TRIGGER))
            && Time.time > nextFire)
        {
            // Update the time when our player can fire next
            nextFire = Time.time + fireRate;

            // Set the start position for our visual effect for our laser to the position of gunEnd
            laserLine.SetPosition(0, gunEnd.position);

            // Check if our raycast has hit anything
            if (didHit)
            {
                // Set the end position for our laser line 
                laserLine.SetPosition(1, hit.point);

                // Get a reference to a health script attached to the collider we hit
                hitEnemy = hit.collider.GetComponentInParent<Enemy>();

                // If there was a health script attached
                if (hitEnemy != null)
                {
                    // Call the damage function of that script, passing in our gunDamage variable
                    hitEnemy.AddDamage(gunDamage);
                    hitEnemy.Alert();
                }

                // Check if the object we hit has a rigidbody attached
                if (hit.rigidbody != null)
                {
                    // Add force to the rigidbody we hit, in the direction from which it was hit
                    hit.rigidbody.AddForce(-hit.normal * hitForce);
                }
            }
            else
            {
                // If we did not hit anything, set the end of the line to a position directly in front of the gun end at the distance of weaponRange
                laserLine.SetPosition(1, rayOrigin + (gunEnd.transform.forward * weaponRange));
            }

            // Start our ShotEffect coroutine to turn our laser line on and off
            StartCoroutine("ShotEffect");

            foreach (Enemy enemy in enemyMaster.enemies.Where((Enemy e) => e != hitEnemy))
            {
                enemy.AlertIfInHearingRange();
            }
        }

        // update reticule
        if (didHit)
        {
            // put the reticule right above the hit surface (to avoid clipping)
            reticule.transform.position = hit.point - gunEnd.forward * 0.01f;
            reticule.transform.rotation = Quaternion.LookRotation(hit.normal);
            //reticule.transform.localScale = originalReticuleScale * hit.distance;
            reticule.transform.localScale = originalReticuleScale * Vector3.Distance(player._camera.transform.position, hit.point);
        }
        else
        {
            reticule.transform.position = gunEnd.position + gunEnd.forward * weaponRange;
            reticule.transform.rotation = Quaternion.LookRotation(gunEnd.forward);
            reticule.transform.localScale = originalReticuleScale * weaponRange;
        }
    }
    private IEnumerator ShotEffect()
    {
        //Debug.Log("Firing");
        // Play the shooting sound effect
        gunAudio.Play();

        // Turn on our line renderer
        laserLine.enabled = true;

        //Wait for .07 seconds
        yield return shotDuration;

        // Deactivate our line renderer after waiting
        laserLine.enabled = false;
    }
}