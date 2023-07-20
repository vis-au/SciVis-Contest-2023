package main

import (
	"context"
	"flag"
	"fmt"
	"io"
	"log"
	"math"
	"math/rand"
	"time"

	"brain-go-grpc/brain"

	"google.golang.org/grpc"
	"google.golang.org/grpc/credentials/insecure"
)

const (
	defaultName = "world"
)

var (
	addr = flag.String("addr", "localhost:50053", "the address to connect to")
	name = flag.String("name", defaultName, "Name to greet")
)

func timeAllSynapsesStream(ctx context.Context, client brain.BrainClient) {
	pre := time.Now()
	randomTimestep := rand.Intn(10000) * 100
	fmt.Printf("randomTimestep: %v\n", randomTimestep)
	in := &brain.AllSynapsesQuery{Timestep: uint32(randomTimestep), Simulation: "no_network"}
	stream, err := client.AllSynapsesStream(ctx, in)
	if err != nil {
		panic(err)
	}

	done := make(chan bool)
	i := 0
	go func() {
		for {
			_, err := stream.Recv()
			if err == io.EOF {
				done <- true //close(done)
				return
			}
			if err != nil {
				log.Fatalf("can not receive %v", err)
			}

			i++
		}
	}()
	<-done
	log.Printf("Stream returned %d synapses in %s", i, time.Since(pre))
}

func timeDeltaSynapsesStream(ctx context.Context, client brain.BrainClient, t1 uint32, t2 uint32) {
	pre := time.Now()
	in := &brain.DeltaSynapsesQuery{Timestep1: t1, Timestep2: t2, Simulation: "calcium"}
	stream, err := client.DeltaSynapsesStream(ctx, in)
	if err != nil {
		panic(err)
	}

	done := make(chan bool)
	i := 0
	go func() {
		for {
			_, err := stream.Recv()
			if err == io.EOF {
				done <- true //close(done)
				return
			}
			if err != nil {
				log.Fatalf("can not receive %v", err)
			}
			i++

		}
	}()
	<-done
	log.Printf("Delta Stream returned %d synapses in %s", i, time.Since(pre))
}

func timeNeuronsStream(ctx context.Context, client brain.BrainClient, debug bool) {
	pre := time.Now()
	// randomTimestep := rand.Intn(10000) * 100
	// fmt.Printf("randomTimestep: %v\n", randomTimestep)
	// randomTimestep := 0
	in := &brain.NeuronsQuery{
		Simulation:      "no_network",
		Filters:         []string{"current_calcium > 0.004"},
		Projection:      []string{"community_level1", "community_level2"},
		GimmeEverything: false,
	}
	stream, err := client.NeuronsStream(ctx, in)
	if err != nil {
		panic(err)
	}

	done := make(chan bool)
	i := 0
	go func() {
		for {
			resp, err := stream.Recv()
			if err == io.EOF {
				done <- true //close(done)
				return
			}
			if err != nil {
				log.Fatalf("can not receive %v", err)
			}
			if i < 100 && debug {
				fmt.Printf("resp: %v\n", resp)
			}
			i++
		}
	}()
	<-done

	log.Printf("Stream (filter version) returned %d neurons in %s", i, time.Since(pre))

}

func timeHierarchyStream(ctx context.Context, client brain.BrainClient, debug bool) {
	pre := time.Now()
	// randomTimestep := rand.Intn(10000) * 100
	// fmt.Printf("randomTimestep: %v\n", randomTimestep)
	randomTimestep := 0
	in := &brain.NeuronsQuery{
		Simulation:      "calcium",
		Filters:         []string{fmt.Sprintf("timestep = %v", randomTimestep)},
		Projection:      []string{"community_level1", "community_level2", "community_level3", "community_level4"},
		GimmeEverything: false,
	}
	stream, err := client.NeuronsStream(ctx, in)
	if err != nil {
		panic(err)
	}

	done := make(chan bool)
	i := 0
	go func() {
		for {
			resp, err := stream.Recv()
			if err == io.EOF {
				done <- true //close(done)
				return
			}
			if err != nil {
				log.Fatalf("can not receive %v", err)
			}
			if debug && i < 300 {
				fmt.Printf("resp: %v\n", resp)
			}
			i++
		}
	}()
	<-done

	log.Printf("Stream (filter version) returned %d neurons in %s", i, time.Since(pre))

}

func timeStratum(ctx context.Context, client brain.BrainClient, debug bool, g uint32) float64 {
	pre := time.Now()
	randomTimestep := rand.Intn(10000) * 100
	// fmt.Printf("randomTimestep: %v\n", randomTimestep)
	// randomTimestep := 0
	var attr string
	if g == 0 {
		attr = "current_calcium"
	} else {
		attr = "max_current_calcium"
	}
	in := &brain.StratumQuery{
		Simulation:  "calcium",
		Granularity: g,
		Attribute:   attr,
		Timestep:    uint32(randomTimestep),
		// Filter:      "calcium < 0.66",
	}
	stream, err := client.Stratum(ctx, in)
	if err != nil {
		panic(err)
	}

	done := make(chan bool)
	i := 0
	go func() {
		for {
			resp, err := stream.Recv()
			if err == io.EOF {
				done <- true //close(done)
				return
			}
			if err != nil {
				log.Fatalf("can not receive %v", err)
			}
			if i < 5 && debug {
				fmt.Printf("resp: %v\n", resp)
			}
			i++
		}
	}()
	<-done
	return time.Since(pre).Seconds()

	// log.Printf("Stratum returned %d neurons in %s", i, time.Since(pre))

}

func timePips(ctx context.Context, client brain.BrainClient, debug bool) {
	pre := time.Now()
	nPips := 100
	in := &brain.PipsQuery{
		NPips:       uint32(nPips),
		Granularity: 2,
		Simulation:  "no_network",
		Attribute:   "fired",
		Aggregation: "avg",
	}
	stream, err := client.Pips(ctx, in)
	if err != nil {
		panic(err)
	}

	done := make(chan bool)
	i := 0

	go func() {
		prev_timestep := uint32(0)
		for {
			resp, err := stream.Recv()
			if err == io.EOF {
				done <- true //close(done)
				return
			}
			if err != nil {
				log.Fatalf("can not receive %v", err)
			}
			if debug && i%1000 == 0 {
				log.Printf("Resp %v received: %s", i, resp)
			}
			if debug && i%nPips != 0 && resp.Timestep < prev_timestep {
				log.Fatalf("timestep went backwards: %v -> %v %v", prev_timestep, resp.Timestep, i%nPips)
			}
			prev_timestep = resp.Timestep
			i++
		}
	}()
	<-done

	log.Printf("Pips returned %d neurons in %s", i, time.Since(pre))

}

func timeClusterEdges(ctx context.Context, client brain.BrainClient, granularity uint32, timestep uint32, debug bool) {
	pre := time.Now()
	in := &brain.ClusterEdgesQuery{Timestep: timestep, Simulation: "stimulus", Granularity: granularity}
	stream, err := client.ClusterEdges(ctx, in)
	if err != nil {
		panic(err)
	}

	done := make(chan bool)
	i := 0
	go func() {
		for {
			res, err := stream.Recv()
			if err == io.EOF {
				done <- true //close(done)
				return
			}
			if err != nil {
				log.Fatalf("can not receive %v", err)
			}
			if debug && i%1000 == 49 {
				fmt.Printf("received: %v\n", res)
			}
			i++
		}
	}()
	<-done
	log.Printf("Stream returned %d edges in %s", i, time.Since(pre))
}

func timeLeaves(ctx context.Context, client brain.BrainClient, granularity uint32, clusterIds []uint32, timestep uint32, debug bool) {
	pre := time.Now()
	in := &brain.LeavesQuery{Timestep: timestep, Simulation: "no_network", Granularity: granularity, ClusterIds: clusterIds}
	stream, err := client.Leaves(ctx, in)
	if err != nil {
		panic(err)
	}

	done := make(chan bool)
	i := 0
	go func() {
		for {
			res, err := stream.Recv()
			if err == io.EOF {
				done <- true //close(done)
				return
			}
			if err != nil {
				log.Fatalf("can not receive %v", err)
			}
			if i%1000 == 0 && debug {
				fmt.Printf("received: %v\n", res)
			}
			i++
		}
	}()
	<-done
	log.Printf("Stream returned %d leaves in %s", i, time.Since(pre))
}
func timeEdgesInCluster(ctx context.Context, client brain.BrainClient, granularity uint32, clusterId uint32, timestep uint32, debug bool) {
	pre := time.Now()
	in := &brain.EdgesInClusterQuery{Timestep: timestep, Simulation: "no_network", Granularity: granularity, ClusterId: clusterId}
	stream, err := client.EdgesInCluster(ctx, in)
	if err != nil {
		panic(err)
	}

	done := make(chan bool)
	i := 0
	go func() {
		for {
			res, err := stream.Recv()
			if err == io.EOF {
				done <- true //close(done)
				return
			}
			if err != nil {
				log.Fatalf("can not receive %v", err)
			}
			if debug && i%1000 == 49 {
				fmt.Printf("received: %v\n", res)
			}
			i++
		}
	}()
	<-done
	log.Printf("Stream returned %d cluster edges in %s", i, time.Since(pre))
}
func timeNodesInCluster(ctx context.Context, client brain.BrainClient, granularity uint32, clusterId uint32, timestep uint32, debug bool) {
	pre := time.Now()
	in := &brain.NodesInClusterQuery{Timestep: timestep, Simulation: "no_network", Granularity: granularity, ClusterId: clusterId, Projection: []string{"min_current_calcium", "avg_current_calcium"}}
	stream, err := client.NodesInCluster(ctx, in)
	if err != nil {
		panic(err)
	}

	done := make(chan bool)
	i := 0
	go func() {
		for {
			res, err := stream.Recv()
			if err == io.EOF {
				done <- true //close(done)
				return
			}
			if err != nil {
				log.Fatalf("can not receive %v", err)
			}
			if debug {
				fmt.Printf("received: %v\n", res)
			}
			i++
		}
	}()
	<-done
	log.Printf("Stream returned %d nodes inside cluster %d in %s", i, in.ClusterId, time.Since(pre))
}

func timeClusters(ctx context.Context, client brain.BrainClient, granularity uint32, timestep uint32, debug bool) {
	pre := time.Now()
	in := &brain.ClustersQuery{Timestep: timestep, Simulation: "no_network", Granularity: granularity,
		Projection: []string{"avg_current_calcium", "avg_fired_fraction"}}
	stream, err := client.Clusters(ctx, in)
	if err != nil {
		panic(err)
	}

	done := make(chan bool)
	i := 0
	go func() {
		for {
			res, err := stream.Recv()
			if err == io.EOF {
				done <- true //close(done)
				return
			}
			if err != nil {
				log.Fatalf("can not receive %v", err)
			}
			if debug && i%100 == 0 {
				fmt.Printf("received: %v\n", res)
			}
			i++
		}
	}()
	<-done
	log.Printf("Stream returned %d clusters at granularity %d in %s", i, in.Granularity, time.Since(pre))
}

func timeBillboard(ctx context.Context, client brain.BrainClient, granularity uint32, clusterIds []uint32, debug bool) {
	pre := time.Now()
	in := &brain.BillboardQuery{
		Simulation:  "no_network",
		Granularity: granularity,
		Attribute:   "current_calcium",
		Aggregation: "max",
		NPips:       100,
		ClusterIds:  clusterIds}
	stream, err := client.Billboard(ctx, in)
	if err != nil {
		panic(err)
	}

	done := make(chan bool)
	i := 0
	go func() {
		for {
			res, err := stream.Recv()
			if err == io.EOF {
				done <- true //close(done)
				return
			}
			if err != nil {
				log.Fatalf("can not receive %v", err)
			}
			if debug {
				fmt.Printf("received: %v\n", res)
			}
			i++
		}
	}()
	<-done
	log.Printf("Billboard returned %d pips at granularity %d in %s", i, in.Granularity, time.Since(pre))
}

func timeSplines(ctx context.Context, client brain.BrainClient, debug bool) {
	pre := time.Now()
	randomTimestep := rand.Intn(10000) * 100
	in := &brain.SplinesQuery{
		Simulation:  "calcium",
		Granularity: 4,
		ClusterId:   uint32(rand.Intn(9)),
		Timestep:    uint32(randomTimestep)}
	stream, err := client.Splines(ctx, in)
	if err != nil {
		panic(err)
	}

	done := make(chan bool)
	i := 0
	go func() {
		for {
			_, err := stream.Recv()
			if err == io.EOF {
				done <- true //close(done)
				return
			}
			if err != nil {
				log.Fatalf("can not receive %v", err)
			}
			// if debug {
			// 	fmt.Printf("received: %v\n", res)
			// }
			i++
		}
	}()
	<-done
	log.Printf("Splines returned %d splines at granularity %d in %s", i, in.Granularity, time.Since(pre))
}

func calculateMean(arr []float64) float64 {
	n := len(arr)
	if n <= 1 {
		return 0.0
	}

	// Step 1: Calculate the mean
	sum := 0.0
	for _, num := range arr {
		sum += num
	}
	mean := sum / float64(n)
	return mean
}

func calculateStandardDeviation(arr []float64) float64 {
	mean := calculateMean(arr)
	n := len(arr)
	if n <= 1 {
		return 0.0
	}
	// Step 2: Calculate the sum of squared differences
	sumOfSqDiff := 0.0
	for _, num := range arr {
		diff := num - mean
		sumOfSqDiff += diff * diff
	}

	// Step 3: Calculate the variance
	variance := sumOfSqDiff / float64(n-1)

	// Step 4: Calculate the standard deviation
	standardDeviation := math.Sqrt(variance)

	return standardDeviation
}

func main() {
	flag.Parse()
	// Set up a connection to the server.
	conn, err := grpc.Dial(*addr, grpc.WithTransportCredentials(insecure.NewCredentials()))
	if err != nil {
		log.Fatalf("did not connect: %v", err)
	}
	defer conn.Close()
	c := brain.NewBrainClient(conn)

	// Contact the server and print out its response.
	ctx, cancel := context.WithTimeout(context.Background(), 10*60*time.Second)
	defer cancel()

	// timeAllNeurons(ctx, c)
	// timeAllNeuronsStream(ctx, c)
	// timeDeltaSynapses(ctx, c, 500000, 510000)
	// timeDeltaSynapsesStream(ctx, c, 500000, 510000)
	// timeDeltaSynapsesStream(ctx, c, 500000, 520000)
	// timeAllSynapses(ctx, c)
	timeAllSynapsesStream(ctx, c)
	// timeNeurons(ctx, c)
	// timeNeuronsStream(ctx, c, true)
	// timeHierarchyStream(ctx, c, true)
	// timePips(ctx, c, false)

	// timeClusterEdges(ctx, c, 0, 800_000, true)
	// timeLeaves(ctx, c, 3, []uint32{0, 2}, 900000, true)
	// timeClusters(ctx, c, 1, 0, true)
	// timeEdgesInCluster(ctx, c, 2, 0, 220_000, true)
	// timeNodesInCluster(ctx, c, 2, 10, 834200, true)

	// ids := []uint32{}
	// rand.Seed(90)
	// for i := 0; i < 100; i++ {
	// 	ids = append(ids, uint32(rand.Intn(9)))
	// }
	// fmt.Printf("ids: %v\n", ids)
	// timeBillboard(ctx, c, 4, ids, false)
	var Times []float64
	for g := 0; g < 5; g++ {
		Times = []float64{}
		for i := 0; i < 10; i++ {
			Times = append(Times, timeStratum(ctx, c, false, uint32(g)))
		}
		fmt.Printf("g: %v, mean: %v, std: %v\n", g, calculateMean(Times), calculateStandardDeviation(Times))
	}

	// timeSplines(ctx, c, false)
}
