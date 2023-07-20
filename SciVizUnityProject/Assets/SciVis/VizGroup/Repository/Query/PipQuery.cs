using System;

public class PipQuery
{
    public int PipsNumber { get; set; }
    public int Granularity { get; set; }
    public SimulationType Simulation { get; set; }
    public NeuronAttribute NeuronAttribute { get; set; }
    public AggregationType Aggregation { get; set;}
    public PipQuery(int pipsNumber, int granularity, SimulationType simulation, NeuronAttribute neuronAttribute, AggregationType aggregation)
    {
        PipsNumber = pipsNumber;
        Granularity = granularity;
        Simulation = simulation;
        NeuronAttribute = neuronAttribute;
        Aggregation = aggregation;
    }
}