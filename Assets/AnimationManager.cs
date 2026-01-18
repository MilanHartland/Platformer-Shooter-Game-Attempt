using System.Collections.Generic;
using UnityEngine;
using MilanUtils;
using UnityEngine.UIElements;

[RequireComponent(typeof(Rigidbody2D)), RequireComponent(typeof(SpriteRenderer))]
public class AnimationManager : MonoBehaviour
{
    public Sprite standingSprite;
    public List<Sprite> spriteList;

    Transform leftArm, rightArm, gun;

    int curWalkingFrame = 0;

    public float fps;

    Timer walkingFrameTimer;

    int lastFrameArmsSet = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        walkingFrameTimer = new(1f / fps);
        leftArm = transform.Find("Left Arm");
        rightArm = transform.Find("Right Arm");
        gun = transform.Find("Gun");
    }

    public void LateUpdate()
    {
        if(MenuManager.IsPaused || MissionManager.isTransitioning || PlayerManager.isDead) return;
        
        //If this is the player, point arms to mouse. If not (thus an enemy), if sees the player, point to player, otherwise point downwards
        if (transform == Variables.player)
            SetArmPointPos(World.mousePos);
        else
        {
            if(GetComponent<EnemyBehaviour>().seesPlayer)
                SetArmPointPos(Variables.player.position);
            else
                SetArmAngle(Mathf.Sign(transform.lossyScale.x) * -160f);
        }

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

    void WalkingAnim()
    {
        //Loops the current walking frame to not surpass spriteList count, then sets the sprite to the correct frame, then increases the frame
        curWalkingFrame = Mathf.RoundToInt(Mathf.Repeat(curWalkingFrame, spriteList.Count - 1));
        GetComponent<SpriteRenderer>().sprite = spriteList[curWalkingFrame];
        curWalkingFrame++;
    }

    void StandingAnim(){GetComponent<SpriteRenderer>().sprite = standingSprite;}
    
    /// <summary>Sets the angle of the arms. What it does is it places the gun at the set angle, and applies IK to the arms, where 0 is up and 90 is forward</summary>
    public void SetArmAngle(float angle)
    {
        Vector3 armReachPos = (leftArm.transform.position + rightArm.transform.position) / 2f + (Vector3)Angle2D.Convert<float, Vector2>(angle);

        SetArmPointPos(armReachPos);
    }

    /// <summary>Gets the angle to the gun, which is what the arms are pointing at</summary><returns></returns>
    public float GetArmAngle()
    {
        return gun.eulerAngles.z;
    }

    /// <summary>Sets the angle of the arms to point towards the position</summary><param name="pos"></param>
    public void SetArmPointPos(Vector3 pos)
    {
        if(lastFrameArmsSet == Time.frameCount) return;
        lastFrameArmsSet = Time.frameCount;
        
        //Gets the position that is within the reach of an arm towards the mousepos (center between left/right arm + average of the directions to point to mouse)
        Vector3 armReachPos = (leftArm.transform.position + rightArm.transform.position) / 2f + 
        (Vector3)(Angle2D.GetAngle<Vector2>(leftArm.transform.position, pos) + Angle2D.GetAngle<Vector2>(rightArm.transform.position, pos)) / 2f;

        float signedDir = Mathf.Sign((armReachPos - transform.position).x);

        //Turns the arms towards the armReachPos
        Angle2D.TurnTo(leftArm.gameObject, armReachPos, 0f);
        Angle2D.TurnTo(rightArm.gameObject, armReachPos, 0f);

        //Sets localscales based on signedDir (player: sets x sign to signedDir. Arms: set x and y sign to signedDir to not have a flipped arm)
        transform.localScale = new(Mathf.Abs(transform.localScale.x) * signedDir, transform.localScale.y);
        leftArm.localScale = new(Mathf.Abs(leftArm.localScale.x) * signedDir, Mathf.Abs(leftArm.localScale.y) * signedDir);
        rightArm.localScale = new(Mathf.Abs(rightArm.localScale.x) * signedDir, Mathf.Abs(rightArm.localScale.y) * signedDir);
        gun.localScale = new(Mathf.Abs(gun.localScale.x) * signedDir, Mathf.Abs(gun.localScale.y) * signedDir);
        
        //Places the gun on armReachPos and rotates it outward from the chest (center of arms)
        gun.position = armReachPos;
        gun.rotation = Angle2D.GetAngle<Quaternion>((leftArm.transform.position + rightArm.transform.position) / 2f, armReachPos, 0f);
    }
}
