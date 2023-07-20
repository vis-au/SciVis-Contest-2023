using System.Threading.Tasks;
using System;
using System.Collections.Generic;

public interface IBrainSubject {
        // Attach an observer to the subject.
        void Attach(IBrainObserver observer);

        // Detach an observer from the subject.
        void Detach(IBrainObserver observer);

        // Notify all observers about an event.
        void NotifyNeurons();
        void NotifySynapses(); 
        void NotifySplines(); 
        void NotifyTerrain();   
        void NotifyConvexHull(); 
        Task UpdateSynapsesStream(int timestep);
        Task UpdateClusters(ClusterQuery clusterQuery);
        Task UpdateLeaves(LeaveQuery leaveQuery);
        void SetSimulation(SimulationType simulation_id);
        void SetNeuronSizeEncoding(NeuronAttribute attribute);
        void SetNeuronColorEncoding(NeuronAttribute attribute);
        void SetBrushedIndicies(HashSet<int> brushedIndicies);
        void AddNeuronFilter(NeuronFilter filter);
        void RemoveNeuronFilter(Guid Id);
        Task Stratum(StratumQuery query);
        void UpdateFilter(Guid Id, NeuronFilter filter);
        Specification GetSpec();
        void SetSpec(Specification spec);
        Task UpdateTerrain(PipQuery pipsQuery);
        void SetTerrainEncoding(NeuronAttribute attribute);
        void InitTerrain();
        void SetTerrainAggregationType(AggregationType chosenValue);
        void SetTerrainClusterLevel(int chosenValue);

        HashSet<int> neuronsInClusters(int clusterId, int granularity);

        Dictionary<int,float> clustersContainingSelection(int granularity);

}