using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameTimeCountDownBean
{
    public float timeUpdate;
    public float timeUpdateMax;
    public int numCountDown;//当前倒计时次数
    public int numCountDownMax = -1;//倒计时次数
    public Action<GameTimeCountDownBean> actionForEnd;
    public Action<GameTimeCountDownBean> actionForDestory;

    public void Update(float deltaTime)
    {
        timeUpdate += deltaTime;
        if (timeUpdate>= timeUpdateMax)
        {
            actionForEnd?.Invoke(this);
            timeUpdate = 0;
            if (numCountDownMax != -1)
            {
                numCountDown++;
                if (numCountDown >= numCountDownMax)
                {
                    actionForEnd?.Invoke(this);
                }
            }
        }
    }

    public void Clear()
    {
        timeUpdate = 0;
        timeUpdateMax = 0;
        numCountDown = 0;
        numCountDownMax = -1;
        actionForEnd = null;
        actionForDestory = null;
    }
}
