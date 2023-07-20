import grpc
from grpc_interceptor import ServerInterceptor
import tomli
from random import sample

import time

import brain_pb2
import brain_pb2_grpc
from gqlalchemy import Memgraph
from pymongo import MongoClient
from dotwiz import DotWiz

from concurrent import futures

from filterParser import FilterParser, ParseFilterError

with open("server/config.toml", "rb") as f:
    config = DotWiz(tomli.load(f))

# memgraph = Memgraph(host=config.memgraph.host, port=config.memgraph.port)

filterParser = FilterParser()


def get_mongo_db():
    client = MongoClient(config.mongodb.connection_string)
    db = client[config.mongodb.db_name][config.mongodb.collection_name]
    return db


db = get_mongo_db()


class BrainSevricerInterceptor(ServerInterceptor):
    def intercept(self, method, request, context: grpc.ServicerContext, method_name):
        method_to_timestep_attrs = {
            "AllNeurons": ("timestep",),
            "AllNeuronsStream": ("timestep",),
            "AllSynapses": ("timestep",),
            "AllSynapsesStream": ("timestep",),
            "DeltaSynapses": ("timestep1", "timestep2"),
            "DeltaSynapsesStream": ("timestep1", "timestep2"),
        }
        timestep_attrs = method_to_timestep_attrs.get(method_name.split("/")[-1], ())
        timesteps = [getattr(request, a) for a in timestep_attrs]
        for timestep in timesteps:
            if not 0 <= timestep <= 1_000_000:
                context.abort(
                    code=grpc.StatusCode.INVALID_ARGUMENT,
                    details="Invalid time step, must be between 0 and 1,000,000",
                )
                return
        else:
            return method(request, context)


class BrainServicer(brain_pb2_grpc.BrainServicer):
    def AllNeurons(self, request, context):
        reply = brain_pb2.NeuronReply()
        stream_results = self.AllNeuronsStream(request, context)
        for neuron in stream_results:
            reply.neurons.append(neuron)
        return reply

    def AllNeuronsStream(self, request, context):
        cursor = db.find(
            {"step": request.timestep},
            {"_id": 0, "neuron_id": 1, "current_calcium": 1, "fired_fraction": 1},
        )
        for neuron in cursor:
            yield brain_pb2.Neuron(
                id=neuron["neuron_id"],
                calcium=neuron["current_calcium"],
                fired_fraction=neuron["fired_fraction"],
            )


    def Stratum(self, request, context):
        # print(request.timestep, request.attribute)
        cursor = db.find(
            {"step": request.timestep, "property": request.attribute},
            {"_id": 0, "values": 1},
        )
        values = next(cursor)["values"]
        print(len(values))

        t1 = time.time()
        for i, value in enumerate(values):
            yield brain_pb2.Node(
                id=i,
                value=value,
            )
        print(f"Loop took {time.time() - t1} s")

    def Neurons(self, request, context):
        reply = brain_pb2.NeuronReply()

        stream_results = self.NeuronsStream(request, context)
        for neuron in stream_results:
            reply.neurons.append(neuron)
        return reply

    def NeuronsStream(self, request, context):
        simulation_id = request.simulation_id
        timestep = request.timestep
        filters = request.filters

        try:
            mongo_filter = filterParser.parse(filters)
        except ParseFilterError:
            context.abort(
                code=grpc.StatusCode.INVALID_ARGUMENT,
                details="Cannot convert value to float",
            )
            return

        cursor = db.find(
            filter={"step": timestep, **mongo_filter},
            projection={
                "_id": 0,
                "neuron_id": 1,
                "current_calcium": 1,
                "fired_fraction": 1,
            },
        )

        for neuron in cursor:
            yield brain_pb2.Neuron(
                id=neuron["neuron_id"],
                calcium=neuron["current_calcium"],
                fired_fraction=neuron["fired_fraction"],
            )

    def AllSynapses(self, request, context):
        # For now, we just give a 10000 random sample because 700k is too much
        reply = brain_pb2.SynapseReply()
        stream_results = self.AllSynapsesStream(request, context)
        synapses = []
        for synapse in stream_results:
            synapses.append(synapse)
        sample_reply = list(sample(synapses, 10000))
        reply.synapses.extend(sample_reply)
        return reply

    def AllSynapsesStream(self, request, context):
        step = request.timestep // 10000
        # For now, we just give a 10000 random sample because 700k is too much
        query = f"""MATCH (n:Neuron)-[e:SYNAPSE]->(m:Neuron)
                    WHERE e.w{step} > 0
                    RETURN n.id as from_id, e.w{step} as weight, m.id as to_id"""
        results = memgraph.execute_and_fetch(query)

        for result in results:
            synapse = brain_pb2.Synapse(**result)
            yield synapse

    def AllSynapsesHybrid(self, request, context):
        batch_size = request.batch_size if request.batch_size != 0 else 125_000
        step = request.timestep // 10000
        # For now, we just give a 10000 random sample because 700k is too much
        query = f"""MATCH (n:Neuron)-[e:SYNAPSE]->(m:Neuron)
                    WHERE e.w{step} > 0
                    RETURN n.id as from_id, e.w{step} as weight, m.id as to_id"""
        results = memgraph.execute_and_fetch(query)

        batch_reply = brain_pb2.SynapseReply()
        for i, synapse in enumerate(results, start=1):
            batch_reply.synapses.append(brain_pb2.Synapse(**synapse))
            if i % batch_size == 0:
                yield batch_reply
                batch_reply = brain_pb2.SynapseReply()
        yield batch_reply

    def DeltaSynapses(self, request, context):
        """Get all edges that change from timestep1 to timestep2
        Returns: adjacent nodes to edge and edge weight at timestep2"""

        t1, t2 = request.timestep1 // 10000, request.timestep2 // 10000
        query = f"""MATCH (n:Neuron)-[e:SYNAPSE]->(m:Neuron)
                    WHERE e.w{t1} != e.w{t2}
                    RETURN n.id as from_id, e.w{t2} as weight, m.id as to_id"""

        results = memgraph.execute_and_fetch(query)
        reply = brain_pb2.SynapseReply()

        for result in results:
            synapse = brain_pb2.Synapse(**result)
            reply.synapses.append(synapse)
        return reply

    def DeltaSynapsesStream(self, request, context):
        """Get all edges that change from timestep1 to timestep2
        Returns: adjacent nodes to edge and edge weight at timestep2"""

        t1, t2 = request.timestep1 // 10000, request.timestep2 // 10000
        query = f"""MATCH (n:Neuron)-[e:SYNAPSE]->(m:Neuron)
                    WHERE e.w{t1} != e.w{t2}
                    RETURN n.id as from_id, e.w{t2} as weight, m.id as to_id"""

        results = memgraph.execute_and_fetch(query)

        for result in results:
            synapse = brain_pb2.Synapse(**result)
            yield synapse


def serve():
    interceptors = [BrainSevricerInterceptor()]
    server = grpc.server(
        futures.ThreadPoolExecutor(max_workers=10),
        interceptors=interceptors,
    )
    brain_pb2_grpc.add_BrainServicer_to_server(BrainServicer(), server)

    server.add_insecure_port(f"[::]:{config.port}")

    server.start()
    print(f"Server running on {config.port}...")
    server.wait_for_termination()


if __name__ == "__main__":
    serve()
