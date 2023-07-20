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
    public class StartStopMenuBehaviour : MonoBehaviour, IBrainObserver
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
            SetStepButtonsActions();
            SetDeleteButtonActions();
            SetStartButtonAction();
            SetPauseButtonAction();
            SetToggleTerrainViewAction();
            
            // Slider
            brainSubject = gameObject.GetComponentInParent<IBrainSubject>();
        }

        private void Start()
        {
            SetValue(brainSubject.GetSpec().NeuronTimeStep);
            brainSubject.Attach(this);
            SetSliderActions();
            SetSimulation(brainSubject.GetSpec().SimulationId);
            sliderLabel = slider.transform.Find("Handle/Thumb/textValue").GetComponent<TextMeshProUGUI>();
        }

        private void SetStepButtonsActions()
        {
            buttons[0].OnClicked.AddListener(()=>
            {
                float newValue = slider.Value - 1 / 10000f;
                if (newValue < 0)
                {
                    return;
                }
                slider.Value = newValue;
                SliderReleased();
            });
            buttons[3].OnClicked.AddListener(()=>
            {
                StepForward();
            });
        }

        public void StepForward()
        {
            float newValue = slider.Value + 1 / 10000f;
            if (newValue > 1)
            {
                return;
            }
            slider.Value = newValue;
            SliderReleased();
        }

        private void SetStartButtonAction()
        {
            buttons[1].OnClicked.AddListener(() =>
            {
                if (!playing)
                {
                    playing = true;
                    playCoroutine = playArea.PlaySimulation();
                }
                
            });
        }

        private void SetPauseButtonAction()
        {
            buttons[2].OnClicked.AddListener(() =>
            {
                playing = false;
                StopCoroutine(playCoroutine);
            });
        }

        private void SetDeleteButtonActions()
        {
            deleteButtonAction += DeletePlayArea;
            buttons[4].OnClicked.AddListener(deleteButtonAction);
        }

        private void SetToggleTerrainViewAction()
        {
            buttons[5].OnClicked.AddListener(() =>
            {
                Debug.LogWarning("Toggle Terrain Clicked");
                toggleTerrainView(buttons[5].IsToggled);
            });
        }

        private void toggleTerrainView(TimedFlag isToggled)
        {
            playArea.ToggleTerrainView(isToggled);
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

                //await brainSubject.UpdateSynapsesStream(val);
                await (brainSubject as BrainSubject).UpdateSplinesStreamForSlider(val);
                

            }
            finally{
                (brainSubject as BrainSubject).semaphoreSlim.Release();
            }
           
        }

        public void SetValue(int timeStamp)
        {
            SetSliderValueVisualOnly(timeStamp);
            SliderReleased();
        }

        public void SetSliderValueVisualOnly(int timeStamp)
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
            TimeCapsuleUtil util = gameObject.GetComponentInChildren<TimeCapsuleUtil>(true);
            util.ChangeSimColor(specSimulationId);
        }

        public void ObserverUpdateSynapses(IBrainSubject subject)
        {
            //Not interesting
        }

        public void ObserverUpdateNeurons(IBrainSubject subject)
        {
            SetSliderValueVisualOnly(subject.GetSpec().NeuronTimeStep);
        }

        public void ObserverUpdateConvexHull(IBrainSubject subject)
        {
            // :)
        }

        public void ObserverUpdateSelection(IBrainSubject brainSubject)
        {
            //Not interesting
        }

        public void ObserverUpdateTerrain(IBrainSubject brainSubject)
        {
            // Nothing
        }

        public void ObserverUpdateSplines(IBrainSubject subject)
        {
            //Nothing
        }
    }
}
