using Grpc.Core;
using UnityEngine;
using System.Collections.Concurrent;
using Brain;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

public class GrpcClient : IClient{

    private readonly Brain.Brain.BrainClient _client;
    private readonly Channel _channel;
    // private readonly string _server = "localhost:50053"; //"localhost:50052";
    private readonly string _server = "localhost:50052"; //"localhost:50052";

    public GrpcClient() {
        _channel = new Channel(_server, ChannelCredentials.Insecure);
        _client = new Brain.Brain.BrainClient(_channel);
    }

    private List<string> NeuronAttributeParser(List<NeuronAttribute> neuronAttributes){
        List<string> attributes = new List<string>();
        foreach(var attribute in neuronAttributes){
            switch(attribute.value){
                case NeuronAttributeType.Fired:
                    attributes.Add("fired");
                    break;
                case NeuronAttributeType.FiredFraction:
                    attributes.Add("fired_fraction");
                    break;
                case NeuronAttributeType.ElectricActivity:
                    attributes.Add("activity");
                    break;
                case NeuronAttributeType.Calcium:
                    attributes.Add("current_calcium");
                    break;
                case NeuronAttributeType.TargetCalcium:
                    attributes.Add("target_calcium");
                    break;
                case NeuronAttributeType.SynapticInput:
                    attributes.Add("synaptic_input");
                    break;
                case NeuronAttributeType.BackgroundActivity:
                    attributes.Add("background_input");
                    break;
                case NeuronAttributeType.GrownAxons:
                    attributes.Add("grown_axons");
                    break;
                case NeuronAttributeType.ConnectedAxons:
                    attributes.Add("connected_axons");
                    break;
                case NeuronAttributeType.GrownDendrites:
                    attributes.Add("grown_dendrites");
                    break;
                case NeuronAttributeType.ConnectedDendrites:
                    attributes.Add("connected_dendrites");
                    break;
                case NeuronAttributeType.Synapses:
                    attributes.Add("projections");
                    break;
                case NeuronAttributeType.CommunityLevel1:
                    attributes.Add("community_level1");
                    break;
                case NeuronAttributeType.CommunityLevel2:
                    attributes.Add("community_level2");
                    break;
                case NeuronAttributeType.CommunityLevel3:
                    attributes.Add("community_level3");
                    break;
                case NeuronAttributeType.CommunityLevel4:
                    attributes.Add("community_level4");
                    break;
            }
        }
        return attributes;
    }

    private string NeuronAggregationParser(AggregationType aggregation){
        switch(aggregation){
                case AggregationType.Min:
                    return "min";
                case AggregationType.Max:
                    return "max";
                case AggregationType.Average:
                    return "avg";
                default:
                    return "";
        }
    }

    public async Task Stratum(StratumQuery query, BlockingCollection<Node> _nodeConcurrent){

        List<string> currentAttributes = NeuronAttributeParser(new List<NeuronAttribute>{ query.Attribute });
        string attribute = currentAttributes[0]; // Only contains one

        using var stratumStreaming = _client.Stratum(new Brain.StratumQuery{
            Simulation = (string) query.Simulation.ToString(),
            Granularity = (uint) query.Granularity,
            Timestep = (uint) query.TimeStep,
            Attribute = (string) attribute,
            Filter = (string) ""
        });
        // Debug.Log("Printing Query Attributes");
        // Debug.Log(neuronQuery.Simulation.ToString());
        // Debug.Log(currentFilters);
        // Debug.Log("Current attributes");
        // foreach (string element in currentAttributes) {
        //     Debug.Log(element);
        // }

        // Debug.Log("Current filters");
        // foreach (string element in currentFilters) {
        //     Debug.Log(element);
        // }
        // Debug.Log(currentAttributes);
        // Debug.Log(neuronQuery.GiveAll);



        // Debug.Log(neuronsStreaming);

        try{
            while (await stratumStreaming.ResponseStream.MoveNext()){
                Node node = new Node(
                    (int) stratumStreaming.ResponseStream.Current.Id,
                    (float) stratumStreaming.ResponseStream.Current.Value);

                _nodeConcurrent.Add(node);
            }
            _nodeConcurrent.CompleteAdding();
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
        {
            Debug.Log("Stream cancelled.");
        }
    }

    public async Task AllSynapsesStream(SynapseQuery synapseQuery, BlockingCollection<Synapse> _synapsesConcurrent) {
        using var synapsesStreaming = _client.AllSynapsesStream(new AllSynapsesQuery {Timestep= (uint) synapseQuery.TimeStep, Simulation = (string) synapseQuery.Simulation.ToString()});
        try
        {
            while (await synapsesStreaming.ResponseStream.MoveNext())
            {
                Synapse res = new Synapse{
                    SourceId = (int) synapsesStreaming.ResponseStream.Current.FromId,
                    TargetId = (int) synapsesStreaming.ResponseStream.Current.ToId,
                    Weight = (int) synapsesStreaming.ResponseStream.Current.Weight
                };
                _synapsesConcurrent.Add(res);
            }
            _synapsesConcurrent.CompleteAdding();
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
        {
            Debug.Log("Stream cancelled.");
        }
    }


    public async Task Splines(SplineQuery splineQuery, BlockingCollection<Spline> _splinesConcurrent) {
        using var splinesStreaming = _client.Splines(new SplinesQuery {Timestep= (uint) splineQuery.TimeStep, Granularity = (uint) splineQuery.Heirarchy,Simulation = (string) splineQuery.Simulation.ToString(), ClusterId= (uint) splineQuery.ClusterId});
        try
        {
            while (await splinesStreaming.ResponseStream.MoveNext())
            {
                Spline res = new Spline{
                    SourceId = (string) splinesStreaming.ResponseStream.Current.FromId,
                    TargetId = (string) splinesStreaming.ResponseStream.Current.ToId,
                    Weight = (int) splinesStreaming.ResponseStream.Current.Weight,
                    splinesList = (string) splinesStreaming.ResponseStream.Current.Points
                };
                _splinesConcurrent.Add(res);
            }
            _splinesConcurrent.CompleteAdding();
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
        {
            Debug.Log("Stream cancelled.");
        }
    }

    private SimulationType NeuronParseSimulation(Pip pipResponse){
        switch(pipResponse.Simulation){
            case "no_network":
                return SimulationType.no_network;
            case "disable":
                return SimulationType.disable;
            case "stimulus":
                return SimulationType.stimulus;
            case "calcium":
                return SimulationType.calcium;
            default:
                return SimulationType.calcium;
        }
    }

public async Task AllPipsStream(PipQuery pipsQuery, BlockingCollection<NeuronTimestep> _pipsConcurrent){
        List<string> field = NeuronAttributeParser(new List<NeuronAttribute>{pipsQuery.NeuronAttribute});
        string aggregation = NeuronAggregationParser(pipsQuery.Aggregation);
        using var pipsStreaming = _client.Pips(new PipsQuery {NPips = (uint) pipsQuery.PipsNumber,
                                                                    Granularity = (uint) pipsQuery.Granularity,
                                                                    Simulation = (string) pipsQuery.Simulation.ToString(),
                                                                    Attribute = (string) field[0], // There is always only one
                                                                    Aggregation = (string) aggregation});
        try
        {
            int previous_id = -1;
            int zOrder = -1;
            while (await pipsStreaming.ResponseStream.MoveNext())
            {
                if ((int)pipsStreaming.ResponseStream.Current.Id != previous_id){
                    zOrder = zOrder + 1;
                    previous_id = (int)pipsStreaming.ResponseStream.Current.Id;
                }
                SimulationType simtyp = NeuronParseSimulation(pipsStreaming.ResponseStream.Current);
                NeuronTimestep res = new NeuronTimestep(
                    (int) pipsStreaming.ResponseStream.Current.Id,
                    (int) zOrder,
                    (float) pipsStreaming.ResponseStream.Current.Value,
                    (int) pipsStreaming.ResponseStream.Current.Timestep,
                    (SimulationType) simtyp,
                    (int)pipsStreaming.ResponseStream.Current.Granularity,
                    pipsQuery.NeuronAttribute);
                _pipsConcurrent.Add(res);
            }
            _pipsConcurrent.CompleteAdding();
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
        {
            Debug.Log("Stream cancelled.");
        }
    }

    public async Task<List<Neuron>> LeavesStream(List<int> ClusterId, int granularity, string simulaiton, int timestep ){
        LeavesQuery query = new LeavesQuery{Granularity = (uint) granularity, Simulation = simulaiton, Timestep =(uint) timestep};
        List<uint> uintClusterIds = ClusterId.ConvertAll(x => (uint)x);
        query.ClusterIds.Add(uintClusterIds);
        List<Neuron> res = new List<Neuron>();
        using var leavesStreaming = _client.Leaves(query);
        while (await leavesStreaming.ResponseStream.MoveNext())
            {
                 Neuron neuron = new Neuron{
                    Id = (int) leavesStreaming.ResponseStream.Current.Id,
                    Fired = (bool) leavesStreaming.ResponseStream.Current.Fired,
                    FiredFraction = (float) leavesStreaming.ResponseStream.Current.FiredFraction,
                    X = (float) leavesStreaming.ResponseStream.Current.Activity,
                    Calcium = (float) leavesStreaming.ResponseStream.Current.CurrentCalcium,
                    TargetCalcium = (float) leavesStreaming.ResponseStream.Current.TargetCalcium,
                    SynapticInput = (float) leavesStreaming.ResponseStream.Current.SynapticInput,
                    BackgroundActivity = (float) leavesStreaming.ResponseStream.Current.BackgroundInput,
                    GrownAxons = (float) leavesStreaming.ResponseStream.Current.GrownAxons,
                    ConnectedAxons = (int) leavesStreaming.ResponseStream.Current.ConnectedAxons,
                    GrownExcitatoryDendrites = (float) leavesStreaming.ResponseStream.Current.GrownDendrites,
                    ConnectedExcitatoryDendrites = (int) leavesStreaming.ResponseStream.Current.ConnectedDendrites,
                    Dampening = (float) leavesStreaming.ResponseStream.Current.Dampening
                };
                res.Add(neuron);
            }
        return res;
    }


    public async Task<List<NeuronTimestep>> BillBoard(BillBoardQuery query) {
        List<string> attributes = NeuronAttributeParser(new List<NeuronAttribute>{query.NeuronAttribute});

        BillboardQuery grpcQuery = new BillboardQuery{Aggregation = query.Aggregation, Attribute = attributes[0], Granularity = (uint) query.Granularity, NPips = 100, Simulation = query.Simulation.ToString()};
        List<uint> uintClusterIds = query.Cluster_ids.ConvertAll(x => (uint)x);
        grpcQuery.ClusterIds.Add(uintClusterIds);
        List<NeuronTimestep> res = new List<NeuronTimestep>();
        using var billBoardStreaming = _client.Billboard(grpcQuery);
        while (await billBoardStreaming.ResponseStream.MoveNext())
            {
                 NeuronTimestep pip = new NeuronTimestep(
                    (int) billBoardStreaming.ResponseStream.Current.Id,
                    (int) 0,
                    (float) billBoardStreaming.ResponseStream.Current.Value,
                    (int) billBoardStreaming.ResponseStream.Current.Timestep,
                    (SimulationType) query.Simulation,
                    (int)billBoardStreaming.ResponseStream.Current.Granularity,
                    query.NeuronAttribute);
                res.Add(pip);
            }
        return res;
    }


    public List<Synapse> DeltaSynapses(DeltaSynapseQuery deltaSynapseQuery) {
        return null;
    }
    public List<Synapse> DeltaSynapsesStream(DeltaSynapseQuery deltaSynapseQuery) {
        return null;
    }

    private void OnDisable() {
        _channel.ShutdownAsync().Wait();
    }
}