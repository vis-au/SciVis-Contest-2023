using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.UX;
using SciVis.XRGroup.Scripts.TwoKnobSlider;
using TMPro;
using UnityEngine;

public class FilterBehaviour : MonoBehaviour
{
    private NeuronFilter filter;
    private PressableButton checkBox;
    private PressableButton deleteButton;
    private TwoKnobSliderHandler slider;
    private TextMeshProUGUI header;
    private IBrainSubject brainSubject;
    void Start()
    {
        init();
        checkBox.OnClicked.AddListener(()=>checkBoxClicked());
        deleteButton.OnClicked.AddListener(()=>deleteButtonClicked());
        slider.AddSlidingFinishedListener((left,right)=>updateFilterValues(left,right));
        brainSubject = GetComponentInParent<IBrainSubject>();
    }

    private void deleteButtonClicked()
    {
        GetComponentInParent<FilterMenuBehaviour>().DeleteFilter(this);
    }

    private void init()
    {
        PressableButton[] buttons = transform.GetComponentsInChildren<PressableButton>(true);
        checkBox = buttons[0];
        deleteButton = buttons[1];
        slider = transform.GetComponentInChildren<TwoKnobSliderHandler>();
        header = transform.Find("Header1").GetComponentInChildren<TextMeshProUGUI>();
    }

    private void updateFilterValues(float left, float right)
    {
        filter.Min = left;
        filter.Max = right;
        
        brainSubject.NotifyNeurons();
        brainSubject.NotifySynapses();
    }

    private void checkBoxClicked()
    {
        filter.Checked = checkBox.IsToggled;
        brainSubject.NotifyNeurons();
        brainSubject.NotifySynapses();
    }

    public void SetFilter(NeuronFilter neuronFilter)
    {
        if (slider == null)
        {
            init();
        }
        filter = neuronFilter;
        
        checkBox.ForceSetToggled(filter.Checked);
        (float min, float max) = filter.Attribute.GetMinMax();
        slider.setMax(max);
        slider.setMin(min);
        slider.setValues(filter.Min, filter.Max);
        header.SetText(filter.Attribute.value.ToString());
    }

    public NeuronFilter GetFilter()
    {
        return filter;
    }

    public void SetDeleteMode(bool deletionModeActive)
    {
        checkBox.gameObject.SetActive(!deletionModeActive);
        deleteButton.gameObject.SetActive(deletionModeActive);
    }
}
