public class LeaveQuery{
    public int TimeStep { get; set;}
    public string Simulation { get; set; }
    public int Granularity { get; set; }
    public int ClusterId { get; set; }
    public LeaveQuery(int timeStep, string simulation, int granularity, int clusterId){
        TimeStep = timeStep;
        Simulation = simulation;
        Granularity = granularity;
        ClusterId = clusterId;
    }
}