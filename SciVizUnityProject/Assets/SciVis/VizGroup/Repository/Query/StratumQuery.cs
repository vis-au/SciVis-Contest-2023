public class StratumQuery{
    public StratumQuery(SimulationType simulation, int granularity, int timeStep, NeuronAttribute attribute, NeuronFilter filter){
        Simulation = simulation;
        Granularity = granularity;
        TimeStep = timeStep;
        Attribute = attribute;
        Filter = filter;
    }

    public SimulationType Simulation { get; set; }

    public int Granularity { get; set; }

    public int TimeStep { get; set; }

    public NeuronAttribute Attribute { get; set; }

    public NeuronFilter Filter { get; set; }
}