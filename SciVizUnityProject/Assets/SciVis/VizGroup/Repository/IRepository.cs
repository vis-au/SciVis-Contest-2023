using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections.Concurrent;

public interface IRepository{
    Task GetSynapsesStream(SynapseQuery synapseQuery, BlockingCollection<Synapse> _synapseConcurrent);
    Task GetPipsStream(PipQuery pipsQuery, BlockingCollection<NeuronTimestep> _pipsConcurrent);
    Task Stratum(StratumQuery query, BlockingCollection<Node> _nodeConcurrent);
    Task GetSplinesStream(SplineQuery splineQuery, BlockingCollection<Spline> _splinesConcurrent);
    Task<List<NeuronTimestep>> GetBillBoard(BillBoardQuery query);
}