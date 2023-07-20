using System.Collections.Generic;

using UnityEngine;

public enum SimulationType{
    no_network,
    disable,
    stimulus,
    calcium
}
public class Specification
{
    public SimulationType SimulationId { get; set; }
    public int NeuronTimeStep { get; set; }
    public int SynapseTimeStep { get; set; }
    public NeuronAttribute NeuronSizeEncoding { get; set; }
    public NeuronAttribute NeuronColorEncoding { get; set; }
    public NeuronAttribute TerrainEncoding { get; set; }
    public HashSet<int> BrushedIndicies { get; set; }
    public List<NeuronFilter> Filters { get; set; }
    public AggregationType AggregationType {get; set;}
    public int TerrainClusterLevel {get;set;}
    public int TerrainPips {get;set;}
    public int BrainClusterLevel {get;set;}
    public Quaternion BrainRotation {get;set;}
    public Vector3 BrainScale {get;set;}
    public List<HashSet<int>> ExplodedClusters { get; set; }
    public bool LocalColorScale { get; set; }
    // We could consider adding filters for synapses, if some interesting ways of filtering come to mind.

    public bool DivergentColorScale { get; set;}
    
    public Specification(){
        SimulationId = SimulationType.stimulus;
        NeuronTimeStep = 420000;
        SynapseTimeStep = 420000;
        NeuronColorEncoding = new NeuronAttribute{ value = NeuronAttributeType.Calcium}; // Default value
        TerrainEncoding = new NeuronAttribute{ value = NeuronAttributeType.Calcium}; // Default value
        NeuronSizeEncoding = new NeuronAttribute{ value = NeuronAttributeType.FiredFraction}; //Default value
        Filters = new List<NeuronFilter>();
        BrushedIndicies = new HashSet<int>();
        AggregationType = AggregationType.Average;
        TerrainClusterLevel = 4;
        TerrainPips = 100;
        BrainClusterLevel = 5;
        LocalColorScale = false;
        BrainRotation = Quaternion.Euler(-90, 180, 0);
        BrainScale = new Vector3(0.01f,0.01f,0.01f);
        ExplodedClusters = new List<HashSet<int>> {new HashSet<int>(), new HashSet<int>(),new HashSet<int>(),new HashSet<int>(),new HashSet<int>()};
        DivergentColorScale = false;
    }

    /**
     * A Constructor for copying a specification object
     */
    public Specification(Specification specification)
    {
        SimulationId = specification.SimulationId;
        NeuronTimeStep = specification.NeuronTimeStep;
        SynapseTimeStep = specification.SynapseTimeStep;
        NeuronColorEncoding = specification.NeuronColorEncoding;
        NeuronSizeEncoding = specification.NeuronSizeEncoding;
        TerrainEncoding = specification.TerrainEncoding;
        Filters = new List<NeuronFilter>();
        BrushedIndicies = specification.BrushedIndicies;
        foreach (var filter in specification.Filters)
        {
            Filters.Add(new NeuronFilter(filter));
        }
        AggregationType = specification.AggregationType;
        TerrainClusterLevel = specification.TerrainClusterLevel;
        BrainClusterLevel = specification.BrainClusterLevel;
        TerrainPips = specification.TerrainPips;
        LocalColorScale = specification.LocalColorScale;
        ExplodedClusters = new List<HashSet<int>>();
        foreach (HashSet<int> gran in specification.ExplodedClusters)
        {
            ExplodedClusters.Add(new HashSet<int>(gran));
        }
        BrainRotation = new Quaternion(specification.BrainRotation.x, specification.BrainRotation.y, specification.BrainRotation.z, specification.BrainRotation.w);
        BrainScale = new Vector3(specification.BrainScale.x, specification.BrainScale.y, specification.BrainScale.z);
        DivergentColorScale = specification.DivergentColorScale;
    }
}