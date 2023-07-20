using System;
using Microsoft.MixedReality.Toolkit.UX;
using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Linq;

namespace SciVis.XRGroup.Scripts
{
    public class CustomDropdown : MonoBehaviour
    {
        private List<NeuronAttributeType> options;
        private List<AggregationType> aggregationOptions;
        private List<int> clusterLevelOptions;

        private PressableButton[] buttons;

        private DropDownClickHandler callback;

        public GameObject actionButtonPrefab;

        private Transform verticalLayoutGroup;
        // Start is called before the first frame update
        void Start()
        {
        }
        public static List<T> CreateList<T>(params T[] items)
        {
            return new List<T>(items);
        }
        public List<NeuronAttributeType> GetAllDropDownOptions()
        {
            return Enum.GetValues(typeof(NeuronAttributeType)).Cast<NeuronAttributeType>().ToList();

        }
    
        public List<NeuronAttributeType> GetShortDropDownOptions()
        {
            return new List<NeuronAttributeType> {
                NeuronAttributeType.SynapticInput,
                NeuronAttributeType.BackgroundActivity,
                NeuronAttributeType.GrownAxons,
                NeuronAttributeType.ConnectedAxons,
                NeuronAttributeType.GrownDendrites,
                NeuronAttributeType.Dampening,
                NeuronAttributeType.ConnectedDendrites,
                NeuronAttributeType.Fired,
                NeuronAttributeType.FiredFraction,
                NeuronAttributeType.ElectricActivity,
                NeuronAttributeType.TargetCalcium,
                NeuronAttributeType.Calcium,
                NeuronAttributeType.Synapses,
                NeuronAttributeType.None};

        }

        public void Initialize(DropDownClickHandler callBack, List<NeuronAttributeType> options)
        {
            this.options = GetShortDropDownOptions();
            this.callback = callBack;
            verticalLayoutGroup = gameObject.transform.Find("Canvas/Vertical");
            gameObject.SetActive(false);
            InstantiateOptions();
            buttons = GetComponentsInChildren<PressableButton>();
        }
        public void InitializeAggregation(DropDownClickHandler callBack, List<AggregationType> options)
        {
            this.aggregationOptions = options;
            this.callback = callBack;
            verticalLayoutGroup = gameObject.transform.Find("Canvas/Vertical");
            gameObject.SetActive(false);
            InstantiateAggregationOptions();
            buttons = GetComponentsInChildren<PressableButton>();
        }
        public void InitializeClusterLevel(DropDownClickHandler callBack, List<int> options)
        {
            this.clusterLevelOptions = options;
            this.callback = callBack;
            verticalLayoutGroup = gameObject.transform.Find("Canvas/Vertical");
            gameObject.SetActive(false);
            InstantiateClusterLevelOptions();
            buttons = GetComponentsInChildren<PressableButton>();
        }

        private void InstantiateOptions()
        {
        
            foreach (NeuronAttributeType option in options)
            {
                //Create Button
                GameObject button = Instantiate(actionButtonPrefab,verticalLayoutGroup);
                GameObject icon = button.transform.Find("Frontplate/AnimatedContent/Icon").gameObject;
                icon.SetActive(false);
                GameObject text = button.transform.Find("Frontplate/AnimatedContent/Text").gameObject;
                text.SetActive(true);
                text.GetComponent<TMPro.TextMeshProUGUI>().text= option.ToString();
                
                //Set Callback
                UnityAction action = () => callback.OnValueChosen(option);
                action += ToggleVisibility;
                button.GetComponent<PressableButton>().OnClicked.AddListener(action);
            }
        }
        private void InstantiateAggregationOptions()
        {
        
            foreach (AggregationType option in aggregationOptions)
            {
                //Create Button
                GameObject button = Instantiate(actionButtonPrefab,verticalLayoutGroup);
                GameObject icon = button.transform.Find("Frontplate/AnimatedContent/Icon").gameObject;
                icon.SetActive(false);
                GameObject text = button.transform.Find("Frontplate/AnimatedContent/Text").gameObject;
                text.SetActive(true);
                text.GetComponent<TMPro.TextMeshProUGUI>().text= option.ToString();
                
                //Set Callback
                UnityAction action = () => callback.OnValueChosen(option);
                action += ToggleVisibility;
                button.GetComponent<PressableButton>().OnClicked.AddListener(action);
            }
        }
        private void InstantiateClusterLevelOptions()
        {
        
            foreach (int option in clusterLevelOptions)
            {
                //Create Button
                GameObject button = Instantiate(actionButtonPrefab,verticalLayoutGroup);
                GameObject icon = button.transform.Find("Frontplate/AnimatedContent/Icon").gameObject;
                icon.SetActive(false);
                GameObject text = button.transform.Find("Frontplate/AnimatedContent/Text").gameObject;
                text.SetActive(true);
                text.GetComponent<TMPro.TextMeshProUGUI>().text= option.ToString();
                
                //Set Callback
                UnityAction action = () => callback.OnValueChosen(option);
                action += ToggleVisibility;
                button.GetComponent<PressableButton>().OnClicked.AddListener(action);
            }
        }


        public void ToggleVisibility()
        {
            gameObject.SetActive(!gameObject.activeSelf);
        }


            // Update is called once per frame
            void Update()
        {
            
        }
    }
}
