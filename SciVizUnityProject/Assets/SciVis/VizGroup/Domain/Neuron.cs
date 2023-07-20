using System;
using System.Collections.Generic;

[Serializable]
public class Neuron{
    public int Id { get; set; }
    public Boolean Fired { get; set; }
    public float FiredFraction { get; set; }
    public float X { get; set; }
    public float SecondaryVariable { get; set; }
    public float Calcium { get; set; }
    public float TargetCalcium { get; set; }
    public float SynapticInput { get; set; }
    public float BackgroundActivity { get; set; }
    public float GrownAxons { get; set; }
    public int ConnectedAxons { get; set; }
    public float GrownExcitatoryDendrites { get; set; }
    public int ConnectedExcitatoryDendrites { get; set; }
    public float Dampening { get; set; }
    public int CommunityLevel1 { get; set; }
    public int CommunityLevel2 { get; set; }
    public int CommunityLevel3 { get; set; }
    public int CommunityLevel4 { get; set; }

    public float GetValueOf(NeuronAttribute attribute){
        switch (attribute.value)
            {
            case NeuronAttributeType.Fired:
                if (Fired){
                    return 1.0f; 
                }else {
                    return 0.0f; 
                }
            case NeuronAttributeType.FiredFraction:
                return FiredFraction;
            case NeuronAttributeType.ElectricActivity:
                return X;
            case NeuronAttributeType.SecondaryVariable:
                return SecondaryVariable;
            case NeuronAttributeType.Calcium:
                return Calcium;
            case NeuronAttributeType.TargetCalcium:
                return TargetCalcium;
            case NeuronAttributeType.SynapticInput:
                return SynapticInput;
            case NeuronAttributeType.BackgroundActivity:
                return BackgroundActivity;
            case NeuronAttributeType.GrownAxons:
                return GrownAxons;
            case NeuronAttributeType.ConnectedAxons:
                return (float)ConnectedAxons;
            case NeuronAttributeType.GrownDendrites:
                return GrownExcitatoryDendrites;
            case NeuronAttributeType.ConnectedDendrites:
                return (float)ConnectedExcitatoryDendrites;
            case NeuronAttributeType.Dampening:
                return Dampening;
            case NeuronAttributeType.None:
                return 0f;
            default:
                return 0.0f;
            }
    }

}