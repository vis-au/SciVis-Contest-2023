using System;
using System.Collections.Generic;
public class BillBoardQuery
{
    public int PipsNumber { get; set; }
    public int Granularity { get; set; }
    public SimulationType Simulation { get; set; }
    public NeuronAttribute NeuronAttribute { get; set; }
    public string Aggregation { get; set; }
    public List<int> Cluster_ids {get; set;}

    public BillBoardQuery(int pipsNumber, int granularity, SimulationType simulation, NeuronAttribute neuronAttribute, string aggregation, List<int> cluster_ids)
    {
        PipsNumber = pipsNumber;
        Granularity = granularity;
        Simulation = simulation;
        NeuronAttribute = neuronAttribute;
        Aggregation = aggregation;
        Cluster_ids = cluster_ids;
    }
}