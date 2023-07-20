using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class RadialMenuItems : MonoBehaviour
{
    public Dictionary<Sprite, UnityAction> Items;
    // Start is called before the first frame update
    void Awake()
    {
        GameObject cameraOffset = GameObject.FindWithTag("CameraOffset");
        ToolHandler toolHandler = cameraOffset.GetComponent<ToolHandler>();
        
        Items = new Dictionary<Sprite, UnityAction>();
        Sprite brushingIcon = Resources.Load<Sprite>("Flat Icons [Free]/Icon_Pen_F");
        UnityAction brushing = () => {toolHandler.ChooseTool(ToolHandler.Tools.BrushingTool); };
        Items.Add(brushingIcon, brushing);
        
        Sprite erasingIcon = Resources.Load<Sprite>("Flat Icons [Free]/Icon_Erase_F");
        UnityAction erasing = () => {toolHandler.ChooseTool(ToolHandler.Tools.ErasingTool); };
        Items.Add(erasingIcon, erasing);
        
        Sprite handIcon = Resources.Load<Sprite>("Flat Icons [Free]/Icon_Move_F");
        UnityAction hand = () => {toolHandler.ChooseTool(ToolHandler.Tools.HandTool); };
        Items.Add(handIcon, hand);
        
        Sprite zoomIcon = Resources.Load<Sprite>("Flat Icons [Free]/Icon_Search_F");
        UnityAction zoom = () => {toolHandler.ChooseTool(ToolHandler.Tools.ZoomTool); };
        Items.Add(zoomIcon, zoom);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
