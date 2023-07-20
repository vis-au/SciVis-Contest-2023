using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.Input;
using SciVis.XRGroup.Scripts.Tools;
using UnityEngine;
using UnityEngine.InputSystem;

public class ToolHandler : MonoBehaviour,IInteractionModeDetector
{
    private Tools currentTool;
    private AdjustBrush brushingTool;
    private InputAction radialMenuAction;

    public InputActionAsset actions;
    public GameObject radialMenuPrefab;
    private GameObject radialMenuInstance;
    public Transform RightHand;
    private bool radialMenuActive;
    
    [SerializeField]
    private InteractionMode modeOnDetection;

    [SerializeField] private InteractionMode zoomMode;
    [SerializeField] private GameObject leftHandController;
    [SerializeField] private GameObject rightHandController;
    private List<GameObject> controllers;

    public enum Tools
    {
        BrushingTool,
        ErasingTool,
        HandTool,
        ZoomTool
    }
    
    
    // Start is called before the first frame update
    void Awake()
    {
        currentTool = Tools.HandTool;
        brushingTool = GetComponentInChildren<AdjustBrush>();
        radialMenuActive = false;

    }

    private void Start()
    {
        radialMenuAction = actions.FindActionMap("UI").FindAction("ToolSelection");
        controllers = new List<GameObject>();
        controllers.Add(leftHandController);
        controllers.Add(rightHandController);
        brushingTool.SetBrushToolActive(false);
    }

    private void OnEnable()
    {
        actions.FindActionMap("UI").Enable();
            
    }

    private void OnDisable()
    {
        actions.FindActionMap("UI").Disable();
    }

    // Update is called once per frame
    void Update()
    {
        if (radialMenuAction.IsPressed() && !radialMenuActive)
        {
            //Enable Radial Menu
            radialMenuInstance = Instantiate(radialMenuPrefab,RightHand.position + RightHand.forward*0.4f, Quaternion.identity);
            radialMenuInstance.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
            radialMenuInstance.GetComponent<RadialMenu>().rightHand = RightHand;
            radialMenuInstance.transform.forward = RightHand.position - radialMenuInstance.transform.position;
            radialMenuActive = true;
            return;
        }

        if (!radialMenuAction.IsPressed() && radialMenuActive)
        {
            radialMenuInstance.GetComponent<RadialMenu>().ExecuteSelected();
            radialMenuActive = false;
            radialMenuInstance.Destroy();
            //perform action of selected menu item
            return;
        }
        
    }


    public void ChooseTool(Tools tool)
    {
        if (currentTool == tool)
        {
            return;
        }

        switch (tool)
        {
            case Tools.BrushingTool:
            {
                brushingTool.SetBrushToolActive(true);
                GameObject playAreasParent = GameObject.FindWithTag("PlayArea");
                BrushingAndLinking2[] brushScripts = playAreasParent.GetComponentsInChildren<BrushingAndLinking2>();
                foreach (var brush in brushScripts)
                {
                    brush.isBrushing = false;
                    brush.SELECTION_TYPE = BrushingAndLinking2.SelectionType.ADD;
                }

                MeshRenderer[] renderers = brushingTool.GetComponentsInChildren<MeshRenderer>(true);
                foreach (var rend in renderers)
                {
                    rend.material.color = Color.red;
                }
                //Disable other interactors
                //Visual indicator on controller?
                currentTool = Tools.BrushingTool;
                break;
            }
            case Tools.ErasingTool:
            {
                brushingTool.SetBrushToolActive(true);
                GameObject playAreasParent = GameObject.FindWithTag("PlayArea");
                BrushingAndLinking2[] brushScripts = playAreasParent.GetComponentsInChildren<BrushingAndLinking2>();
                foreach (var brush in brushScripts)
                {
                    brush.isBrushing = false;
                    brush.SELECTION_TYPE = BrushingAndLinking2.SelectionType.SUBTRACT;
                }

                MeshRenderer[] renderers = brushingTool.GetComponentsInChildren<MeshRenderer>(true);
                foreach (var rend in renderers)
                {
                    rend.material.color = Color.white;
                }                //Same but different
                currentTool = Tools.ErasingTool;
                break;
            }
            case Tools.HandTool:
            {
                brushingTool.SetBrushToolActive(false);
                GameObject playAreasParent = GameObject.FindWithTag("PlayArea");
                BrushingAndLinking2[] brushScripts = playAreasParent.GetComponentsInChildren<BrushingAndLinking2>();
                foreach (var brush in brushScripts)
                {
                    brush.isBrushing = false;
                }
                //Disable sphere and adjust, and brushing and linking?
                //Reset Interaction Mode
                currentTool = Tools.HandTool;
                break;
            }
            case Tools.ZoomTool:
            {
                brushingTool.SetBrushToolActive(false);
                GameObject playAreasParent = GameObject.FindWithTag("PlayArea");
                BrushingAndLinking2[] brushScripts = playAreasParent.GetComponentsInChildren<BrushingAndLinking2>();
                foreach (var brush in brushScripts)
                {
                    brush.isBrushing = false;
                }
                currentTool = Tools.ZoomTool;
                
                //TODO Enable the zoom tool
                break;
            }
        }
    }

    public bool IsModeDetected()
    {
        return currentTool != Tools.HandTool;
    }

    public List<GameObject> GetControllers()
    {
        return controllers;
    }

    public InteractionMode ModeOnDetection => currentTool != Tools.ZoomTool ? modeOnDetection : zoomMode;
}
