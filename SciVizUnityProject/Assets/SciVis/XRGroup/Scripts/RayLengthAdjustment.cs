using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.Input;
using SciVis.XRGroup.Scripts;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class RayLengthAdjustment : MonoBehaviour
{
    public MRTKRayInteractor RayInteractor;

    public InputActionAsset actions;
    public InputActionAsset customActions;

    private InputAction touchPadClicked;

    public bool leftHand;

    private void OnEnable()
    {
        if (leftHand)
        {
            actions.FindActionMap("MRTK LeftHand").FindAction("Translate Anchor").performed += move;
        }
        else
        {
            actions.FindActionMap("MRTK RightHand").FindAction("Translate Anchor").performed += move;
            touchPadClicked = customActions.FindActionMap("BrushSelect").FindAction("RightTouchPadClicked");
        }
        
        

    }

    private void move(InputAction.CallbackContext context)
    {
        if (!leftHand && touchPadClicked.ReadValue<Vector2>().magnitude > 0)
        {
            return;
        }
        if (RayInteractor.hasSelection)
        {
            Transform anchor = RayInteractor.attachTransform; 
            
            if (Mathf.Approximately(1, 0f))
                return;

            var originPosition = RayInteractor.rayOriginTransform.position;
            var originForward = RayInteractor.rayOriginTransform.forward;
            Vector2 val = context.ReadValue<Vector2>();
            float directionAmount = val[1];
            float m_TranslateSpeed = 2;
            var resultingPosition = anchor.position + originForward * (directionAmount * m_TranslateSpeed * Time.deltaTime);

            // Check the delta between the origin position and the calculated position.
            // Clamp so it doesn't go further back than the origin position.
            var posInAttachSpace = resultingPosition - originPosition;
            var dotResult = Vector3.Dot(posInAttachSpace, originForward);

            anchor.position = dotResult > 0f ? resultingPosition : originPosition;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
