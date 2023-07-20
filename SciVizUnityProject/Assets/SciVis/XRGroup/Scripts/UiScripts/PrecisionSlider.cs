using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.UX;
using Unity.Profiling;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class PrecisionSlider : Slider
{
    private float SliderStepVal => (MaxValue - MinValue) / SliderStepDivisions;
    private Vector3 lastInteractionPoint;
    public StrengthIndicator upperStrengthIndicator;
    public StrengthIndicator lowerStrengthIndicator;
    private static readonly ProfilerMarker ProcessInteractablePerfMarker =
        new ProfilerMarker("[MRTK] PinchSlider.ProcessInteractable");
    public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
    {
        using (ProcessInteractablePerfMarker.Auto())
        {
            //base.ProcessInteractable(updatePhase);

            if (TrackCollider != null)
            {
                // Make sure our track collider is enabled if
                // we have snapToPosition enabled.
                if (SnapToPosition != TrackCollider.enabled)
                {
                    TrackCollider.enabled = SnapToPosition;
                }
            }

            if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic && isSelected)
            {
                UpdateSliderValue();
            }
        }
    }
    private float SnapSliderToStepPositions(float value)
    {
        var stepCount = value / SliderStepVal;
        var snappedValue = SliderStepVal * Mathf.RoundToInt(stepCount);
        Mathf.Clamp(snappedValue, MinValue, MaxValue);
        return snappedValue;
    }

    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);
        lastInteractionPoint = StartInteractionPoint;
        upperStrengthIndicator.gameObject.SetActive(true);
        lowerStrengthIndicator.gameObject.SetActive(true);
    }

    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        base.OnSelectExited(args);
        upperStrengthIndicator.gameObject.SetActive(false);
        lowerStrengthIndicator.gameObject.SetActive(false);
    }

    private  void UpdateSliderValue()
    {
        Vector3 interactionPoint = interactorsSelecting[0].GetAttachTransform(this).position;
        Vector3 interactorDelta = interactionPoint - lastInteractionPoint;

        float yDist = (interactionPoint - StartInteractionPoint).y;
        bool positive = yDist >= 0;
        float dampeningFactor = Math.Max((float)Math.Exp(Math.Abs(yDist) * 30), 1.0f); //interactorDelta.y * 100 * interactorDelta.y
        var handDelta = Vector3.Dot(SliderTrackDirection.normalized, interactorDelta) / dampeningFactor;

        float unsnappedValue = Mathf.Clamp(Value + handDelta / (SliderTrackDirection.magnitude) , MinValue, MaxValue);

        Value = UseSliderStepDivisions ? SnapSliderToStepPositions(unsnappedValue) : unsnappedValue;

        lastInteractionPoint = interactionPoint;
        
        //Update strength indicators
        int max = 20000;
        if (positive)
        {
            upperStrengthIndicator.IndicateStrength(dampeningFactor,0,max);
            lowerStrengthIndicator.IndicateStrength(-dampeningFactor,0,max);
        }
        else
        {
            upperStrengthIndicator.IndicateStrength(-dampeningFactor,0,max);
            lowerStrengthIndicator.IndicateStrength(dampeningFactor,0,max);
        }
    }
}
