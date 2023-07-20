package main

import (
	"brain-go-grpc/brain"
	"brain-go-grpc/pipsBenchmark"
	"fmt"
	"log"
	"net"
	"os"

	"google.golang.org/grpc"
	"google.golang.org/grpc/reflection"
)

func serve() {
	println("gRPC brain server in Go")
	var listener net.Listener
	var err error
	if _, err := os.Stat("/.dockerenv"); err == nil {
		fmt.Println("Running in Docker")
		listener, err = net.Listen("tcp", ":50052")
	} else {
		listener, err = net.Listen("tcp", "localhost:50052")
	}
	if err != nil {
		panic(err)
	}
	println("Listening on port :50052")
	s := grpc.NewServer()
	brain.SetDbUris()
	brain.ConnectToMongoDB()
	brain.ConnectToMemgraph()
	brain.ConnectToRedis()

	defer brain.DisconnectDBs()

	err = brain.GetNeuronOrder()

	if err != nil {
		panic(err)
	}
	brain.RegisterBrainServer(s, &brain.Server{})
	reflection.Register(s)
	if err := s.Serve(listener); err != nil {
		log.Fatalf("failed to serve: %v", err)
	}
}

func main() {
	if len(os.Args) < 2 {
		serve()
	} else if os.Args[1] == "test" {
		if len(os.Args) < 3 {
			pipsBenchmark.Testpip("sin")
		} else {
			pipsBenchmark.Testpip(os.Args[2])
		}
	} else {
		serve()
	}
}
