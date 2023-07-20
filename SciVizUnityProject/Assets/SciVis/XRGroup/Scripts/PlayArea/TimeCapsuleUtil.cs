using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.SpatialManipulation;
using SciVis.XRGroup;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.Interaction.Toolkit;

namespace Assets.SciVis.XRGroup
{
    public class TimeCapsuleUtil : MonoBehaviour
    {
        public TMP_Text[] texts;
        public int simID;
        protected int cubeID;
        private GameObject comparisonPrefab;
        protected bool onShelf = false;
        public bool menuCube = false;
        protected int numOfSelectors = 0;
        public static IDictionary<SimulationType, Color32> colors = new Dictionary<SimulationType, Color32>() { { SimulationType.no_network, new Color32(102, 194, 165, 255) }, { SimulationType.disable, new Color32(252, 141, 98, 255) }, { SimulationType.stimulus, new Color32(231, 138, 195, 255) }, { SimulationType.calcium, new Color32(255, 217, 47, 255) } };

        public Specification specification;
        // Start is called before the first frame update
        protected virtual void Awake()
        {
            if (simID >= 0 && simID < 4)
            {
                SimulationType simtyp = (SimulationType)simID;
                specification = new Specification();
                specification.SimulationId = simtyp;
                ChangeSimColor(simtyp);
                SetCubeID(simID);
                SetTextLabel(""+cubeID);
            }

        }

        private void Start()
        {
            comparisonPrefab = Resources.Load<GameObject>("Prefabs/Models/BrainTimeCaptureCubeComparison");
        }

        private void Update()
        {
            
        }

        public void SetSpecification(Specification spec)
        {
            this.specification = spec;
        }
        public Specification GetSpecification()
        {
            return specification;
        }

        public void SetCubeID(int id)
        {
            this.cubeID = id;
        }

        public int GetCubeID()
        {
            return this.cubeID;
        }

        public void SetTextLabel(string text)
        {
            foreach (TMP_Text textBox in texts)
            {
                textBox.text = text;
            }
        }
        public virtual void ChangeSimColor(SimulationType i)
        {
            GameObject cube = gameObject.transform.Find("Cube")?.gameObject;
            var cubeRenderer = cube.GetComponent<Renderer>();
            cubeRenderer.material.SetColor("_InnerGlowColor", colors[i]);


        }

        private void ChangeBrain()
        {
            //go.GetComponentInChildren<BrainLoader>().SetGraphColor();
        }
        
        public virtual void OnCollisionEnter(Collision collision)
        {
            // Snap to table target on collision
            if (collision.gameObject.tag == "Target" && this.gameObject.GetComponent<ObjectManipulator>().isSelected)
            {
                //Set Play Area Specification
                IBrainSubject brainSubject = collision.gameObject.GetComponentInParent<IBrainSubject>();
                PlayAreaUtil playAreaUtil = collision.gameObject.GetComponentInParent<PlayAreaUtil>();
                playAreaUtil.CubeInserted(specification);
                //gameObject.GetComponent<ObjectManipulator>().interactorsSelecting.Remove(gameObject.GetComponent<ObjectManipulator>().interactorsSelecting[0]);
                gameObject.Destroy();


            }

            if (collision.gameObject.tag == "TimeCapsule")
            {
                
                ObjectManipulator om = gameObject.GetComponent<ObjectManipulator>();
                ObjectManipulator om2 = collision.gameObject.GetComponent<ObjectManipulator>();
                //om.interactionManager.interactorRegistered.
                //Make sure this time capsule is being manipulated by the right hand
                var interactors = om.interactorsSelecting;
                bool rightHand = false;
                foreach (var i in interactors)
                {
                    // Check that only cube in right hand spawns comparison cube
                    if (i.transform.gameObject.GetComponentInParent<ArticulatedHandController>().positionAction
                        .reference.name.Contains("RightHand"))
                    {
                        rightHand = true;
                    }
                }
                
                //Make sure the other time capsule is being manipulated by the left hand
                var othersInteractors = om2.interactorsSelecting;
                bool leftHandAlsoInteracting = false;
                foreach (var i in othersInteractors)
                {
                    // Check that only cube in right hand spawns comparison cube
                    if (i.transform.gameObject.GetComponentInParent<ArticulatedHandController>().positionAction
                        .reference.name.Contains("LeftHand"))
                    {
                        leftHandAlsoInteracting = true;
                    }
                }
                
                if ((om.IsRaySelected | om.IsGrabSelected) & (om2.IsRaySelected | om2.IsGrabSelected) & rightHand & leftHandAlsoInteracting)
                {
                    Debug.Log("Both selected");
                    GameObject cube = Instantiate(comparisonPrefab, collision.transform.position, collision.transform.rotation);
                    TimeCapsuleComparisonUtil util = cube.GetComponent<TimeCapsuleComparisonUtil>();
                    
                    
                    util.SetSpecification(specification);
                    util.SetSpecification2(collision.gameObject.GetComponent<TimeCapsuleUtil>().GetSpecification());
                    util.ChangeSimColor(util.GetSpecification().SimulationId);
                    BrainSelectionManager brainSelectionManager =
                        GameObject.FindWithTag("PlayArea").GetComponentInParent<BrainSelectionManager>();
                    int cubeIDcounter = brainSelectionManager.GetCubeCounter();
                    util.SetCubeID(cubeIDcounter);
                    brainSelectionManager.IncrementCubeCounter();
                    util.SetTextLabel("Comparison of ID:\n"+ GetCubeID() + " and " + collision.gameObject.GetComponent<TimeCapsuleUtil>().GetCubeID());
                    // Destroy old cubes? or save them somewhere?
                    Destroy(collision.gameObject);
                    Destroy(gameObject);
                }
                
            }
        }

        private void OnCollisionExit(Collision other)
        {
            if (other.gameObject.tag == "Target"){}
        }

        public virtual void OnSelectEntered(SelectEnterEventArgs args)
        {
            if (onShelf)
            {
                ShelfBehavior shelfBehavior = GetComponentInParent<ShelfBehavior>();
                if (!shelfBehavior.isEditMode() || shelfBehavior.isTimeLineMode())
                {
                    GameObject copy = Instantiate(gameObject, transform.position, transform.rotation,GameObject.FindWithTag("shelf").transform);
                    TimeCapsuleUtil copyUtil = copy.GetComponent<TimeCapsuleUtil>();
                    SolverHandler menuSolver = copy.GetComponentInChildren<SolverHandler>(true);
                    menuSolver.AdditionalOffset = new Vector3(0, 0.14f, 0);
                    copyUtil.setOnShelf(true);
                    copyUtil.SetCubeID(this.cubeID);
                    copyUtil.specification = new Specification(this.specification);
                    copyUtil.simID = this.simID;
                    gameObject.layer = 14;
                    gameObject.transform.Find("Cube").SetLayerRecursively(14);
                    //gameObject.GetComponent<Rigidbody>().isKinematic = false;
                    gameObject.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.None;
                }
            }

            if (menuCube)
            {
                GameObject copy = Instantiate(gameObject, transform.position, transform.rotation, gameObject.transform.parent);
                BrainSubject brainSubject = gameObject.GetComponentInParent<BrainSubject>();
                //copy.transform.parent = gameObject.transform.parent;
                Debug.Log("Menu cube grabbed");
                menuCube = false;
                Rigidbody rb = gameObject.GetComponent<Rigidbody>();
                rb.constraints = RigidbodyConstraints.None;
                int cubeIDcounter = gameObject.GetComponentInParent<BrainSelectionManager>().GetCubeCounter();
                SetTextLabel(""+ cubeIDcounter); // " TS: " + brainSubject.GetSpec().TimeStep
                ChangeSimColor(brainSubject.GetSpec().SimulationId);
                Specification spec = new Specification(brainSubject.GetSpec());
                spec.BrainRotation = brainSubject.gameObject.transform.GetComponentInChildren<BrainClusterVisualizer>().transform.rotation;
                spec.BrainScale = brainSubject.gameObject.transform.GetComponentInChildren<BrainClusterVisualizer>().transform.localScale;
                SetSpecification(spec);
                SetCubeID(cubeIDcounter);
                gameObject.GetComponentInParent<BrainSelectionManager>().IncrementCubeCounter();
                gameObject.transform.parent = null;
                Vector3 newScale = new Vector3(0.09f, 0.09f, 0.09f);
                StartCoroutine(ScaleObject(transform, newScale));
                gameObject.layer = 12;
                gameObject.transform.Find("Cube").SetLayerRecursively(12);

            }

            if (numOfSelectors == 0)
            {
                GameObject menu = gameObject.GetComponentInChildren<CubeSelectMenuBehavior>(true).gameObject;
                menu.SetActive(true);
                
            }
            numOfSelectors++;
            
            
        }

        protected IEnumerator<WaitForEndOfFrame> ScaleObject(Transform movedObject, Vector3 newScale, float duration = 0.5f)
        {
            float startTime = Time.time;
            float elapsedTime = 0f;
            Vector3 startingScale = Vector3.zero + movedObject.localScale;
            Vector3 menuStartOffset = new Vector3(0, 0.14f, 0);
            Vector3 menuTargetOffset = new Vector3(0, 0.024f, 0);
            SolverHandler menu = movedObject.GetComponentInChildren<SolverHandler>(true);
            while (elapsedTime < duration) {
                elapsedTime = Time.time - startTime;
                float t = elapsedTime / duration;
                movedObject.localScale = Vector3.Lerp(startingScale, newScale, t);
                menu.AdditionalOffset = Vector3.Lerp(menuStartOffset, menuTargetOffset, t);
                yield return new WaitForEndOfFrame();
            }
        }
        
        public void OnSelectExit(SelectExitEventArgs args)
        {
            numOfSelectors--;
            if (numOfSelectors == 0)
            {
                CubeSelectMenuBehavior menuBehavior = gameObject.GetComponentInChildren<CubeSelectMenuBehavior>(true);
                GameObject menu = null;
                if(menuBehavior!=null) menu = menuBehavior.gameObject;
                
                if(menu!=null) menu.SetActive(false);

                if (onShelf)
                {
                    Debug.LogWarning("On Shelf about to check duplicate");
                    ShelfBehavior shelfBehavior = GetComponentInParent<ShelfBehavior>();
                    gameObject.layer = 15;
                    gameObject.transform.Find("Cube").SetLayerRecursively(15);
                    bool duplicate = shelfBehavior.DeleteIfDuplicate(this);
                    if (!duplicate && shelfBehavior.isTimeLineMode())
                    {
                        shelfBehavior.MoveToTimeline(this,false);
                    }
                }
            }
            
            
        }

        public void setOnShelf(bool onShelf)
        {
            this.onShelf = onShelf;
        }
    }
}