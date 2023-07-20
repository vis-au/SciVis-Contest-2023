using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SciVis.XRGroup.Scripts.Tools
{
    public class AdjustBrush : MonoBehaviour
    {
        // Adjust brush size and location
        private InputAction brushAction;
        private InputAction rightTouchPadClickedAction;
        private InputAction brushingActivationAction;
        public InputActionAsset actions;
        private GameObject brush;
        private GameObject stick;
        public Transform rightHandTransform;
        private bool isBrushing;
        private Dictionary<BrushingAndLinking2, HashSet<int>> newlyBrushedNeuronIds;
        private Dictionary<BrushingAndLinking2, HashSet<int>> newlyBrushedClusterIndices;
        void Awake()
        {
            brush = transform.Find("brushingSphere").gameObject;
            stick = transform.Find("Cylinder").gameObject;
            isBrushing = true;
            Debug.LogWarning("AWAKE BRUSH CALLED");
            brushAction = actions.FindActionMap("BrushSelect").FindAction("AdjustBrush");
            rightTouchPadClickedAction = actions.FindActionMap("BrushSelect").FindAction("RightTouchPadClicked");
            brushingActivationAction = actions.FindActionMap("BrushSelect").FindAction("ActivateBrushing");
        }
        private void OnEnable()
        {
            actions.FindActionMap("BrushSelect").Enable();
            
        }

        private void OnDisable()
        {
            actions.FindActionMap("BrushSelect").Disable();
        }

        public void SetBrushToolActive(bool active)
        {
            Debug.LogWarning("TOGGLE BRUSH CALLED");
            brush.SetActive(active);
            stick.SetActive(active);
            isBrushing = active;
            UpdateBrush();
        }

        // Update is called once per frame
        void Update()
        {
            
            if (brushAction.IsPressed() && isBrushing && rightTouchPadClickedAction.ReadValue<Vector2>().magnitude<=0)
            {
                UpdateBrush();
            }

            if (isBrushing&& brushingActivationAction.WasPressedThisFrame())
            {
                GameObject playAreasParent = GameObject.FindWithTag("PlayArea");
                BrushingAndLinking2[] brushScripts = playAreasParent.GetComponentsInChildren<BrushingAndLinking2>();
                brush.GetComponent<Collider>().enabled = true;
                newlyBrushedNeuronIds = new Dictionary<BrushingAndLinking2, HashSet<int>>();
                newlyBrushedClusterIndices = new Dictionary<BrushingAndLinking2, HashSet<int>>();
                foreach (var brush in brushScripts)
                {
                    brush.isBrushing = true;
                }
                
                MeshRenderer[] renderers = brush.GetComponentsInChildren<MeshRenderer>(true);
                foreach (var rend in renderers)
                {
                    rend.material.EnableKeyword("_Glossiness");
                    rend.material.EnableKeyword("_Metallic");
                    rend.material.SetFloat("_Metallic", 1.0f);
                    rend.material.SetFloat("_Glossiness", 0.6f);
                }     
            }

            if (isBrushing&& brushingActivationAction.WasReleasedThisFrame())
            {
                GameObject playAreasParent = GameObject.FindWithTag("PlayArea");
                BrushingAndLinking2[] brushScripts = playAreasParent.GetComponentsInChildren<BrushingAndLinking2>();
                brush.GetComponent<Collider>().enabled = false;
                foreach (var brush in brushScripts)
                {
                    brush.isBrushing = false;
                    bool brushingNotErasing = brush.SELECTION_TYPE == BrushingAndLinking2.SelectionType.ADD;
                    if (newlyBrushedNeuronIds.ContainsKey(brush))
                    {
                        brush.SetBrushedIndices(newlyBrushedNeuronIds[brush], brushingNotErasing);
                    }

                    int granularity = brush.gameObject.GetComponentInParent<BrainSubject>().spec.TerrainClusterLevel;
                    BrainClusterVisualizer clusterVis = brush.gameObject.GetComponent<BrainClusterVisualizer>();

                    if (newlyBrushedClusterIndices.ContainsKey(brush) && brushingNotErasing)
                    {
                        clusterVis.ExplodeParentsOfCluster(newlyBrushedClusterIndices[brush],granularity);
                    }
                    
                    //Update brainsubjects with selection
                    BrainSubject subject = brush.GetComponentInParent<BrainSubject>();
                    subject.SetBrushedIndicies(brush.GetBrushedIndices());
                }
                
                MeshRenderer[] renderers = brush.GetComponentsInChildren<MeshRenderer>(true);
                foreach (var rend in renderers)
                {
                    rend.material.EnableKeyword("_Glossiness");
                    rend.material.EnableKeyword("_Metallic");
                    rend.material.SetFloat("_Metallic", 0);
                    rend.material.SetFloat("_Glossiness", 0.5f);
                }  
            }

        }
        private void UpdateBrush()
        {
            Vector2 val = brushAction.ReadValue<Vector2>();
            float sizeAmount = val[0];

            // Scale if X-axis is not 0, else move
            if (sizeAmount != 0)
            {
                // Debug.Log("[Brush] " + val.ToString());

                // Change brush size
                float minSize = 0.02f;
                float m_ScaleSpeed = 0.4f;
                var resultingScale = brush.transform.localScale + Vector3.one * (sizeAmount * m_ScaleSpeed * Time.deltaTime);
                resultingScale = Vector3.Max(resultingScale, Vector3.one * minSize);
                brush.transform.localScale = resultingScale;
                
                GameObject playAreasParent = GameObject.FindWithTag("PlayArea");
                BrushingAndLinking2[] brushScripts = playAreasParent.GetComponentsInChildren<BrushingAndLinking2>();
                foreach (var brushingAndLinking2 in brushScripts)
                {
                    brushingAndLinking2.brushRadius = this.brush.transform.localScale.x;
                }
            }
            else
            {
                var originPosition = rightHandTransform.position + 0.13f*rightHandTransform.forward;
                Vector3 forwardVector = rightHandTransform.forward;
                
                // Brush position
                float directionAmount = val[1];
                float m_TranslateSpeed = 1;
                var resultingPosition = brush.transform.position + forwardVector * (directionAmount * m_TranslateSpeed * Time.deltaTime);

                // Check the delta between the origin position and the calculated position.
                // Clamp so it doesn't go further back than the origin position.
                var posInAttachSpace = resultingPosition - originPosition;
                var dotResult = Vector3.Dot(posInAttachSpace, forwardVector);

                brush.transform.position = dotResult > 0f ? resultingPosition : originPosition;
                
                //Stick
                Vector3 stickPosition = (brush.transform.position + rightHandTransform.position) / 2;
                float stickLength = (brush.transform.position - rightHandTransform.position).magnitude;
                stick.transform.position = stickPosition;
                stick.transform.localScale = new Vector3(0.01f, stickLength/2, 0.01f);
            }
        }


        public void BrushInBrain(HashSet<int> brushedIndices, GameObject playArea)
        {
            BrushingAndLinking2[] brushScripts = playArea.GetComponentsInChildren<BrushingAndLinking2>(true);
            foreach (var brush in brushScripts)
            {
                if (!brush.isBrushing)
                {
                    return;
                }
                brush.SetBrushedIndices(brushedIndices, brush.SELECTION_TYPE == BrushingAndLinking2.SelectionType.ADD);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            TerrainViewSelectionHandler selectionHandler = other.GetComponent<TerrainViewSelectionHandler>();
            if (selectionHandler == null)
            {
                return;
            }
            
            /*BrushingAndLinking2[] brushScripts = selectionHandler.getPlayArea().GetComponentsInChildren<BrushingAndLinking2>(true);
            bool brushingNotErasing = false;
            foreach (var brush in brushScripts)
            {
                if (!brush.isBrushing)
                {
                    return;
                }

                brushingNotErasing = brush.SELECTION_TYPE == BrushingAndLinking2.SelectionType.ADD;
                brush.SetBrushedIndices(selectionHandler.GetNodeIds(), brushingNotErasing);
            }
            */
            

            BrushingAndLinking2 bl2 = selectionHandler.getPlayArea().GetComponentInChildren<BrushingAndLinking2>(true);
            if (bl2 != null)
            {
                if (newlyBrushedNeuronIds.ContainsKey(bl2))
                {
                    newlyBrushedNeuronIds[bl2].UnionWith(selectionHandler.GetNodeIds());
                    newlyBrushedClusterIndices[bl2].Add(selectionHandler.GetClusterId());
                }
                else
                {
                    newlyBrushedNeuronIds[bl2] = new HashSet<int>(selectionHandler.GetNodeIds());
                    newlyBrushedClusterIndices[bl2] = new HashSet<int>(selectionHandler.GetClusterId());
                }
                selectionHandler.Brush(bl2.SELECTION_TYPE == BrushingAndLinking2.SelectionType.ADD);
            }
            
        }
    }
}
