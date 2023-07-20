package main

import (
	"context"
	"flag"
	"log"
	"time"

	"mock-grpc/server"

	"google.golang.org/grpc"
	"google.golang.org/grpc/credentials/insecure"
)

const (
	defaultName = "world"
)

var (
	addr = flag.String("addr", "localhost:50051", "the address to connect to")
	name = flag.String("name", defaultName, "Name to greet")
)

func timeStratum(ctx context.Context, client server.MockClient) {
	pre := time.Now()
	r, err := client.Stratum(ctx, &server.StratumQuery{})
	if err != nil {
		log.Fatalf("could not greet: %v", err)
	}
	log.Printf("List returned %d values in %s", len(r.GetData()), time.Since(pre))

}

func main() {
	flag.Parse()
	// Set up a connection to the server.
	conn, err := grpc.Dial(*addr, grpc.WithTransportCredentials(insecure.NewCredentials()))
	if err != nil {
		log.Fatalf("did not connect: %v", err)
	}
	defer conn.Close()
	c := server.NewMockClient(conn)

	// Contact the server and print out its response.
	ctx, cancel := context.WithTimeout(context.Background(), 10*60*time.Second)
	defer cancel()

	timeStratum(ctx, c)
}
