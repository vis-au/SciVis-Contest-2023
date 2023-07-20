package main

import (
	"log"
	"net"

	"mock-grpc/server"

	"google.golang.org/grpc"
	"google.golang.org/grpc/reflection"
)

func main() {
	println("gRPC brain server in Go")
	var listener net.Listener
	var err error
	listener, err = net.Listen("tcp", "localhost:50051")

	if err != nil {
		panic(err)
	}
	println("Listening on port :50051")
	s := grpc.NewServer()

	if err != nil {
		panic(err)
	}

	server.Setup()

	server.RegisterMockServer(s, &server.Server{})
	reflection.Register(s)
	if err := s.Serve(listener); err != nil {
		log.Fatalf("failed to serve: %v", err)
	}
}
