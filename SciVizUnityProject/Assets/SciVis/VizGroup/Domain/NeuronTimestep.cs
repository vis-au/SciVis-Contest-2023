using System;
using System.Collections.Generic;
using UnityEngine;
[Serializable]
public class NeuronTimestep
{
    public int Id { get; set; }
    public int ZOrder { get; set; }
    public float Value { get; set; }
    public int TimeStep { get; set; }
    public SimulationType SimulationType { get; set; }
    public int Granularity { get; set; }
    public NeuronAttribute NeuronAttribute { get; set; }
    public NeuronTimestep(int Id , int Zorder, float Value, int TimeStep, SimulationType SimulationType, int Granularity, NeuronAttribute NeuronAttribute)
    {
        this.Id = Id;
        this.ZOrder = Zorder;
        this.Value = Value;
        this.TimeStep = TimeStep;
        this.SimulationType = SimulationType;
        this.Granularity = Granularity;
        this.NeuronAttribute = NeuronAttribute;
    }
}