using System.Collections.Generic;
using UnityEngine;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Linq;

public class TerrainSubject : MonoBehaviour, ITerrainSubject{
    public List<List<NeuronTimestep>> _points;
    public SimulationType simulation { get; set; }
    public int granularity { get; set; }

    public int pips { get; set; }
 
    public NeuronAttribute _field = new NeuronAttribute();
    private List<ITerrainObserver> _observers = new List<ITerrainObserver>();

    private IRepository _repository;
    private HashSet<int> selectedIDs;

    private void Awake(){
        _repository = Repository.Instance;
        selectedIDs = new HashSet<int>();
        //transform.parent.GetComponentInChildren<IBrainSubject>().Attach(this);
    }

    private async void Start()
    {
        simulation = SimulationType.calcium;
        granularity = 2;
        pips = 100;
        _field.value = NeuronAttributeType.Calcium;
        AggregationType aggregation = AggregationType.Average;
        PipQuery pipsQuery = new PipQuery(pips, granularity, simulation, _field, aggregation);
        await UpdateTerrain(pipsQuery);
    }

    public async Task UpdateTerrain(PipQuery pipsQuery){
        _points = new List<List<NeuronTimestep>>();
        
        using (BlockingCollection<NeuronTimestep> bc = new BlockingCollection<NeuronTimestep>()){
            // Kick off a producer task
            var producerTask = Task.Run(async () =>
            {
                await _repository.GetPipsStream(pipsQuery, bc);
            });
        
            // Kick off a consumer task
            var consumerTask = Task.Run(() =>
            {
                foreach (NeuronTimestep item in bc.GetConsumingEnumerable())
                    {
                        if (item.ZOrder >= _points.Count){
                           List<NeuronTimestep> new_points = new List<NeuronTimestep>();
                           _points.Add(new_points);
                           _points[item.ZOrder].Add(item);
                        } else {
                            _points[item.ZOrder].Add(item);
                        }
                    }   
            });
            await Task.WhenAll(producerTask, consumerTask);

            this.Notify();
        }
    }


    public List<int> ZOrder_To_ClusterIds(List<int> z_orders){

        List<int> current = new List<int>();

        for (int i = 1; i < z_orders.Count; i++){
            NeuronTimestep neuron = _points[i][0];
            if (neuron != null){
                current.Add(neuron.Id);
            }
        }
        return current; 
    }



    // Notify all observers about an event.
    public void Notify(){
        Debug.Log("Subject: Notifying Terrain observers...");

        foreach (var observer in _observers)
        {
            observer.ObserverUpdate(this);
        }
    }

    // Attach an observer to the subject.
    public void Attach(ITerrainObserver observer){
        Debug.Log("Subject: Attached a Terrain observer.");
        this._observers.Add(observer);
    }

    // Detach an observer from the subject.
    public void Detach(ITerrainObserver observer){
        this._observers.Remove(observer);
        Debug.Log("Subject: Detached a Terrain observer.");
    }

    public void ObserverUpdateSynapses(IBrainSubject subject)
    {
        //Do nothing
    }

    public void ObserverUpdateNeurons(IBrainSubject subject)
    {
        //DO nothing
    }
    public void ObserverUpdateTerrain(IBrainSubject subject)
    {
        //DO nothing
    }

    public void ObserverUpdateSelection(IBrainSubject brainSubject)
    {
        this.selectedIDs = new HashSet<int>(brainSubject.GetSpec().BrushedIndicies);
        Notify();
    }

    public HashSet<int> GetSelectedIDs()
    {
        return selectedIDs;
    }
}