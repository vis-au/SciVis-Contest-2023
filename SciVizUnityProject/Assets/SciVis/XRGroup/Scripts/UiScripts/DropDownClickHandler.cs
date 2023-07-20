using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.MixedReality.GraphicsTools;
using Microsoft.MixedReality.Toolkit.UX;
using SciVis.XRGroup.Scripts;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public abstract class DropDownClickHandler : MonoBehaviour
{
    protected PressableButton button;

    protected UnityAction toggleAction;

    [SerializeField]
    protected CustomDropdown dropdown;

    protected CustomDropdown dropdownInstance;

    protected NeuronAttribute chosenValue;

    protected List<NeuronAttributeType> dropdownOptions;
    
    // Start is called before the first frame update

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
    public virtual void Start()
    {
        dropdownOptions = GetShortDropDownOptions();
        button = gameObject.GetComponent<PressableButton>();
        dropdownInstance = Instantiate(dropdown, gameObject.transform.position,gameObject.transform.rotation,gameObject.transform);
        dropdownInstance.transform.Translate(- button.transform.forward*0.01f, Space.World);
        dropdownInstance.Initialize(this, dropdownOptions);
        toggleAction += dropdownInstance.ToggleVisibility;
        button.OnClicked.AddListener(toggleAction);
    }
    
    public abstract void OnValueChosen(NeuronAttributeType value);
    public abstract void OnValueChosen(AggregationType value);
    public abstract void OnValueChosen(int value);
}
