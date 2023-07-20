public class DeltaSynapseQuery
{
    public int FirstTimeStep { get; set; }
    public int SecondTimeStep { get; set; }
    public SimulationType Simulation { get; set; }

    public DeltaSynapseQuery(int firstTimeStep, int secondTimeStep, SimulationType simulation)
    {
        FirstTimeStep = firstTimeStep;
        SecondTimeStep = secondTimeStep;
        Simulation = simulation;
    }
}
