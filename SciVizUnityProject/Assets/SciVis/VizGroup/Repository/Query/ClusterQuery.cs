using System.Collections.Generic;
public class ClusterQuery{
    public int TimeStep { get; set; }
    public int Granularity { get; set; }
    public string Simulation { get; set; }
    public List<string> Projections { get; set; }
    public ClusterQuery(int timeStep, int granularity, string simulation, List<string> projections){
        TimeStep = timeStep;
        Granularity = granularity;
        Simulation = simulation;
        Projections = projections;
    }
}