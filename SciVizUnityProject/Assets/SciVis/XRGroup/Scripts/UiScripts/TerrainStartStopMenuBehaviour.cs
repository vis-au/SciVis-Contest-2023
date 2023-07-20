using System;
using Assets.SciVis.XRGroup;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.UX;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.UI;
using Slider = Microsoft.MixedReality.Toolkit.UX.Slider;
using System.Collections.Generic;
using System.Threading;
using Microsoft.MixedReality.Toolkit;

namespace SciVis.XRGroup.Scripts
{
    public class TerrainStartStopMenuBehaviour : MonoBehaviour, IBrainObserver
    {
        private GameObject menuInstance;
        private UnityAction saveButtonAction;
        private UnityAction deleteButtonAction;

        private GameObject cubePrefab;

        private PressableButton[] buttons;
        private RawImage headerBackground;
        private TextMeshProUGUI header;

        public bool playing;
        private Coroutine playCoroutine;

        private PlayAreaUtil playArea;
        
        // Slider
        private IBrainSubject brainSubject;
        private Slider slider;
        private TextMeshProUGUI sliderLabel;
        private int cubeIDcounter = 4;

        // TerrainView
        private ITerrainSubject terrainSubject;

        // Start is called before the first frame update
        void Awake()
        {
            playing = false;
            playArea = gameObject.GetComponentInParent<PlayAreaUtil>();
            cubeIDcounter = gameObject.GetComponentInParent<BrainSelectionManager>().GetCubeCounter();
            menuInstance = gameObject;
            cubePrefab = Resources.Load<GameObject>("Prefabs/Models/BrainTimeCaptureCube");
            headerBackground = transform.Find("Outer/SimulationInfo/Backplate").GetComponent<RawImage>();
            header = transform.GetComponentInChildren<TextMeshProUGUI>();
            buttons = menuInstance.GetComponentsInChildren<PressableButton>();
            slider = menuInstance.GetComponentInChildren<Slider>();
            SetDeleteButtonActions();
            SetToggleBrainViewAction();
            
            brainSubject = gameObject.GetComponentInParent<IBrainSubject>();
            brainSubject.Attach(this);
        }

        private void Start()
        {
            SetValue(brainSubject.GetSpec().NeuronTimeStep);
            SetSliderActions();
            SetSimulation(brainSubject.GetSpec().SimulationId);
            sliderLabel = slider.transform.Find("Handle/Thumb/textValue").GetComponent<TextMeshProUGUI>();
        }

        private void SetDeleteButtonActions()
        {
            deleteButtonAction += DeletePlayArea;
            buttons[0].OnClicked.AddListener(deleteButtonAction);
        }

        private void SetToggleBrainViewAction()
        {
            buttons[1].OnClicked.AddListener(() =>
            {
                Debug.LogWarning("Toggle Terrain Clicked");
                toggleBrainView(buttons[1].IsToggled);
            });
            buttons[1].ForceSetToggled(true);
        }

        private void toggleBrainView(TimedFlag isToggled)
        {
            playArea.ToggleBrainView(isToggled);
        }

        private void DeletePlayArea()
        {
            //GameObject playArea = this.playArea.gameObject;
            this.playArea.Deactivate();
            /*playArea.SetActive(false);
            Destroy(playArea,1);*/
        }

        private void SetSliderActions()
        {
            UnityAction action = () => SliderReleased();
            UnityAction<SliderEventData> updateLabelAction = (SliderEventData data) => SliderUpdated(data);
            slider.OnValueUpdated.AddListener(updateLabelAction);
            slider.OnClicked.AddListener(action);
        }

        private void SliderUpdated(SliderEventData data)
        {
            int val = ((int) (Math.Round(data.NewValue,4) * 1000000));
            sliderLabel.SetText(val.ToString());
        }


        private async void SliderReleased()
        {
            if (brainSubject == null)
            {
                brainSubject = gameObject.GetComponentInParent<IBrainSubject>();
            }
            Specification specification = brainSubject.GetSpec();
            int val = ((int) (Math.Round(slider.Value,4) * 1000000));
            specification.NeuronTimeStep = val;
            await (brainSubject as BrainSubject).semaphoreSlim.WaitAsync();
            try{
                StratumQuery queryColor = new StratumQuery(
                    specification.SimulationId, 
                    0, 
                    specification.NeuronTimeStep, 
                    new NeuronAttribute{ value = specification.NeuronColorEncoding.value}, 
                    new NeuronFilter(Guid.NewGuid(), new NeuronAttribute{ value = NeuronAttributeType.Calcium}, 0, 1));

                StratumQuery querySize = new StratumQuery(
                    specification.SimulationId, 
                    0, 
                    specification.NeuronTimeStep, 
                    new NeuronAttribute{ value = specification.NeuronSizeEncoding.value}, 
                    new NeuronFilter(Guid.NewGuid(), new NeuronAttribute{ value = NeuronAttributeType.Calcium}, 0, 1));
                
                await brainSubject.Stratum(queryColor);
                await brainSubject.Stratum(querySize);

                // await brainSubject.UpdateSynapsesStream(val);
                await (brainSubject as BrainSubject).UpdateSplinesStreamForSlider(val);
            }
            finally{
                (brainSubject as BrainSubject).semaphoreSlim.Release();
            }
           
        }

        public void SetValue(int timeStamp)
        {
            SetSliderValueVisualsOnly(timeStamp);
            SliderReleased();
        }

        private void SetSliderValueVisualsOnly(int timeStamp)
        {
            if (slider == null || sliderLabel == null)
            {
                menuInstance = gameObject;
                
                slider = menuInstance.GetComponentInChildren<Slider>();
                sliderLabel = slider.transform.Find("Handle/Thumb/textValue").GetComponent<TextMeshProUGUI>();
            }
            slider.Value = timeStamp / 1000000f;
            sliderLabel.SetText(timeStamp.ToString());
        }

        public Slider GetSlider()
        {
            return slider;
        }

        public void SetSimulation(SimulationType specSimulationId)
        {
            if (headerBackground == null ||header == null)
            {
                headerBackground = transform.Find("Outer/SimulationInfo/Backplate").GetComponent<RawImage>();
                header = transform.GetComponentInChildren<TextMeshProUGUI>();
            }
            headerBackground.color = TimeCapsuleUtil.colors[specSimulationId];
            header.SetText("Simulation " + specSimulationId);
            TimeCapsuleUtil util = gameObject.GetComponentInChildren<TimeCapsuleUtil>();
            util.ChangeSimColor(specSimulationId);
        }

        public void ObserverUpdateSynapses(IBrainSubject subject)
        {
        }

        public void ObserverUpdateSplines(IBrainSubject subject)
        {
            //Nothing
        }        

        public void ObserverUpdateNeurons(IBrainSubject subject)
        {
            SetSliderValueVisualsOnly(subject.GetSpec().NeuronTimeStep);
        }

        public void ObserverUpdateConvexHull(IBrainSubject subject)
        {
        }

        public void ObserverUpdateSelection(IBrainSubject brainSubject)
        {
        }

        public void ObserverUpdateTerrain(IBrainSubject brainSubject)
        {
        }
    }
}
