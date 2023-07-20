using System.Collections.Generic;
public class NeuronQuery
{
    public SimulationType Simulation { get; set; }
    public List<NeuronFilter> Filters { get; set; }
    public List<NeuronAttribute> Attributes { get; set; }
    public bool GiveAll { get; set; }

    public NeuronQuery(SimulationType simulation, List<NeuronFilter> filters, List<NeuronAttribute> attributes, bool giveAll)
    {
        Simulation = simulation;
        Filters = filters;
        Attributes = attributes;
        GiveAll = giveAll;
    }
}
