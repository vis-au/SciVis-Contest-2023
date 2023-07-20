using System.Collections.Generic;

public enum NeuronAttributeType
    {
        SecondaryVariable, //TODO: Delete
        CommunityLevel1,
        CommunityLevel2,
        CommunityLevel3,
        CommunityLevel4,
        SynapticInput,
        BackgroundActivity,
        GrownAxons,
        ConnectedAxons,
        GrownDendrites,
        ConnectedDendrites,
        Dampening,
        Fired,
        FiredFraction,
        ElectricActivity,
        TargetCalcium,
        Calcium,
        Synapses,
        None
    }

public class NeuronAttribute{


    public NeuronAttributeType value { get; set; }

    private static Dictionary<NeuronAttributeType, (float, float)> AttributeToMinMax = new Dictionary<NeuronAttributeType, (float, float)>
    {
        {NeuronAttributeType.Calcium,(0,1.20681f)},
        {NeuronAttributeType.FiredFraction, (0,0.08f)},
        {NeuronAttributeType.ElectricActivity, (-75,30)},
        {NeuronAttributeType.Fired, (0,1)},
        {NeuronAttributeType.TargetCalcium, (0.6f,1.1f)},
        {NeuronAttributeType.SynapticInput, (0,19.5f)},
        {NeuronAttributeType.BackgroundActivity, (-1.88f,10.08f)},
        {NeuronAttributeType.GrownAxons, (0,50.5f)},
        {NeuronAttributeType.ConnectedAxons, (0,50)},
        {NeuronAttributeType.GrownDendrites, (2.81f,39.1f)},
        {NeuronAttributeType.ConnectedDendrites, (0,31)},
        {NeuronAttributeType.Dampening , (-13.13f,-6.2f)},
        {NeuronAttributeType.Synapses, (0f,70f)}, //TODO: set correct max
        {NeuronAttributeType.None, (0,0)}
    };
    
    private static Dictionary<NeuronAttributeType, (float, float)> ColorAttributeToMinMax = new Dictionary<NeuronAttributeType, (float, float)>
    {
        // {NeuronAttributeType.Calcium,(0,1.20681f)},
        {NeuronAttributeType.Calcium,(0.64f, 0.75f)},
        {NeuronAttributeType.FiredFraction, (0.032f,0.045f)},
        // {NeuronAttributeType.ElectricActivity, (-75,30)},
        {NeuronAttributeType.ElectricActivity, (-66, -54)},
        {NeuronAttributeType.Fired, (0,1)},
        {NeuronAttributeType.TargetCalcium, (0.6f,1.1f)},
        {NeuronAttributeType.SynapticInput, (0,19.5f)},
        {NeuronAttributeType.BackgroundActivity, (-1.88f,10.08f)},
        // {NeuronAttributeType.GrownAxons, (0,50.5f)},
        {NeuronAttributeType.GrownAxons, (14f, 17f)},
        {NeuronAttributeType.ConnectedAxons, (0,50)},
        // {NeuronAttributeType.GrownDendrites, (2.81f,39.1f)},
        {NeuronAttributeType.GrownDendrites, (9f, 15f)},
        {NeuronAttributeType.ConnectedDendrites, (0,31)},
        {NeuronAttributeType.Dampening , (-13.13f,-6.2f)},
        {NeuronAttributeType.Synapses, (0f,70f)}, //TODO: set correct max
        {NeuronAttributeType.None, (0,0)}
    };
    
    public (float,float) GetMinMax(){
        return AttributeToMinMax[value];
    }

    public (float, float) GetColorMinMax()
    {
        return ColorAttributeToMinMax[value];
    }
}


