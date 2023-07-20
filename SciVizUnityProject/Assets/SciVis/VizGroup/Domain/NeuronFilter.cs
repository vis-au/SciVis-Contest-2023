using System;
public class NeuronFilter {
    public NeuronFilter(NeuronAttribute attribute, float min, float max){
        Id = Guid.NewGuid();
        Checked = true;
        Min = min;
        Max = max;
        Attribute = attribute; 
    }
    public NeuronFilter(Guid id,NeuronAttribute attribute, float min, float max){
        Id = id;
        Checked = true;
        Min = min;
        Max = max;
        Attribute = attribute; 
    }
    /**
     * A constructor for copying a NeuronFilter Object.
     */
    public NeuronFilter(NeuronFilter neuronFilter)
    {
        Id = neuronFilter.Id;
        Checked = neuronFilter.Checked;
        Min = neuronFilter.Min;
        Max = neuronFilter.Max;
        Attribute = neuronFilter.Attribute; 
    }
    public Guid Id { get; set; }
    public bool Checked { get; set; }

    public NeuronAttribute Attribute { get; set; }

    public float Min { get; set; }

    public float Max { get; set; }
}
