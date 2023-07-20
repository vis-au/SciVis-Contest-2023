using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections.Concurrent;

public interface IClient{
    Task AllSynapsesStream(SynapseQuery synapseQuery, BlockingCollection<Synapse> _synapsesConcurrent);
    Task Splines(SplineQuery splineQuery, BlockingCollection<Spline> _splinesConcurrent);
    List<Synapse> DeltaSynapses(DeltaSynapseQuery deltaSynapsesQuery);
    List<Synapse> DeltaSynapsesStream(DeltaSynapseQuery deltaSynapsesQuery);
    Task AllPipsStream(PipQuery pipsQuery, BlockingCollection<NeuronTimestep> _pipsConcurrent);
    Task<List<NeuronTimestep>> BillBoard(BillBoardQuery query);

    Task Stratum(StratumQuery query, BlockingCollection<Node> _nodeConcurrent);
}