using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class StrengthIndicator : MonoBehaviour
{
    private List<RawImage> ticks;

    private void Awake()
    {
        ticks = new List<RawImage>();
        ticks = GetComponentsInChildren<RawImage>().ToList();
        ticks.Reverse();
    }

    // Start is called before the first frame update
    void Start()
    {
        reset();
    }

    public void IndicateStrength(float currentValue, float minValue, float maxValue)
    {
        if (ticks == null)
        {
            ticks = new List<RawImage>();
            ticks = GetComponentsInChildren<RawImage>().ToList();
            ticks.Reverse();
        }
        
        
        int numTicks = (int) Math.Log(Math.Pow(currentValue, ticks.Count), maxValue);
        numTicks = Math.Min(numTicks, ticks.Count);
        if(currentValue < 0) numTicks *= (-1);
        reset();
        
        for (int i = 0; i < numTicks; i++)
        {
            var tick = ticks[i];
            tick.color = new Color(tick.color.r, tick.color.g, tick.color.b, 1);
        }
    }

    private void reset()
    {
        foreach (RawImage tick in ticks)
        {
            tick.color = new Color(tick.color.r, tick.color.g, tick.color.b,0.1f);
        }
    }
}
