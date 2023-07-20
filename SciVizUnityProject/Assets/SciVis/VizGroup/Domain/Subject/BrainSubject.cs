using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

//importing this to easily print the properties of an object, without prior knowledge of the property names


public class BrainSubject : MonoBehaviour, IBrainSubject
{
    private static BrainSubject instance = null;
    public List<Synapse> _synapses = new();
    public List<Spline> _splines = new();
    public List<Neuron> _neurons = new();
    public List<Neuron> _neuronsCommunities = new();
    public List<Neuron> _neuronsForClusters = new();

    private readonly List<IBrainObserver> _observers = new();

    private readonly bool testing = false;

    public NeuronAttribute _field = new();

    // Terrain stuff
    public List<List<NeuronTimestep>> _points;
    private IRepository _repository;
    public Dictionary<string, clusterSplinesData> clusterHeirarchyDict = new();


    //Instantiate a Singleton of the Semaphore with a value of 1. This means that only 1 thread can be granted access at a time.
    public SemaphoreSlim semaphoreSlim = new(1, 1);
    public Specification spec = new();

    private void Awake()
    {
        if (!testing) _repository = Repository.Instance;
        spec = new Specification();
    }


    public void InitTerrain()
    {
        DoPipQuery(spec.TerrainPips, spec.TerrainClusterLevel, spec.SimulationId,
            spec.TerrainEncoding, spec.AggregationType);
    }

    public HashSet<int> neuronsInClusters(int clusterId, int granularity)
    {
        var result = new HashSet<int>();
        result = _neuronsForClusters.Where(x =>
        {
            var clusterID = x.Id;
            if (granularity == 1) clusterID = x.CommunityLevel1;

            if (granularity == 2) clusterID = x.CommunityLevel2;

            if (granularity == 3) clusterID = x.CommunityLevel3;

            if (granularity == 4) clusterID = x.CommunityLevel4;

            return clusterId == clusterID;
        }).Select(x => x.Id).ToHashSet();
        return result;
    }

    /**
     * Returns a dictionary from cluster ID to percentage of cluster that is selected
     * The dictionary only contains clusters that actually have something selected
     */
    public Dictionary<int, float> clustersContainingSelection(int granularity)
    {
        var clusterTotals = new Dictionary<int, float>();
        var res = new Dictionary<int, float>();
        foreach (var neuron in _neuronsForClusters)
        {
            var x = neuron;
            var clusterID = x.Id;
            if (granularity == 1) clusterID = x.CommunityLevel1;

            if (granularity == 2) clusterID = x.CommunityLevel2;

            if (granularity == 3) clusterID = x.CommunityLevel3;

            if (granularity == 4) clusterID = x.CommunityLevel4;

            if (clusterTotals.ContainsKey(clusterID))
                clusterTotals[clusterID] += 1;
            else
                clusterTotals[clusterID] = 1;

            if (!spec.BrushedIndicies.Contains(x.Id)) continue;

            if (res.ContainsKey(clusterID))
                res[clusterID] += 1;
            else
                res[clusterID] = 1;
        }

        foreach (var key in res.Keys.ToList()) res[key] /= clusterTotals[key];
        return res;
    }

    public async Task Stratum(StratumQuery query)
    {
        using (var bc = new BlockingCollection<Node>())
        {
            // Kick off a producer task
            var producerTask = Task.Run(async () => { await _repository.Stratum(query, bc); });

            // Kick off a consumer task
            var consumerTask = Task.Run(() =>
            {
                foreach (var item in bc.GetConsumingEnumerable())
                    UpdateNeuronAttribute(item, query.Attribute);
            });
            var updateNeuronCoroutine = UpdateNeuronView();
            StartCoroutine(updateNeuronCoroutine);

            await Task.WhenAll(producerTask, consumerTask);
            StopCoroutine(updateNeuronCoroutine);

            NotifyConvexHull();

            if (_neuronsForClusters.Count == 0) _neuronsForClusters = new List<Neuron>(_neurons);
        }
    }

    public async Task UpdateSynapsesStream(int timestep)
    {
        var oldTimeStep = spec.SynapseTimeStep;
        // spec.SynapseTimeStep = timestep;
        if (isDifferentSynapseTimestep(oldTimeStep, spec.SynapseTimeStep) || _synapses.Count == 0)
        {
            _synapses = new List<Synapse>();
            using (var bc = new BlockingCollection<Synapse>())
            {
                // Kick off a producer task
                var producerTask = Task.Run(async () =>
                {
                    var synapseQuery = new SynapseQuery(timestep, spec.SimulationId);
                    await _repository.GetSynapsesStream(synapseQuery, bc);
                });

                // Kick off a consumer task
                var consumerTask = Task.Run(() =>
                {
                    foreach (var item in bc.GetConsumingEnumerable()) _synapses.Add(item);
                });
                var corotine = UpdateSynapseView();
                StartCoroutine(corotine);

                await Task.WhenAll(producerTask, consumerTask);
                StopCoroutine(corotine);
            }
        }
    }

    public void NotifySplines()
    {
        foreach (var observer in _observers) observer.ObserverUpdateSplines(this);
    }


    public async void SetNeuronSizeEncoding(NeuronAttribute attribute)
    {
        var attributes = GetCurrentAttributes();
        var contains = attributes.Any(p => p.value == attribute.value);
        spec.NeuronSizeEncoding = attribute;
        if (!contains)
        {
            await semaphoreSlim.WaitAsync();
            try
            {
                var queryColor = new StratumQuery(
                    spec.SimulationId,
                    0,
                    spec.NeuronTimeStep,
                    new NeuronAttribute { value = spec.NeuronColorEncoding.value },
                    new NeuronFilter(Guid.NewGuid(),
                        new NeuronAttribute { value = NeuronAttributeType.Calcium }, 0, 1));

                var querySize = new StratumQuery(
                    spec.SimulationId,
                    0,
                    spec.NeuronTimeStep,
                    new NeuronAttribute { value = spec.NeuronSizeEncoding.value },
                    new NeuronFilter(Guid.NewGuid(),
                        new NeuronAttribute { value = NeuronAttributeType.Calcium }, 0, 1));

                await Stratum(queryColor);
                await Stratum(querySize);
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }
        else
        {
            NotifyNeurons();
        }

        NotifyConvexHull();
    }

    public async void SetNeuronColorEncoding(NeuronAttribute attribute)
    {
        var attributes = GetCurrentAttributes();
        var contains = attributes.Any(p => p.value == attribute.value);
        spec.NeuronColorEncoding = attribute;
        if (!contains)
        {
            await semaphoreSlim.WaitAsync();
            try
            {
                var queryColor = new StratumQuery(
                    spec.SimulationId,
                    0,
                    spec.NeuronTimeStep,
                    new NeuronAttribute { value = spec.NeuronColorEncoding.value },
                    new NeuronFilter(Guid.NewGuid(),
                        new NeuronAttribute { value = NeuronAttributeType.Calcium }, 0, 1));

                var querySize = new StratumQuery(
                    spec.SimulationId,
                    0,
                    spec.NeuronTimeStep,
                    new NeuronAttribute { value = spec.NeuronSizeEncoding.value },
                    new NeuronFilter(Guid.NewGuid(),
                        new NeuronAttribute { value = NeuronAttributeType.Calcium }, 0, 1));

                await Stratum(queryColor);
                await Stratum(querySize);

                //await UpdateNeuronsStream(spec.NeuronTimeStep); 
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }
        else
        {
            NotifyNeurons();
        }

        MakeBrainLegend();
        NotifyConvexHull();
    }

    public void SetTerrainEncoding(NeuronAttribute attribute)
    {
        // TODO: Change terrain encoding
        spec.TerrainEncoding = attribute;
        DoPipFromSpec(spec);
    }

    public void SetTerrainAggregationType(AggregationType chosenValue)
    {
        spec.AggregationType = chosenValue;
        DoPipFromSpec(spec);
    }

    public void SetTerrainClusterLevel(int chosenValue)
    {
        spec.TerrainClusterLevel = chosenValue;
        DoPipFromSpec(spec);
    }

    public async void AddNeuronFilter(NeuronFilter filter)
    {
        var attributes = GetCurrentAttributes();
        var contains = attributes.Any(p => p.value == filter.Attribute.value);
        spec.Filters.Add(filter);
        if (!contains)
        {
            await semaphoreSlim.WaitAsync();
            try
            {
                var queryColor = new StratumQuery(
                    spec.SimulationId,
                    0,
                    spec.NeuronTimeStep,
                    new NeuronAttribute { value = spec.NeuronColorEncoding.value },
                    new NeuronFilter(Guid.NewGuid(),
                        new NeuronAttribute { value = NeuronAttributeType.Calcium }, 0, 1));

                var querySize = new StratumQuery(
                    spec.SimulationId,
                    0,
                    spec.NeuronTimeStep,
                    new NeuronAttribute { value = spec.NeuronSizeEncoding.value },
                    new NeuronFilter(Guid.NewGuid(),
                        new NeuronAttribute { value = NeuronAttributeType.Calcium }, 0, 1));

                await Stratum(queryColor);
                await Stratum(querySize);

                //await UpdateNeuronsStream(spec.NeuronTimeStep); 
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }
        else
        {
            NotifyNeurons();
        }

        NotifyConvexHull();
    }

    public void RemoveNeuronFilter(Guid Id)
    {
        var filterToRemove = spec.Filters.FirstOrDefault(r => r.Id == Id);
        if (filterToRemove != null) spec.Filters.Remove(filterToRemove);
        NotifyNeurons();
    }

    public void UpdateFilter(Guid Id, NeuronFilter filter)
    {
        var filterToUpdate = spec.Filters.FirstOrDefault(r => r.Id == Id);
        if (filterToUpdate != null) filterToUpdate = filter;
        NotifyNeurons();
    }

    public void SetSimulation(SimulationType simulation_id)
    {
        spec.SimulationId = simulation_id;
    }

    public Specification GetSpec()
    {
        return spec;
    }

    public void SetSpec(Specification newSpec)
    {
        spec = newSpec;
    }

    public void SetLocalColorScale(bool localColorScaleActive)
    {
        spec.LocalColorScale = localColorScaleActive;
        NotifyNeurons();
        NotifyConvexHull();
    }
    
    public void SetDivergentColorScale(bool divergentColorScaleActive)
    {
        spec.DivergentColorScale = divergentColorScaleActive;
        NotifyNeurons();
        NotifyConvexHull();
        MakeBrainLegend();
    }

    public void SetBrushedIndicies(HashSet<int> brushedIndicies)
    {
        spec.BrushedIndicies = brushedIndicies;
        NotifySelection();
    }

    // Attach an observer to the subject.
    public void Attach(IBrainObserver observer)
    {
        _observers.Add(observer);
    }

    // Detach an observer from the subject.
    public void Detach(IBrainObserver observer)
    {
        _observers.Remove(observer);
    }

    // Notify all observers about an event.
    public void NotifyNeurons()
    {
        foreach (var observer in _observers) observer.ObserverUpdateNeurons(this);
    }

    public void NotifySynapses()
    {
        foreach (var observer in _observers) observer.ObserverUpdateSynapses(this);
    }

    public void NotifyConvexHull()
    {
        foreach (var observer in _observers) observer.ObserverUpdateConvexHull(this);
    }
    

    public Task UpdateClusters(ClusterQuery clusterQuery)
    {
        //TODO: query the database and save it in a field called _clusters
        return null;
    }

    public Task UpdateLeaves(LeaveQuery leaveQuery)
    {
        return null;
    }

    public void NotifyTerrain()
    {
        foreach (var observer in _observers) observer.ObserverUpdateTerrain(this);
    }

    public async Task UpdateTerrain(PipQuery pipsQuery)
    {
        _points = new List<List<NeuronTimestep>>();

        using (var bc = new BlockingCollection<NeuronTimestep>())
        {
            // Kick off a producer task
            var producerTask = Task.Run(async () =>
            {
                await _repository.GetPipsStream(pipsQuery, bc);
            });

            // Kick off a consumer task
            var consumerTask = Task.Run(() =>
            {
                foreach (var item in bc.GetConsumingEnumerable())
                    if (item.ZOrder >= _points.Count)
                    {
                        while (item.ZOrder >= _points.Count)
                        {
                            var new_points = new List<NeuronTimestep>();
                            _points.Add(new_points);
                        }

                        _points[item.ZOrder].Add(item);
                    }
                    else
                    {
                        _points[item.ZOrder].Add(item);
                    }
            });
            await Task.WhenAll(producerTask, consumerTask);

            NotifyTerrain();
        }
    }

    public async void DoPipQuery(int pips, int granularity, SimulationType simID,
        NeuronAttribute attrType, AggregationType aggregationType)
    {
        spec.TerrainPips = pips;
        spec.TerrainClusterLevel = granularity;
        spec.SimulationId = simID;
        spec.TerrainEncoding = attrType;
        spec.AggregationType = aggregationType;
        var pipQuery = new PipQuery(pips, granularity, simID, attrType, aggregationType);
        await UpdateTerrain(pipQuery);
    }

    public void DoPipFromSpec(Specification spec)
    {
        DoPipQuery(spec.TerrainPips, spec.TerrainClusterLevel, spec.SimulationId,
            spec.TerrainEncoding, spec.AggregationType);
    }

    private List<NeuronAttribute> GetCurrentAttributes()
    {
        var attributes = new List<NeuronAttribute>
        {
            new() { value = NeuronAttributeType.CommunityLevel1 },
            new() { value = NeuronAttributeType.CommunityLevel2 },
            new() { value = NeuronAttributeType.CommunityLevel3 },
            new() { value = NeuronAttributeType.CommunityLevel4 }
        };
        for (var i = 0; i < spec.Filters.Count; i++) attributes.Add(spec.Filters[i].Attribute);
        if (!attributes.Contains(spec.NeuronColorEncoding))
            attributes.Add(spec.NeuronColorEncoding);
        if (!attributes.Contains(spec.NeuronSizeEncoding)) attributes.Add(spec.NeuronSizeEncoding);
        return attributes;
    }

    public void UpdateNeuronAttribute(Node item, NeuronAttribute attribute)
    {
        if (_neurons.Count == 0)
            for (var i = 0; i < 50000; i++)
            {
                var neuron = new Neuron();
                neuron.Id = i;
                _neurons.Add(neuron);
            }

        var selected = _neurons[item.Id];

        switch (attribute.value)
        {
            case NeuronAttributeType.FiredFraction:
                selected.FiredFraction = item.Value;
                break;
            case NeuronAttributeType.ElectricActivity:
                selected.X = item.Value;
                break;
            case NeuronAttributeType.SecondaryVariable:
                selected.SecondaryVariable = item.Value;
                break;
            case NeuronAttributeType.Calcium:
                selected.Calcium = item.Value;
                break;
            case NeuronAttributeType.TargetCalcium:
                selected.TargetCalcium = item.Value;
                break;
            case NeuronAttributeType.SynapticInput:
                selected.SynapticInput = item.Value;
                break;
            case NeuronAttributeType.BackgroundActivity:
                selected.BackgroundActivity = item.Value;
                break;
            case NeuronAttributeType.GrownAxons:
                selected.GrownAxons = item.Value;
                break;
            case NeuronAttributeType.ConnectedAxons:
                selected.ConnectedAxons = (int)item.Value;
                break;
            case NeuronAttributeType.GrownDendrites:
                selected.GrownExcitatoryDendrites = item.Value;
                break;
            case NeuronAttributeType.ConnectedDendrites:
                selected.ConnectedExcitatoryDendrites = (int)item.Value;
                break;
            case NeuronAttributeType.Dampening:
                selected.Dampening = item.Value;
                break;
            case NeuronAttributeType.CommunityLevel1:
                selected.CommunityLevel1 = (int)item.Value;
                break;
            case NeuronAttributeType.CommunityLevel2:
                selected.CommunityLevel2 = (int)item.Value;
                break;
            case NeuronAttributeType.CommunityLevel3:
                selected.CommunityLevel3 = (int)item.Value;
                break;
            case NeuronAttributeType.CommunityLevel4:
                selected.CommunityLevel4 = (int)item.Value;
                break;
        }
    }

    private IEnumerator UpdateNeuronView()
    {
        while (true)
        {
            NotifyNeurons();
            yield return null;
        }
    }

    public bool isDifferentSynapseTimestep(int oldTimeStep, int newTimeStep)
    {
        return Math.Floor((float)oldTimeStep / 10000.0) != Math.Floor((float)newTimeStep / 10000.0);
    }

    private IEnumerator UpdateSynapseView()
    {
        while (true)
        {
            NotifySynapses();
            yield return null;
        }
    }

    public async Task UpdateSplinesStream(int timestep, int clusterId, int heirarchy,
        string parentId, List<string> childIds)
    {
        var splinesForDict = new List<Spline>();
        using (var bc = new BlockingCollection<Spline>())
        {
            // Kick off a producer task
            var producerTask = Task.Run(async () =>
            {
                var splineQuery =
                    new SplineQuery(timestep, heirarchy, clusterId, spec.SimulationId);
                await _repository.GetSplinesStream(splineQuery, bc);
            });

            // Kick off a consumer task
            var consumerTask = Task.Run(() =>
            {
                foreach (var item in bc.GetConsumingEnumerable()) splinesForDict.Add(item);


                var thisClusterSplinesData = new clusterSplinesData(childIds, splinesForDict, 1);
                clusterHeirarchyDict[parentId] = thisClusterSplinesData;
                foreach (var childId in clusterHeirarchyDict[parentId].ChildIds)
                {
                    var emptySplineList = new List<Spline>();
                    var emptyStringList = new List<string>();
                    thisClusterSplinesData =
                        new clusterSplinesData(emptyStringList, emptySplineList, 0);
                    clusterHeirarchyDict[childId] = thisClusterSplinesData;
                }


                foreach (var kvp in clusterHeirarchyDict)
                {
                    var data = kvp.Value;
                    foreach (var spline in data.Splines) _splines.Add(spline);
                }
            });
            await Task.WhenAll(producerTask, consumerTask);
            NotifySplines();
        }
    }

    private List<int> GetHullIdAndHeirarchy(string hullName)
    {
        Debug.Log(hullName);
        var IdString = Regex.Match(hullName, @"\d+").Value;
        var resultIdInt = int.Parse(IdString);

        var Granularity = Regex.Match(hullName, @"(\d+)(?!.*\d)").Value;
        var GranularityInt = int.Parse(Granularity);

        var outputList = new List<int>();
        outputList.Add(resultIdInt);
        outputList.Add(GranularityInt);

        return outputList;
    }

    //edge bundles should disappear when slider is moved (since database only has data for one time step)
    // but for some reason this is not happening. Figure this out tomorrow. Check what the database
    // returns and see the dictionary of bundled objects to verify if it is getting updated
    public async Task UpdateSplinesStreamForSlider(int timestep)
    {
        Debug.Log("Traceback__UpdateSplinesStreamForSlider__BrainSubject");
        var oldTimeStep = spec.SynapseTimeStep;
        spec.SynapseTimeStep = timestep;
        Debug.Log("Old time step: " + oldTimeStep);
        Debug.Log("Current synapse time step: " + timestep);

        if (isDifferentSynapseTimestep(oldTimeStep, spec.SynapseTimeStep) | (oldTimeStep == 0 && spec.SynapseTimeStep == 0))
        {
            var clusterIdsToUpdate = new List<string>();

            foreach (var kvp in clusterHeirarchyDict)
            {
                var parentId = kvp.Key;
                var data = kvp.Value;


                if (data.flagExploded == 1)
                {
                    Debug.Log("Will try to update " + parentId);
                    clusterIdsToUpdate.Add(parentId);
                }
            }

            foreach (var id in clusterIdsToUpdate)
            {
                Debug.Log("Object ID with exploded flag == 1: " + id);
                int hullId;
                int hullHeirarchy;
                if (id == "motherCluster")
                {
                    Debug.Log("mother cluster if statement");
                    hullId = 0;
                    hullHeirarchy = 5;
                }
                else
                {
                    var hullIdAndHeirarchy = GetHullIdAndHeirarchy(id);
                    hullId = hullIdAndHeirarchy[0];
                    hullHeirarchy = 4 - hullIdAndHeirarchy[1];
                }

                var splinesForDict = new List<Spline>();
                using (var bc = new BlockingCollection<Spline>())
                {
                    // Kick off a producer task
                    var producerTask = Task.Run(async () =>
                    {
                        var splineQuery = new SplineQuery(timestep, hullHeirarchy, hullId,
                            spec.SimulationId);
                        await _repository.GetSplinesStream(splineQuery, bc);
                    });

                    // Kick off a consumer task
                    var consumerTask = Task.Run(() =>
                    {
                        foreach (var item in bc.GetConsumingEnumerable()) splinesForDict.Add(item);
                        clusterHeirarchyDict[id].Splines = splinesForDict;
                    });
                    await Task.WhenAll(producerTask, consumerTask);
                }

                _splines = new List<Spline>();
                foreach (var kvp in clusterHeirarchyDict)
                {
                    var data = kvp.Value;
                    foreach (var spline in data.Splines)
                    {
                        Debug.Log("Adding spline to _splines");
                        _splines.Add(spline);
                    }
                }

                // if (_splines.Count > 0){
                Debug.Log("Calling NotifySplines from slider update");
                NotifySplines();
                // }  
            }
        }
    }

    private IEnumerator UpdateSplinesView()
    {
        while (true)
        {
            NotifySplines();
            yield return null;
        }
    }

    private void removeSplines(ref Dictionary<string, clusterSplinesData> clusterHeirarchyDict,
        string parentId)
    {
        clusterHeirarchyDict[parentId].Splines =
            new List<Spline>(); // Clear the splines for the given id
        clusterHeirarchyDict[parentId].flagExploded = 0; //set the exploded flag to 0
        if (clusterHeirarchyDict[parentId].ChildIds.Count > 0)
            foreach (var childId in clusterHeirarchyDict[parentId].ChildIds)
                removeSplines(ref clusterHeirarchyDict,
                    childId); // Recursively call the function for each child
    }

    public async Task RetractClusterUpdateSplines(string parentId)
    {
        removeSplines(ref clusterHeirarchyDict, parentId);
        _splines = new List<Spline>();
        foreach (var kvp in clusterHeirarchyDict)
        {
            var data = kvp.Value;
            foreach (var spline in data.Splines) _splines.Add(spline);
        }

        NotifySplines();
    }

    public void MakeTerrainLegend()
    {
        var legendMaker = GetComponentInChildren<LegendMaker>(true);
        legendMaker.MakeTerrainLegend(GetComponentInChildren<TerrainViewBuilder>(true));
    }

    public void MakeBrainLegend()
    {
        var legendMaker = transform.Find("BrainParent/ColorLegend/Canvas/ColorLegend")
            .GetComponent<LegendMaker>();
        
        legendMaker.MakeBrainLegend(GetComponentInChildren<VisualizationHandler>(true));
    }

    public void NotifySelection()
    {
        foreach (var observer in _observers) observer.ObserverUpdateSelection(this);
    }


    public List<int> ZOrder_To_ClusterIds(List<int> z_orders)
    {
        var current = new List<int>();

        for (var i = 1; i < z_orders.Count; i++)
        {
            var neuron = _points[i][0];
            if (neuron != null) current.Add(neuron.Id);
        }

        return current;
    }
}