using System.Collections.Generic;
using UnityEngine;
using MilanUtils;

[RequireComponent(typeof(Rigidbody2D)), RequireComponent(typeof(SpriteRenderer))]
public class AnimationManager : MonoBehaviour
{
    public Sprite standingSprite;
    public List<Sprite> spriteList;

    Transform leftArm, rightArm;

    int curWalkingFrame = 0;

    public float fps;

    Timer walkingFrameTimer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        walkingFrameTimer = new(1f / fps);
        leftArm = transform.Find("Left Arm");
        rightArm = transform.Find("Right Arm");
    }

    public void LateUpdate()
    {
        if(MenuManager.IsPaused) return;
        
        Vector3 armReachPos = Vector3.zero;
        float signedDir = 0f;
        if(transform == Variables.player)
        {
            //Gets the position that is within the reach of an arm towards the mousepos (center between left/right arm + average of the directions to point to mouse)
            armReachPos = (leftArm.transform.position + rightArm.transform.position) / 2f + 
            (Vector3)(Angle2D.GetAngle<Vector2>(leftArm.transform.position, World.mousePos) + Angle2D.GetAngle<Vector2>(rightArm.transform.position, World.mousePos)) / 2f;

            //Gets which direction the mouse is (-1 = left, 1 = right)
            signedDir = Mathf.Sign((World.mousePos - transform.position).x);
        }
        else
        {
            if(GetComponent<EnemyBehaviour>().seesPlayer)
            {
                //Gets the position that is within the reach of an arm towards the player (center between left/right arm + average of the directions to point to player)
                armReachPos = (leftArm.transform.position + rightArm.transform.position) / 2f 
                + (Vector3)(Angle2D.GetAngle<Vector2>(leftArm.transform.position, Variables.player.transform.position) 
                + Angle2D.GetAngle<Vector2>(rightArm.transform.position, Variables.player.transform.position)) / 2f;

                //Gets which direction the player is (-1 = left, 1 = right)
                signedDir = Mathf.Sign((Variables.player.transform.position - transform.position).x);
            }
            else if(GetComponent<EnemyPathfinding>().path.Count > 0 && GetComponent<EnemyPathfinding>().path[0].x != transform.position.x)
            {
                signedDir = Mathf.Sign(GetComponent<EnemyPathfinding>().path[0].x - transform.position.x);
                armReachPos = (leftArm.transform.position + rightArm.transform.position) / 2f + Vector3.right * signedDir;
            }
            else
            {
                signedDir = Mathf.Sign(transform.lossyScale.x);
                armReachPos = (leftArm.transform.position + rightArm.transform.position) / 2f + Vector3.right * signedDir;
            }
        }

        //Turns the arms towards the armReachPos
        Angle2D.TurnTo(leftArm.gameObject, armReachPos, 0f);
        Angle2D.TurnTo(rightArm.gameObject, armReachPos, 0f);

        //Sets localscales based on signedDir (player: sets x sign to signedDir. Arms: set x and y sign to signedDir to not have a flipped arm)
        transform.localScale = new(Mathf.Abs(transform.localScale.x) * signedDir, transform.localScale.y);
        leftArm.localScale = new(Mathf.Abs(leftArm.localScale.x) * signedDir, Mathf.Abs(leftArm.localScale.y) * signedDir);
        rightArm.localScale = new(Mathf.Abs(rightArm.localScale.x) * signedDir, Mathf.Abs(rightArm.localScale.y) * signedDir);
        
        //Places the gun on armReachPos and rotates it outward from the chest (center of arms)
        transform.Find("Gun").position = armReachPos;
        transform.Find("Gun").rotation = Angle2D.GetAngle<Quaternion>((leftArm.transform.position + rightArm.transform.position) / 2f, armReachPos);

        //If moving horizontally and needs to go to next frame, go to next frame. Else, stand
        if (Mathf.Abs(GetComponent<Rigidbody2D>().linearVelocity.x) > .1f && walkingFrameTimer.finished)
        {
            walkingFrameTimer.ResetTimer();
            WalkingAnim();
        }
        else if(Mathf.Abs(GetComponent<Rigidbody2D>().linearVelocity.x) < .1f)
        {
            curWalkingFrame = 0;
            walkingFrameTimer.ResetTimer();
            StandingAnim();
        }
    }

    public void WalkingAnim()
    {
        //Loops the current walking frame to not surpass spriteList count, then sets the sprite to the correct frame, then increases the frame
        curWalkingFrame = Mathf.RoundToInt(Mathf.Repeat(curWalkingFrame, spriteList.Count - 1));
        GetComponent<SpriteRenderer>().sprite = spriteList[curWalkingFrame];
        curWalkingFrame++;
    }

    public void StandingAnim()
    {
        GetComponent<SpriteRenderer>().sprite = standingSprite;
    }
}
