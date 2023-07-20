using System.Collections.Generic;
public class SplineQuery{
    public int TimeStep { get; set; }
    public int Heirarchy { get; set; }
    public int ClusterId { get; set; }
    public SimulationType Simulation { get; set; }

    public SplineQuery(int timeStep, int heirarchy, int clusterId, SimulationType simulation){
        TimeStep = timeStep;
        Heirarchy = heirarchy;
        ClusterId = clusterId;
        Simulation = simulation;
    }
}