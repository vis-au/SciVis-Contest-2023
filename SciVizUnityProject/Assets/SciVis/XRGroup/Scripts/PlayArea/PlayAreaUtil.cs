using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.SpatialManipulation;
using Microsoft.MixedReality.Toolkit.UX;
using SciVis.XRGroup.Scripts;
using SciVis.XRGroup.Scripts.Tools;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SciVis.XRGroup{
    public class PlayAreaUtil : MonoBehaviour, IBrainObserver
    {
        private GameObject brain;
        private GameObject terrain;
        private GameObject handMenu;
        private GameObject terrainHandMenu;

        private BrainSubject brainSubject;

        private GameObject selectedIndicator;
        private GameObject hoverIndicator;
        private StartStopMenuBehaviour startStopMenu;
        private SelectionPanelBehavior[] selectionMenus;
        private Transform brush;
        private bool iAmVisGuy = false;
        public bool inUse;

        // Start is called before the first frame update
        void Awake()
        {
            brush = GameObject.FindWithTag("CameraOffset").GetComponentInChildren<AdjustBrush>(true).GetComponentInChildren<MeshRenderer>(true).transform;
            brainSubject = GetComponent<BrainSubject>();
            handMenu = transform.Find("BrainParent/HandMenu").gameObject;
            terrainHandMenu = transform.Find("Terrainview/HandMenu").gameObject;
            startStopMenu = GetComponentInChildren<StartStopMenuBehaviour>(true);
            selectionMenus = GetComponentsInChildren<SelectionPanelBehavior>(true);
            if (!iAmVisGuy)
            {
                handMenu = transform.Find("BrainParent/HandMenu").gameObject;
                terrainHandMenu = transform.Find("Terrainview/HandMenu").gameObject;
                handMenu.GetComponent<SolverHandler>().TransformOverride = GetComponentInParent<MenuToggle>().leftHandController.transform;
                terrainHandMenu.GetComponent<SolverHandler>().TransformOverride = GetComponentInParent<MenuToggle>().leftHandController.transform;
            }
            brain = gameObject.GetComponentInChildren<BrainSelectionInteractable>().gameObject;
            terrain = gameObject.GetComponentInChildren<TerrainSubject>(true).gameObject;
            brainSubject.Attach(this);
        }

        private void Start()
        {
            initBrushingAndLinking();
            //brainSubject.MakeBrainLegend();
        }

        public void Deactivate()
        {
            TimedFlag t = new TimedFlag();
            t.Initialize(true);
            ToggleBrainView(t);
            TimedFlag f = new TimedFlag();
            f.Initialize(false);
            ToggleTerrainView(f);
            
            startStopMenu.transform.Find("Outer/checkBoxHorizontal/CheckBox").GetComponent<PressableButton>().ForceSetToggled(false);
            terrainHandMenu.transform.Find("TerrainHandStartStopTimeMenu/Outer/checkBoxHorizontal/CheckBox")
                .GetComponent<PressableButton>().ForceSetToggled(true);
            
            Transform brainView = brain.transform.Find("Brain");
            brainView.position = new Vector3(1000, 1000, 1000);
            inUse = false;

            brainView.GetComponent<BrainClusterVisualizer>().CollapseAllClusters();
            
            gameObject.GetComponentInParent<BrainSelectionManager>().Select(null);
        }

        private void initBrushingAndLinking()
        {
            BrushingAndLinking2[] brushes= GetComponentsInChildren<BrushingAndLinking2>();
            foreach (var brushingAndLinking2 in brushes)
            {
                brushingAndLinking2.input1 = brush;
                brushingAndLinking2.input2 = brush;
                brushingAndLinking2.brushRadius = brush.localScale.x;
            }
        }

        public void SetColor(SimulationType i)
        {
            var planeRenderer = gameObject.GetComponent<Renderer>();
            IDictionary<SimulationType, Color32> colors = new Dictionary<SimulationType, Color32>() { { SimulationType.no_network, new Color32(102, 194, 165, 255) }, { SimulationType.disable, new Color32(252, 141, 98, 255) }, { SimulationType.stimulus, new Color32(231, 138, 195, 255) }, { SimulationType.calcium, new Color32(255, 217, 47, 255) } };
            planeRenderer.material.SetColor("_InnerGlowColor", colors[i]);

        }

        public void CubeInserted(Specification specification)
        {
            //Spawn Pop-up
            DialogPool pool = transform.Find("BrainParent/HandMenu/HandStartStopTimeMenu").GetComponent<DialogPool>();
            pool.DialogPrefab.transform.localScale = new Vector3(2, 2, 2);
            SetUIActiveState(false);
            CustomDialog dialog = (CustomDialog) pool.Get();
                dialog.SetFormatPaintButton("WHATEVER", (args)=>FormatPaint(specification))
                .SetHeader("Load Capsule")
                .SetBody("What would you like to load?")
                .SetPositive("Everything", (args)=>LoadAll(specification))
                .SetNegative("Simulation and timestep", (args)=>LoadTimeAndSim(specification))
                .SetNeutral("Cancel", (args) => SetUIActiveState(true))
                .Show();
            //Update Brain Specification based on result
            
            //Update TimeStartStopUI
            
            //Update Other UI menus (optionally)
        }

        private void SetUIActiveState(bool active)
        {
            PressableButton[] buttons = transform.GetComponentsInChildren<PressableButton>(true);
            foreach (PressableButton button in buttons)
            {
                button.enabled = active;
            }

            Slider[] sliders = transform.GetComponentsInChildren<Slider>(true);
            foreach (Slider slider in sliders)
            {
                slider.enabled = active;
            }
        }

        private void LoadTimeAndSim(Specification spec)
        {
            //Re-enable other UI
            SetUIActiveState(true);
            
            //Set Time Slider
            StartStopMenuBehaviour startMenu = transform.GetComponentInChildren<StartStopMenuBehaviour>(true);
            startMenu.SetValue(spec.NeuronTimeStep);
            
            brainSubject.SetSimulation(spec.SimulationId);
            startMenu.SetSimulation(spec.SimulationId);
        }

        private void LoadScaleAndRotation(Specification spec){
            Transform brainTransform = brainSubject.transform.GetComponentInChildren<BrainClusterVisualizer>().gameObject.transform;
            brainTransform.rotation = spec.BrainRotation;
            brainTransform.localScale = spec.BrainScale;
        }

        private void FormatPaint(Specification spec)
        {
            SetUIActiveState(true);
            LoadScaleAndRotation(spec);
            gameObject.GetComponentInChildren<BrushingAndLinking2>(true).SetBrushedIndices(brainSubject.spec.BrushedIndicies, false);
            Specification newSpec = new Specification(spec);
            newSpec.SimulationId = brainSubject.spec.SimulationId;
            newSpec.NeuronTimeStep = brainSubject.spec.NeuronTimeStep;
            newSpec.SynapseTimeStep = brainSubject.spec.SynapseTimeStep;
            brainSubject.SetSpec(newSpec);
            EncodingMenuBehaviour encoding = transform.GetComponentInChildren<EncodingMenuBehaviour>(true);
            encoding.SetColorDropdownValue(spec.NeuronColorEncoding.value);
            encoding.SetSizeDropdownValue(spec.NeuronSizeEncoding.value);
            encoding.SetDivergentColorScale(spec.DivergentColorScale);
            encoding.SetLocalColorScale(spec.LocalColorScale);
            FormatClusterExplosion();

            FilterMenuBehaviour filterMenu = transform.GetComponentInChildren<FilterMenuBehaviour>(true);
            filterMenu.ClearAllUIFilters();
            foreach (NeuronFilter filter in brainSubject.GetSpec().Filters)
            {
                filterMenu.AddFilterUI(filter);
            }
            
            TerrainEncodingMenuBehaviour terrainEncodingMenu = transform.GetComponentInChildren<TerrainEncodingMenuBehaviour>(true);
            terrainEncodingMenu.SetAggregationTypeValue(spec.AggregationType);
            terrainEncodingMenu.SetClusterLevelValue(spec.TerrainClusterLevel);
            terrainEncodingMenu.SetColorPosDropdownValue(spec.TerrainEncoding.value);
            gameObject.GetComponentInChildren<BrushingAndLinking2>(true).SetBrushedIndices(spec.BrushedIndicies, true);
            brainSubject.NotifySelection();
        }

        private async void FormatClusterExplosion()
        {
            BrainClusterVisualizer bcv = brainSubject.transform.GetComponentInChildren<BrainClusterVisualizer>();
            Specification localSpec = new Specification(brainSubject.GetSpec());
            await bcv.CollapseAllClusters();
            await bcv.ExplodeAllClustersFromList(localSpec.ExplodedClusters);
            brainSubject.spec.ExplodedClusters = localSpec.ExplodedClusters;
        }

        public void LoadAll(Specification spec)
        {
            LoadTimeAndSim(spec);
            FormatPaint(spec);
        }

        public Coroutine PlaySimulation()
        {
            return StartCoroutine(Play());
        }
        private IEnumerator<WaitForSeconds> Play()
        {
            Slider slider = startStopMenu.GetSlider();
            float newValue = slider.Value + 1 / 10000f;
            while (newValue < 1)
            {
                startStopMenu.StepForward();
                newValue = slider.Value + 1 / 10000f;
                yield return new WaitForSeconds(1.0f);
            }

            startStopMenu.playing = false;
        }

        public void ObserverUpdateSynapses(IBrainSubject subject)
        {
            //Do nothing
        }

        public void ObserverUpdateNeurons(IBrainSubject subject)
        {
            //Do nothing
        }
        public void ObserverUpdateTerrain(IBrainSubject brainSubject)
        {
            // Do nothing
        }

        public void ObserverUpdateConvexHull(IBrainSubject subject)
        {
            //Do nothing
        }

        public void ObserverUpdateSplines(IBrainSubject subject)
        {
            //Nothing
        }

        public async void ObserverUpdateSelection(IBrainSubject brainSubject)
        {
            if (brainSubject.GetSpec().BrushedIndicies.Count == 0)
            {
                foreach (SelectionPanelBehavior selectionPanelBehavior in selectionMenus)
                {
                    selectionPanelBehavior.gameObject.SetActive(false);
                }
            }
            else
            {
                Specification spec = brainSubject.GetSpec();
                if (spec.NeuronColorEncoding.value == NeuronAttributeType.None)
                {
                    foreach (SelectionPanelBehavior selectionPanelBehavior in selectionMenus)
                    {
                        selectionPanelBehavior.gameObject.SetActive(false);
                    }
                    return;
                }
                foreach (SelectionPanelBehavior selectionPanelBehavior in selectionMenus)
                {
                    selectionPanelBehavior.gameObject.SetActive(true);
                    // Update Billboard on selection menu
                    BillBoardBuilder billBoardBuilder = selectionPanelBehavior.GetComponentInChildren<BillBoardBuilder>(true);
                    await billBoardBuilder.drawBillBoard(spec.BrushedIndicies, 0, spec.SimulationId, new NeuronAttribute{value = spec.NeuronColorEncoding.value});

                    selectionPanelBehavior.UpdatedSelection(brainSubject.GetSpec().BrushedIndicies); 
                }
                
            }
        }

        public void ToggleTerrainView(TimedFlag isToggled)
        {
            terrain.SetActive(isToggled);
            terrain.transform.Find("Frame/Terrainview").GetComponent<TerrainViewBuilder>().InitPosition();
            brainSubject.InitTerrain();
            brainSubject.MakeTerrainLegend();
        }

        public void ToggleBrainView(TimedFlag isToggled)
        {
            brain.SetActive(isToggled);
        }
    }
}