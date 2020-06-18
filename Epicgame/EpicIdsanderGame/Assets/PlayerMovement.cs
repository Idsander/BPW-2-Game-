using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;

public class PlayerMovement : MonoBehaviour
{
    public float baseSpeed = 20f;
    private float speed;
    Vector2 baseUp = new Vector2(0.0f, 1200f);
    Vector2 up;

    Vector2 right = new Vector2(0.0f, 0.0f);
    float horizontalMove = 0f;

    bool facingRight = true;
    bool werewolf = false;

    public Rigidbody2D rb;
    private BoxCollider2D bc;

    private Boolean grounded;
    private Boolean climbable;
    private Boolean stuck;

    int playercamsize = 7;
    int wolfcamsize = 9;
    public PostProcessVolume volume;
    public ColorGrading colour;
    
    public Camera playercam;
    [SerializeField] private LayerMask platformLayerMask;
    [SerializeField] private LayerMask climbwallLayerMask;

    public Animator animator;

    void Awake()
    {
        //Initializes movement variables
        speed = baseSpeed;
        up = baseUp;

        //Gets the colour grading effect of the Post Processing Volume
        bc = transform.GetComponent<BoxCollider2D>();
        volume.profile.TryGetSettings(out colour);
    }

    // Update is called once per frame
    void Update()
    {
        //Sets booleans from functions
        grounded = IsGrounded();
        climbable = CanClimb();
        stuck = IsStuck();

        //Gets user input from A and D
        horizontalMove = Input.GetAxisRaw("Horizontal");

        //Sets animation according to movement input
        animator.SetFloat("speed", Mathf.Abs(horizontalMove));

        //Checks whether the player should be flipped based on where player is facing and their current speed
        if ((rb.velocity.x < 0) && (facingRight)) {
            Flip();
        }
        if ((rb.velocity.x > 0) && (!facingRight))
        {
            Flip();
        }

        //Jumping code
        if (Input.GetButtonDown("Jump"))
        {
            if (animator.GetBool("climb"))
            {
                if (facingRight)
                {
                    rb.AddForce(new Vector2(-600f, 0f));
                } else
                {
                    rb.AddForce(new Vector2(600f, 0f));
                }
                Flip();
                rb.AddForce(up);
            } else if (grounded)
            {
                //Adds vertical force
                rb.AddForce(up);
            }

        }

        //Transform code
        if (Input.GetKeyDown(KeyCode.E))
        {
            if ((grounded) && (!stuck))
            {
                if (!werewolf)
                {
                    //Changes colour, speed and animation to werewolf
                    colour.saturation.value = -100;
                    colour.active = true;
                    speed = 1.8f * baseSpeed;
                    up = baseUp + new Vector2(0.0f, 250f);
                    werewolf = true;
                    animator.SetBool("werewolf", true);
                }
                else
                {
                    //Changes colour, speed and animation to human
                    colour.saturation.value = 0f;
                    colour.active = true;
                    up = baseUp;
                    speed = baseSpeed;
                    werewolf = false;
                    animator.SetBool("werewolf", false);
                }
            }
            
            
        }

        //Climbing code
        if ((Input.GetKey(KeyCode.W)) && (!werewolf) && (climbable))
        {
            //Sets animation to climbing and gives player vertical speed
            animator.SetBool("climb", true);
            rb.velocity = 5 * Vector2.up;
        } else
        {
            //Turns climbing animation off
            animator.SetBool("climb", false);
        }


        //Crouching code
        if ((grounded) && (Input.GetKey(KeyCode.S)) && (!werewolf)) {
            //Sets new hitbox, speed and animation for crouching
            speed = baseSpeed * 0.7f;
            animator.SetBool("crouch", true);
            bc.offset = new Vector2(0f, -0.11f);
            bc.size = new Vector2(0.27f, 0.24f);            
        } else if ((!stuck) && (!werewolf))
        {
            //Sets new hitbox, speed and animation for not crouching
            speed = baseSpeed;
            animator.SetBool("crouch", false);
            bc.offset = new Vector2(0f, -0.03f);
            bc.size = new Vector2(0.27f, 0.4f);
            
        }

        //Zooms the player out and in depending on their current form
        if (werewolf)
        {
            if (playercam.orthographicSize < wolfcamsize)
            {
                playercam.orthographicSize += 0.1f;
            } else
            {
                playercam.orthographicSize = wolfcamsize;
            }
        }
        if (!werewolf)
        {
            if (playercam.orthographicSize > playercamsize)
            {
                playercam.orthographicSize -= 0.1f;
            } else
            {
                playercam.orthographicSize = playercamsize;
            }
        }

        //Changes the animator to the player's jumping or falling animations accordingly
        if (rb.velocity.y > 0.9)
        {
            animator.SetBool("down", false);
            animator.SetBool("up", true);
        } else if (rb.velocity.y < -0.9)
        {
            animator.SetBool("up", false);
            animator.SetBool("down", true);
        } else if (IsGrounded())
        {
            animator.SetBool("up", false);
            animator.SetBool("down", false);
        }
    }

    //Updates player's horizontal movement speed
    void FixedUpdate()
    {
        //Get the user input and update the player's rigidbody accordingly
        right.x = horizontalMove * speed;
        if (IsGrounded())
        {
            rb.velocity = new Vector2(right.x, rb.velocity.y);
        } else
        {
            rb.AddForce(right * 2.5f);
        }
        
    }

    //Checks whether player should be able to climb
    private bool CanClimb()
    {
        //Sets direction according to which direction the player is facing
        Vector2 direction;
        if (facingRight)
        {
            direction = Vector2.right;
        }
        else
        {
            direction = Vector2.left;
        }

        //Distance to check for climbable wall
        float extraHeightText = 0.01f;

        //Y offset so that the RaycastHit originates from player's feet
        Vector3 offset = new Vector3(0, -0.703f, 0);

        //RaycastHit pointing in the direction the player is facing checking for climbable wall
        RaycastHit2D rayCastHit = Physics2D.Raycast(bc.bounds.center + offset , direction, bc.bounds.extents.y + extraHeightText, climbwallLayerMask);

        //Returns whether a climbable wall was found
        return rayCastHit.collider != null;
    }

    //Checks whether player is standing on the ground
    private bool IsGrounded()
    {
        //Offsets to check to left and right of player for ground
        Vector3 offsetleft = new Vector3(-0.45f, 0f, 0f);
        Vector3 offsetright = new Vector3(0.45f, 0f, 0f);

        //Distance
        float extraHeightText = 0.1f;

        //RaycastHits pointing straight down checking for platform underneath player at both offsets
        RaycastHit2D rayCastHit = Physics2D.Raycast(bc.bounds.center + offsetleft, Vector2.down, bc.bounds.extents.y + extraHeightText, platformLayerMask);
        RaycastHit2D rayCastHit2 = Physics2D.Raycast(bc.bounds.center + offsetright, Vector2.down, bc.bounds.extents.y + extraHeightText, platformLayerMask);

        //Returns whether a platform was found by either raycasts
        return ((rayCastHit.collider != null) ||(rayCastHit2.collider != null));
    }

    
    //Checks whether the player has a ceiling directly above them
    private bool IsStuck()
    {
        //Offsets to check to left and right of player for ceiling
        Vector3 offsetleft = new Vector3(-0.45f, 0f, 0f);
        Vector3 offsetright = new Vector3(0.45f, 0f, 0f);

        //Distance to check above player
        float extraHeightText = 1f;

        //RaycastHit pointing straight up at both offsets beside player that check for collision with platform layer
        RaycastHit2D rayCastHit = Physics2D.Raycast(bc.bounds.center + offsetleft, Vector2.up, bc.bounds.extents.y + extraHeightText, platformLayerMask);
        RaycastHit2D rayCastHit2 = Physics2D.Raycast(bc.bounds.center + offsetright, Vector2.up, bc.bounds.extents.y + extraHeightText, platformLayerMask);

        //If either of the rays collided, player IsStuck() returns true
        return ((rayCastHit.collider != null) || (rayCastHit2.collider != null));
    }

    //Flips the player's x-scale
    private void Flip()
    {
        //Updates boolean
        facingRight = !facingRight;

        //Gets playerscale, reverses scale and updates
        Vector3 playerScale = transform.localScale;
        playerScale.x *= -1;
        transform.localScale = playerScale;
    }
}
