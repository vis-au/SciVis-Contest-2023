using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Concurrent;

using UnityEngine;
using Brain;

public class Repository : IRepository{


    private static Repository instance = null;
    private IClient client = new GrpcClient();
    private Repository(){}
    public static Repository Instance{
        get {
            if (instance==null) {
                instance = new Repository();
            }
            return instance;
        }
    }

    public async Task GetSynapsesStream(SynapseQuery synapseQuery, BlockingCollection<Synapse> _synapsesConcurrent){
        await client.AllSynapsesStream(synapseQuery, _synapsesConcurrent);
    }

    public async Task GetSplinesStream(SplineQuery splineQuery, BlockingCollection<Spline> _splinesConcurrent){
        await client.Splines(splineQuery, _splinesConcurrent);
    }

    public async Task GetPipsStream(PipQuery pipsQuery, BlockingCollection<NeuronTimestep> _pipsConcurrent){
        await client.AllPipsStream(pipsQuery, _pipsConcurrent);
    }

    public async Task<List<NeuronTimestep>> GetBillBoard(BillBoardQuery query)
    {
        return await client.BillBoard(query);
    }

    public async Task Stratum(StratumQuery query, BlockingCollection<Node> _nodeConcurrent){
        await client.Stratum(query, _nodeConcurrent);
    }
}
