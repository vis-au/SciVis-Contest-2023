import brain_pb2
import brain_pb2_grpc
import time
import grpc

def run():
    with grpc.insecure_channel('localhost:50051') as channel:
        stub = brain_pb2_grpc.BrainStub(channel)
        print("1. neurons")
        print("2. synapses")
        rpc_call = input("Which rpc call would you like to make? : ")

        if rpc_call == "1":
            neuron_request = brain_pb2.AllNeuronsQuery(timestep=100,simulation_id=1)
            neuron_replies = stub.AllNeurons(neuron_request)
            print("Neuron Response Received:")

            print(neuron_replies)
        elif rpc_call == "2":
            synapses_request = brain_pb2.AllSynapsesQuery(timestep=100,simulation_id=1)
            synapses_replies = stub.AllSynapses(synapses_request)
            print("Synapses Response Received:")
            print(synapses_replies)
        else:
            print("Not a valid option")


if __name__=="__main__":
    run()