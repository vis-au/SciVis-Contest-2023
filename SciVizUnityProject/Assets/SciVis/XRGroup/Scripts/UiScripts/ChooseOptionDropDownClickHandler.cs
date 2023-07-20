using Microsoft.MixedReality.Toolkit.UX;
using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System;
using System.Linq;

namespace SciVis.XRGroup.Scripts
{
    public class ChooseOptionDropDownClickHandler : DropDownClickHandler
    {
        [SerializeField]
        private string encoding;
        

        public override void Start()
        {
            if (encoding.Equals("aggrType")){
                List<AggregationType> aggrTypes = Enum.GetValues(typeof(AggregationType)).Cast<AggregationType>().ToList();
                button = gameObject.GetComponent<PressableButton>();
                dropdownInstance = Instantiate(dropdown, gameObject.transform.position,gameObject.transform.rotation,gameObject.transform);
                dropdownInstance.transform.Translate(- button.transform.forward*0.01f, Space.World);
                dropdownInstance.InitializeAggregation(this, aggrTypes);
                toggleAction += dropdownInstance.ToggleVisibility;
                button.OnClicked.AddListener(toggleAction);

            }
            else if (encoding.Equals("clusterLevel")){
                List<int> clusterLevels = new List<int>(new int[] { 2, 3, 4 } );
                button = gameObject.GetComponent<PressableButton>();
                dropdownInstance = Instantiate(dropdown, gameObject.transform.position,gameObject.transform.rotation,gameObject.transform);
                dropdownInstance.transform.Translate(- button.transform.forward*0.01f, Space.World);
                dropdownInstance.InitializeClusterLevel(this, clusterLevels);
                toggleAction += dropdownInstance.ToggleVisibility;
                button.OnClicked.AddListener(toggleAction);
            }
            else {
                dropdownOptions = Enum.GetValues(typeof(NeuronAttributeType)).Cast<NeuronAttributeType>().ToList();
                button = gameObject.GetComponent<PressableButton>();
                dropdownInstance = Instantiate(dropdown, gameObject.transform.position,gameObject.transform.rotation,gameObject.transform);
                dropdownInstance.transform.Translate(- button.transform.forward*0.01f, Space.World);
                dropdownInstance.Initialize(this, dropdownOptions);
                toggleAction += dropdownInstance.ToggleVisibility;
                button.OnClicked.AddListener(toggleAction);
            }
            
        }
        public override void OnValueChosen(NeuronAttributeType value)
        {
            chosenValue = new NeuronAttribute{value = value};
            IBrainSubject bs = GetComponentInParent<IBrainSubject>();
            GetComponentInChildren<TMPro.TextMeshProUGUI>().SetText(chosenValue.value.ToString());
            if (encoding.Equals("color"))
            {
                bs.SetNeuronColorEncoding(chosenValue);
                transform.parent.parent.Find("checkBoxHorizontalDivergentColor").gameObject.SetActive(chosenValue.value.ToString().Equals("Calcium"));
            }
            else if (encoding.Equals("colorPos"))
            {
                //TerrainView encoding
                bs.SetTerrainEncoding(chosenValue);
            }
            
            else if (encoding.Equals("position"))
            {
                //TODO VisGuys handle positionEncoding
            }
            else
            {
                bs.SetNeuronSizeEncoding(chosenValue);
            }
            
        }
        public override void OnValueChosen(AggregationType value)
        {
            IBrainSubject bs = GetComponentInParent<IBrainSubject>();
            GetComponentInChildren<TMPro.TextMeshProUGUI>().SetText(value.ToString());
            if (encoding.Equals("aggrType")){
                // Handle terrain aggregation type
                bs.SetTerrainAggregationType(value);

            }
        }
        public override void OnValueChosen(int value)
        {
            IBrainSubject bs = GetComponentInParent<IBrainSubject>();
            GetComponentInChildren<TMPro.TextMeshProUGUI>().SetText(value.ToString());
            if (encoding.Equals("clusterLevel")){
                bs.SetTerrainClusterLevel(value);
            }
        }
    }
}
