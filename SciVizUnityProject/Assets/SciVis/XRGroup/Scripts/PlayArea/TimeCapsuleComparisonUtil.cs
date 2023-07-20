using SciVis.XRGroup;
using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.SpatialManipulation;
using TMPro;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.Interaction.Toolkit;

namespace Assets.SciVis.XRGroup
{
    public class TimeCapsuleComparisonUtil : TimeCapsuleUtil
    {
        private Specification specification2;

        // Start is called before the first frame update
        protected override void Awake()
        {
            specification2 = new Specification();
        }

        public void SetSpecification2(Specification spec)
        {
            this.specification2 = spec;
        }

        public Specification GetSpecification2()
        {
            return specification2;
        }

        public override void ChangeSimColor(SimulationType i)
        {
            // TODO: Fix color of cube2
            // TODO: Dublicate not working
            // TODO: Comparison cube ID???
            GameObject cube = gameObject.transform.Find("Cube/Cube")?.gameObject;
            GameObject cube2 = gameObject.transform.Find("Cube/Cube2")?.gameObject;

            var cubeRenderer = cube.GetComponent<Renderer>();
            var cube2Renderer = cube2.GetComponent<Renderer>();

            IDictionary<SimulationType, Color32> colors = new Dictionary<SimulationType, Color32>() { { SimulationType.no_network, new Color32(102, 194, 165, 255) }, { SimulationType.disable, new Color32(252, 141, 98, 255) }, { SimulationType.stimulus, new Color32(231, 138, 195, 255) }, { SimulationType.calcium, new Color32(255, 217, 47, 255) } };

            cubeRenderer.material.SetColor("_InnerGlowColor", colors[specification.SimulationId]);
            cube2Renderer.material.SetColor("_InnerGlowColor", colors[specification2.SimulationId]);

        }

        public override void OnCollisionEnter(Collision collision)
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
        }

        public override void OnSelectEntered(SelectEnterEventArgs args)
        {
            Debug.LogWarning("Select Entered");
            if (onShelf)
            {
                Debug.LogWarning("IsOnShelf");
                ShelfBehavior shelfBehavior = GetComponentInParent<ShelfBehavior>();
                if (!shelfBehavior.isEditMode() || shelfBehavior.isTimeLineMode())
                {
                    Debug.LogWarning("Should Make Copy");
                    GameObject copy = Instantiate(gameObject, transform.position, transform.rotation,GameObject.FindWithTag("shelf").transform);
                    TimeCapsuleUtil copyUtil = copy.GetComponent<TimeCapsuleUtil>();
                    //SolverHandler menuSolver = copy.GetComponentInChildren<SolverHandler>(true);
                    //menuSolver.AdditionalOffset = new Vector3(0, 0.14f, 0);
                    copyUtil.setOnShelf(true);
                    copyUtil.SetCubeID(this.cubeID);
                    copyUtil.specification = new Specification(this.specification);
                    ((TimeCapsuleComparisonUtil) copyUtil).SetSpecification2(new Specification(specification2));
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
                IBrainSubject brainSubject = gameObject.GetComponentInParent<IBrainSubject>();
                //copy.transform.parent = gameObject.transform.parent;
                Debug.Log("Menu cube grabbed");
                menuCube = false;
                Rigidbody rb = gameObject.GetComponent<Rigidbody>();
                rb.constraints = RigidbodyConstraints.None;
                int cubeIDcounter = gameObject.GetComponentInParent<BrainSelectionManager>().GetCubeCounter();
                SetTextLabel(""+ cubeIDcounter); // " TS: " + brainSubject.GetSpec().TimeStep
                ChangeSimColor(brainSubject.GetSpec().SimulationId);
                SetSpecification(new Specification(brainSubject.GetSpec()));
                //TODO Handle specifaction2.
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
                //GameObject menu = gameObject.GetComponentInChildren<CubeSelectMenuBehavior>(true).gameObject;
                //if(menu!=null) menu.SetActive(true);
                
            }
            numOfSelectors++;
        }
    }
}