public class SynapseQuery
{
    public int TimeStep { get; set; }
    public SimulationType Simulation { get; set; }

    public SynapseQuery(int timeStep, SimulationType simulation)
    {
        TimeStep = timeStep;
        Simulation = simulation;
    }
}
