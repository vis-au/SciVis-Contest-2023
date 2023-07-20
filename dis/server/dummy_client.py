import grpc
import brain_pb2_grpc
import brain_pb2
import time

import random

channel = grpc.insecure_channel("localhost:50052")
stub = brain_pb2_grpc.BrainStub(channel)


def time_neuron_stream():
    print("Asking for neurons with a stream")
    neuron_query = brain_pb2.AllNeuronsQuery(timestep=0, simulation_id=1)

    pre = time.time()
    neurons = stub.AllNeuronsStream(neuron_query)

    n_neurons = sum(1 for _ in neurons)

    print(f"Time: {time.time() - pre} seconds")
    print(f"Returned {n_neurons} neurons")
    print()


def time_neuron_list():
    print("Asking for neurons with a list")
    neuron_query = brain_pb2.AllNeuronsQuery(timestep=0, simulation_id=1)

    pre = time.time()
    neurons = stub.AllNeurons(neuron_query)
    n_neurons = len(neurons.neurons)

    print(f"Time: {time.time() - pre} seconds")
    print(f"Returned {n_neurons} neurons")
    print()


def time_synapse_stream():
    print("Asking for synapses with a stream")
    synapse_query = brain_pb2.AllSynapsesQuery(timestep=0, simulation_id=1)
    pre = time.time()
    synapses = stub.AllSynapsesStream(synapse_query)
    n_synapses = len(list(synapses))

    print(f"Time: {time.time() - pre} seconds")
    print(f"Returned {n_synapses} synapses")
    print()


def time_synapse_list():
    print("Asking for synapses with a list")

    synapse_query = brain_pb2.AllSynapsesQuery(timestep=0, simulation_id=1)

    pre = time.time()
    synapses = stub.AllSynapses(synapse_query)

    print(f"Time: {time.time() - pre} seconds")
    print(f"Returned {len(synapses.synapses)} synapses")
    print()


def time_synapse_delta_stream():

    print("Asking for synapses delta with a stream")
    delta_synapse_query = brain_pb2.DeltaSynapsesQuery(
        timestep1=9 * 10000, timestep2=10 * 10000, simulation_id=1
    )
    pre = time.time()
    synapses = stub.DeltaSynapsesStream(delta_synapse_query)
    n_synapses = len(list(synapses))

    print(f"Time: {time.time() - pre} seconds")
    print(f"Returned {n_synapses} synapses")
    print()


def time_synapse_delta_list():
    print("Asking for synapses delta with a list")
    delta_synapse_query = brain_pb2.DeltaSynapsesQuery(
        timestep1=9 * 10000, timestep2=10 * 10000, simulation_id=1
    )

    pre = time.time()
    synapses = stub.DeltaSynapses(delta_synapse_query)

    print(f"Time: {time.time() - pre} seconds")
    print(f"Returned {len(synapses.synapses)} synapses")
    print()


def time_filter_neuron_stream():
    print("Asking for filtered neurons as stream:")
    neuron_query = brain_pb2.NeuronsQuery(
        timestep=0,
        simulation_id=1,
        filters=[
            "current_calcium <= 0.004",
            "current_calcium >= 0.003984",
        ],
    )

    pre = time.time()
    neurons = stub.NeuronsStream(neuron_query)
    n_neurons = len(list(neurons))

    print(f"Time: {time.time() - pre} seconds")
    print(f"Returned {n_neurons} neurons")


def time_filter_neuron_list():
    print("Asking for filtered neurons as list:")
    neuron_query = brain_pb2.NeuronsQuery(
        timestep=0,
        simulation_id=1,
        filters=[
            "current_calcium <= 0.004",
            "current_calcium >= 0.003984",
        ],
    )

    pre = time.time()
    neurons = stub.Neurons(neuron_query)
    n_neurons = len(neurons.neurons)

    print(f"Time: {time.time() - pre} seconds")
    print(f"Returned {n_neurons} neurons")


def time_synapse_hybrid(batch_size=125000):
    print(f"Asking for synapses in hybrid mode with batch size {batch_size}")
    synapse_query = brain_pb2.AllSynapsesHybridQuery(
        timestep=0, simulation_id=1, batch_size=0
    )
    n_synapses = 0
    pre = time.time()
    synapses = stub.AllSynapsesHybrid(synapse_query)

    for synapse_batch in synapses:
        n_synapses += len(synapse_batch.synapses)

    duration = time.time() - pre
    print(f"Time: {duration} seconds")
    print(f"Returned {n_synapses} synapses")
    return duration


def binary_search_hybrid(lower, upper):
    """
    Quick and dirty function to find a good batch size for the hybrid call
    """
    middle = (lower + upper) // 2

    if abs(lower - upper) < 100:
        return middle

    upper_result = time_synapse_hybrid(upper)
    lower_result = time_synapse_hybrid(lower)

    if upper_result < lower_result:
        return binary_search_hybrid(middle, upper)
    else:
        return binary_search_hybrid(lower, middle)


def time_stratum():
    # print("Asking for stratum")
    random_timestep = random.randrange(0, 1_000_000, 10_000)
    stratum_query = brain_pb2.StratumQuery(
        timestep=random_timestep,
        granularity=0,
        attribute="current_calcium",
        simulation="calcium",
        filter=None,
    )

    pre = time.time()
    neurons = stub.Stratum(stratum_query)
    n_neurons = sum(1 for _ in neurons)

    return time.time() - pre

    print(f"Time: {time.time() - pre} seconds")
    print(f"Returned {n_neurons} neurons")

if __name__ == "__main__":
    # time_neuron_list()
    # time_neuron_stream()
    # time_synapse_list()
    # time_synapse_delta_list()
    # time_synapse_delta_stream()
    # time_filter_neuron_stream()
    # time_filter_neuron_list()
    # print(binary_search_hybrid(10, 200000))
    total_time = 0
    n = 10
    for i in range(n):
        total_time += time_stratum()
    print(total_time / n)
