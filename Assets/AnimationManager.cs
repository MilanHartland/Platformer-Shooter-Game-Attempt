using System.Collections.Generic;
using UnityEngine;
using MilanUtils;

public class AnimationManager : MonoBehaviour
{
    public Sprite standingSprite;
    public List<Sprite> spriteList;

    int curWalkingFrame = 0;

    public float fps;

    Timer walkingFrameTimer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        walkingFrameTimer = new(fps);
    }

    public void LateUpdate()
    {
        if(transform == Objects.player)
        {
            if (Input.GetKey(KeyCode.A) && walkingFrameTimer.finished)
            {
                walkingFrameTimer.ResetTimer();
                transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y);
                WalkingAnim();
            }
            else if (Input.GetKey(KeyCode.D) && walkingFrameTimer.finished)
            {
                walkingFrameTimer.ResetTimer();
                transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y);
                WalkingAnim();
            }
            else if(!Input.GetKey(KeyCode.D) && !Input.GetKey(KeyCode.A))
            {
                curWalkingFrame = 0;
                walkingFrameTimer.ResetTimer();
                StandingAnim();
            }
        }
    }

    public void WalkingAnim()
    {
        GetComponent<SpriteRenderer>().sprite = spriteList[curWalkingFrame];
        curWalkingFrame++;
    }

    public void StandingAnim()
    {
        GetComponent<SpriteRenderer>().sprite = standingSprite;
    }
}
