using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.UX;
using UnityEngine;

public class FilterMenuBehaviour : MonoBehaviour
{
    [SerializeField]
    private Transform container;

    [SerializeField]
    private GameObject filterPrefab;

    private PressableButton toggleModeButton;
    
    private IBrainSubject brainSubject;

    private void Awake()
    {
        brainSubject = GetComponentInParent<IBrainSubject>();
        toggleModeButton = transform.Find("Canvas/HeaderHorizontal/ToggleModeButton").GetComponent<PressableButton>();
        toggleModeButton.OnClicked.AddListener(()=>toggleDeletionMode());
    }

    private void toggleDeletionMode()
    {
        bool deletionModeActive = toggleModeButton.IsToggled;
        foreach (FilterBehaviour filter in GetComponentsInChildren<FilterBehaviour>())
        {
            filter.SetDeleteMode(deletionModeActive);
        }
    }

    public void ClearAllUIFilters()
    {
        FilterBehaviour[] filters = GetComponentsInChildren<FilterBehaviour>();
        foreach(FilterBehaviour filter in filters)
        {
            DeleteFilterUI(filter);
        }
    }

    public void DeleteFilterUI(FilterBehaviour filter)
    {
        GameObject.Destroy(filter.gameObject);
    }

    public void AddFilterUI(NeuronFilter filterToAdd)
    {
        Debug.LogWarning(filterToAdd.Attribute.ToString());
        Debug.LogWarning(filterToAdd.Max);
        Debug.LogWarning(filterToAdd.Min);
        if (toggleModeButton == null)
        {
            brainSubject = GetComponentInParent<IBrainSubject>();
            toggleModeButton = transform.Find("Canvas/HeaderHorizontal/ToggleModeButton").GetComponent<PressableButton>();
        }
        GameObject instance = Instantiate(filterPrefab, container);
        FilterBehaviour filterBehaviour = instance.GetComponent<FilterBehaviour>();
        filterBehaviour.SetFilter(filterToAdd);
        filterBehaviour.SetDeleteMode(toggleModeButton.IsToggled);
        
    }
    public void AddFilter(NeuronFilter filterToAdd)
    {
        AddFilterUI(filterToAdd);
        brainSubject.AddNeuronFilter(filterToAdd);
    }

    public void DeleteFilter(FilterBehaviour filterBehaviour)
    {
        brainSubject.RemoveNeuronFilter(filterBehaviour.GetFilter().Id);
        DeleteFilterUI(filterBehaviour);
    }
}
