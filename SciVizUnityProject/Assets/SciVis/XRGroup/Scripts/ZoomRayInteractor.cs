using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.Input;
using SciVis.XRGroup.Scripts;
using UnityEngine;
using UnityEngine.InputSystem;

public class ZoomRayInteractor : RayGrabInteractor
{
    [SerializeField] private InputActionAsset actions;
    private Transform leftController;
    private Transform rightController;
    private bool active;
    protected override void Awake()
    {
        base.Awake();
        leftController = GameObject.FindWithTag("LeftHand").transform;
        rightController = GameObject.FindWithTag("RightHand").transform;
        actions.FindActionMap("UI").FindAction("ZoomActionRight").performed += (x) => { Zoom(true); };
        actions.FindActionMap("UI").FindAction("ZoomActionLeft").performed += (x) => { Zoom(false); };
        active = false;
    }

    private void Zoom(bool rightHand)
    {
        if (!active)
        {
            return;
        }
        Ray ray;
        if (rightHand)
        {
            ray = new Ray(rightController.position, rightController.forward);
        }
        else
        {
            ray = new Ray(leftController.position, leftController.forward);
        }
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 2000, LayerMask.GetMask("ZoomLayer")))
        {
            BrainClusterVisualizer bcv = hit.collider.gameObject.GetComponentInParent<BrainClusterVisualizer>();
            if (bcv == null)
            {
                return;
            }

            bcv.Zoom(hit);
        }
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        GetComponent<MRTKRayReticleVisual>().enabled = false;
        active = false;
    }

    protected override void OnEnable()
    {
        GetComponent<MRTKRayReticleVisual>().enabled = true;
        base.OnEnable();
        active = true;
    }
}
