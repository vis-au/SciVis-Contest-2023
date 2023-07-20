using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit.UX;
using UnityEngine;

public class TerrainSliderBehavior : MonoBehaviour, IBrainObserver
{
    private BrainSubject brainSubject;
    private GameObject timePlane;
    private Slider slider;
    // Start is called before the first frame update
    void Awake()
    {
        slider = GetComponent<Slider>();
        timePlane = GameObject.Find("Handle/TimePlane");
        brainSubject = GetComponentInParent<BrainSubject>();
        brainSubject.Attach(this);
        SetSliderValueVisualOnly(brainSubject.GetSpec().NeuronTimeStep);
        slider.OnClicked.AddListener(()=>SliderReleased());
        //slider.OnValueUpdated.AddListener((x)=>ValueUpdated());
    }

    public void SliderSelected()
    {
        timePlane.GetComponent<MeshRenderer>().material.SetColor("_Color",new Color32(255,255,255,200));
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    private async void SliderReleased()
    {
        timePlane.GetComponent<MeshRenderer>().material.SetColor("_Color",new Color32(255,255,255,29));

        if (brainSubject == null)
        {
            brainSubject = gameObject.GetComponentInParent<BrainSubject>();
        }
        Specification specification = brainSubject.GetSpec();
        int val = ((int) (Math.Round(slider.Value,4) * 1000000));
        specification.NeuronTimeStep = val;
        await brainSubject.semaphoreSlim.WaitAsync();
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
            
            StratumQuery queryCommunity1 = new StratumQuery(
                specification.SimulationId, 
                0, 
                specification.NeuronTimeStep, 
                new NeuronAttribute{ value = NeuronAttributeType.CommunityLevel1}, 
                new NeuronFilter(Guid.NewGuid(), new NeuronAttribute{ value = NeuronAttributeType.Calcium}, 0, 1));
            
            StratumQuery queryCommunity2 = new StratumQuery(
                specification.SimulationId, 
                0, 
                specification.NeuronTimeStep, 
                new NeuronAttribute{ value = NeuronAttributeType.CommunityLevel2}, 
                new NeuronFilter(Guid.NewGuid(), new NeuronAttribute{ value = NeuronAttributeType.Calcium}, 0, 1));

            StratumQuery queryCommunity3 = new StratumQuery(
                specification.SimulationId, 
                0, 
                specification.NeuronTimeStep, 
                new NeuronAttribute{ value = NeuronAttributeType.CommunityLevel3}, 
                new NeuronFilter(Guid.NewGuid(), new NeuronAttribute{ value = NeuronAttributeType.Calcium}, 0, 1));

            StratumQuery queryCommunity4 = new StratumQuery(
                specification.SimulationId, 
                0, 
                specification.NeuronTimeStep, 
                new NeuronAttribute{ value = NeuronAttributeType.CommunityLevel4}, 
                new NeuronFilter(Guid.NewGuid(), new NeuronAttribute{ value = NeuronAttributeType.Calcium}, 0, 1));
            
            await brainSubject.Stratum(queryColor);
            await brainSubject.Stratum(querySize);
            await brainSubject.Stratum(queryCommunity1);
            await brainSubject.Stratum(queryCommunity2);
            await brainSubject.Stratum(queryCommunity3);
            await brainSubject.Stratum(queryCommunity4);

            //await brainSubject.UpdateSynapsesStream(val);
            await (brainSubject as BrainSubject).UpdateSplinesStreamForSlider(val);
        }
        finally{
            brainSubject.semaphoreSlim.Release();
        }
           
    }
    
    public void SetSliderValueVisualOnly(int timeStamp)
    {
        slider.Value = timeStamp / 1000000f;
    }

    public void ObserverUpdateSynapses(IBrainSubject subject)
    {
        //Nothing
    }

    public void ObserverUpdateSplines(IBrainSubject subject)
    {
        //Nothing
    }

    public void ObserverUpdateNeurons(IBrainSubject subject)
    {
        SetSliderValueVisualOnly(subject.GetSpec().NeuronTimeStep);
    }

    public void ObserverUpdateConvexHull(IBrainSubject subject)
    {
        // :(
    }

    public void ObserverUpdateSelection(IBrainSubject brainSubject)
    {
        //Nothing
    }

    public void ObserverUpdateTerrain(IBrainSubject brainSubject)
    {
        //Nothing
    }
}
