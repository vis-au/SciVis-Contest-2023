using System;
using System.Collections;
using System.Collections.Generic;
using Assets.SciVis.XRGroup;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.SpatialManipulation;
using Microsoft.MixedReality.Toolkit.UX;
using SciVis.XRGroup.Scripts;
using TMPro;
using Unity.Mathematics;
using Unity.XR.CoreUtils;
using UnityEngine;
using Random = UnityEngine.Random;

public class ShelfBehavior : MonoBehaviour
{
    private bool editMode;
    private bool timelineMode;
    private Dictionary<int, (Vector3, Quaternion)> oldCubePositions;
    private Dictionary<SimulationType,GameObject> timeLines;
    private PressableButton switchToTimelineMode;

    private PressableButton editModeToggleButton;
    
    public AnimationCurve curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
    // Start is called before the first frame update
    void Start()
    {
        TimeCapsuleUtil[] startCubes = GetComponentsInChildren<TimeCapsuleUtil>();
        foreach (var capsule in startCubes)
        {
            capsule.transform.parent = transform;
        }

        PressableButton[] buttons = GetComponentsInChildren<PressableButton>();
        editModeToggleButton = buttons[0];
        switchToTimelineMode = buttons[1];

        timeLines = new Dictionary<SimulationType, GameObject>();
        var values = (SimulationType[])Enum.GetValues(typeof(SimulationType));
        for (int i = 0; i < 4; i++)
        {
            timeLines.Add(values[i], transform.Find("TimeLineShelfContainer/TimeLineShelf/TimeLine" + i).gameObject);
        }
        
        editModeToggleButton.OnClicked.AddListener(()=>changeMode(editModeToggleButton.IsToggled));
        switchToTimelineMode.OnClicked.AddListener(()=>timeLineToggle());

    }

    private void timeLineToggle()
    {
        timelineMode = !timelineMode;
        if (timelineMode)
        {
            oldCubePositions = new Dictionary<int, (Vector3, Quaternion)>();
            editModeToggleButton.transform.parent.gameObject.SetActive(false);
            switchToTimelineMode.GetComponentInChildren<TextMeshProUGUI>().SetText("Switch to Shelf Mode");

            foreach (TimeCapsuleUtil cubeUtil in GetComponentsInChildren<TimeCapsuleUtil>())
            {
                MoveToTimeline(cubeUtil,true);
            }
            
            transform.Find("NormalShelf").gameObject.SetActive(false);
            transform.Find("TimeLineShelfContainer").gameObject.SetActive(true);
        }
        else
        {
            editModeToggleButton.transform.parent.gameObject.SetActive(true);
            switchToTimelineMode.GetComponentInChildren<TextMeshProUGUI>().SetText("Switch to Timeline Mode");
            foreach (TimeCapsuleUtil cubeUtil in GetComponentsInChildren<TimeCapsuleUtil>())
            {
                GameObject cube = cubeUtil.gameObject;
                cube.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;
                (Vector3 pos, Quaternion rot) oldCubePlacement = oldCubePositions[cube.GetComponent<TimeCapsuleUtil>().GetCubeID()];
                Vector3 newPosition =  oldCubePlacement.pos;
                cube.transform.rotation = oldCubePlacement.rot;
                StartCoroutine(MoveObject(curve,cube.transform,newPosition));
            }
            
            transform.Find("NormalShelf").gameObject.SetActive(true);
            transform.Find("TimeLineShelfContainer").gameObject.SetActive(false);
        }
    }

    public void MoveToTimeline(TimeCapsuleUtil timeCapsuleUtil, bool useOldPosition)
    {
        GameObject cube = timeCapsuleUtil.gameObject;
        Transform timeline = timeLines[timeCapsuleUtil.GetSpecification().SimulationId].transform;

        Vector3 oldCubePosition = cube.transform.position;
        Quaternion oldCubeRotation = cube.transform.rotation;

        if (useOldPosition)
        {
            oldCubePositions.TryAdd(timeCapsuleUtil.GetCubeID(), (oldCubePosition,oldCubeRotation));
        }
        else //Use dummy position
        {
            Transform target = GameObject.FindWithTag("NewPos").transform;
            oldCubePositions.TryAdd(timeCapsuleUtil.GetCubeID(), (target.position, target.rotation));
        }

        Vector3 newPosition =  timeline.position+(timeCapsuleUtil.GetSpecification().NeuronTimeStep/500000f * timeline.forward);
        cube.transform.rotation = timeline.rotation;
                
        StartCoroutine(MoveObject(curve,cube.transform,newPosition));
                
        cube.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeAll;
    }
    
    public IEnumerator<WaitForEndOfFrame> MoveObject(AnimationCurve curve, Transform movedObject, Vector3 newPosition, bool deleteInEnd = false, float duration = 1f)
    {
        movedObject.GetComponent<Rigidbody>().isKinematic = true;
        
        if (deleteInEnd)
        {
            movedObject.gameObject.SetLayerRecursively(14); //Make it a copy cube, so it does not interact with shelf cubes.
        }
        
        float startTime = Time.time;
        float elapsedTime = 0f;
        Vector3 startingPosition = Vector3.zero + movedObject.position;
        while (elapsedTime < duration) {
            elapsedTime = Time.time - startTime;
            float t = curve.Evaluate(elapsedTime / duration);
            movedObject.position = Vector3.Lerp(startingPosition, newPosition, t);
            yield return new WaitForEndOfFrame();
        }
        movedObject.GetComponent<Rigidbody>().isKinematic = false;
        if (deleteInEnd) movedObject.gameObject.Destroy();
    }
    private void changeMode(bool isToggled)
    {
        editMode = isToggled;
        Material lightMaterial = transform.Find("NormalShelf/lights").GetComponent<MeshRenderer>().material;
        lightMaterial.EnableKeyword("_EmissionColor");
        if (isToggled)
        {
            lightMaterial.SetColor("_EmissionColor", Color.black);
        }
        else
        {
            lightMaterial.SetColor("_EmissionColor", new Color(0, 168, 0));
        }
    }

    private void OnTriggerEnter(Collider collision)
    {
        
        TimeCapsuleUtil timeCapsule = collision.gameObject.GetComponent<TimeCapsuleUtil>();
        if (timeCapsule == null) return;
        (bool duplicate, Transform trans) res = CheckDuplicate(timeCapsule);
        if (res.duplicate && !timeCapsule.gameObject.GetComponent<ObjectManipulator>().isSelected && timeCapsule.gameObject.layer!=15)
        {//Should delete cube
            Vector3 newPosition = res.trans.position;
            Quaternion newRotation = res.trans.rotation;
            Collider[] colliders = collision.GetComponentsInChildren<Collider>();
            foreach (var collider in colliders)
            {
                Destroy(collider);
            }
            StartCoroutine(MoveObject(curve, collision.transform, newPosition, true, duration: 0.5f));
        }
        else
        {
            collision.transform.parent = gameObject.transform;
            collision.gameObject.layer = 15; //ShelfCube layer
            collision.transform.Find("Cube").gameObject.SetLayerRecursively(15);
            timeCapsule.setOnShelf(true);
            if (isTimeLineMode() && !timeCapsule.gameObject.GetComponent<ObjectManipulator>().isSelected && !oldCubePositions.ContainsKey(timeCapsule.GetCubeID()))
            {
                //If we are in timeline mode, and the cubes is not being manipulated by user, and it is not currently moving, start animation.
                MoveToTimeline(timeCapsule,false);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        TimeCapsuleUtil timeCapsule = other.gameObject.GetComponent<TimeCapsuleUtil>();
        if (timeCapsule == null) return;
        if (!timeCapsule.GetComponent<ObjectManipulator>().isSelected)
        {
            //If the cube is not selected while exiting the shelf, we assume it is an accident
            //and the cube is not removed from the shelf.
            return;
        }
        other.transform.parent = null;
        other.gameObject.layer = 12; //Cube layer
        other.transform.Find("Cube").gameObject.SetLayerRecursively(12);
        timeCapsule.setOnShelf(false);
    }

    public bool isEditMode()
    {
        return editMode;
    }

    public bool isTimeLineMode()
    {
        return timelineMode;
    }

    public (bool,Transform) CheckDuplicate(TimeCapsuleUtil incomingCapsuleUtil)
    {
        bool duplicate = false;
        Transform dupTrans = null;
        foreach (TimeCapsuleUtil util in GetComponentsInChildren<TimeCapsuleUtil>())
        {
            GameObject cubeOnShelf = util.gameObject;
            
            if (util.GetCubeID() == incomingCapsuleUtil.GetCubeID() && cubeOnShelf != incomingCapsuleUtil.gameObject)
            {
                duplicate =  true;
                dupTrans = util.transform;
                break;
            }
        }

        return (duplicate, dupTrans);
    }

    public bool DeleteIfDuplicate(TimeCapsuleUtil incomingCapsuleUtil)
    {
        (bool duplicate, Transform trans) res = CheckDuplicate(incomingCapsuleUtil);
        if (!res.duplicate)
        {
            return false;
        }
        Vector3 newPosition = res.trans.position;
        Quaternion newRotation = res.trans.rotation;
        /*Collider[] colliders = incomingCapsuleUtil.GetComponentsInChildren<Collider>();
        foreach (Collider collider in colliders)
        {
            Destroy(collider);
        }*/
        //TODO Fix the errors associated with this move operation without creating jitter
        StartCoroutine(MoveObject(curve, incomingCapsuleUtil.transform, newPosition, true,duration:0.5f));
        return true;
    }
}
