from concurrent import futures
import time
import csv

import grpc
import brain_pb2_grpc
import brain_pb2

class BrainServicer(brain_pb2_grpc.BrainServicer):
    def AllNeurons(self, request, context):
        print("AllNeurons request made")
        print(request)
        reply = brain_pb2.NeuronReply()
        for i in range(1,4):
            reply.neurons.append(brain_pb2.Neuron(id=i, calcium=i, fired_fraction=0.2))
        return reply

    def AllSynapses(self, request, context):
        print("AllSynapes request made")
        print(request)
        reply = brain_pb2.SynapseReply()

        # Send actual synapses
        with open('rank_0_step_40000_in_network.csv', newline='') as f:
            reader = csv.reader(f)
            data = list(reader)
        
        # Do not include header
        for i,j,z in data[1:]:
            reply.synapses.append(brain_pb2.Synapse(from_id=int(i),to_id=int(j),weight=int(z)))

        return reply

def serve():
    server = grpc.server(futures.ThreadPoolExecutor(max_workers=10))
    brain_pb2_grpc.add_BrainServicer_to_server(BrainServicer(),server)
    server.add_insecure_port("localhost:50051")
    server.start()
    server.wait_for_termination()
    

if __name__=="__main__":
    serve()