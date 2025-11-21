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
        if(transform == Objects.player)
        {
            //Gets the position that is within the reach of an arm towards the mousepos (center between left/right arm + average of the directions to point to mouse)
            Vector3 armReachPos = (leftArm.transform.position + rightArm.transform.position) / 2f + 
            (Vector3)(Angle2D.GetAngle<Vector2>(leftArm.transform.position, World.mousePos, 0f) + Angle2D.GetAngle<Vector2>(rightArm.transform.position, World.mousePos, 0f)) / 2f;

            //Turns the arms towards the armReachPos
            Angle2D.TurnTo(leftArm.gameObject, armReachPos, 0f);
            Angle2D.TurnTo(rightArm.gameObject, armReachPos, 0f);
            
            //Places the gun on armReachPos and rotates it outward from the chest (center of arms)
            transform.Find("Gun").position = armReachPos;
            transform.Find("Gun").rotation = Angle2D.GetAngle<Quaternion>((leftArm.transform.position + rightArm.transform.position) / 2f, transform.Find("Gun").position, 0f);

            //Gets which direction the mouse is (-1 = left, 1 = right)
            float signedDir = Mathf.Sign((World.mousePos - transform.position).x);

            //Sets localscales based on signedDir (player: sets x sign to signedDir. Arms: set x and y sign to signedDir to not have a flipped arm)
            transform.localScale = new(Mathf.Abs(transform.localScale.x) * signedDir, transform.localScale.y);
            leftArm.localScale = new(Mathf.Abs(leftArm.localScale.x) * signedDir, Mathf.Abs(leftArm.localScale.y) * signedDir);
            rightArm.localScale = new(Mathf.Abs(rightArm.localScale.x) * signedDir, Mathf.Abs(rightArm.localScale.y) * signedDir);

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
