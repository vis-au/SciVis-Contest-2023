package brain

import (
	"bufio"
	context "context"
	"fmt"
	"io"
	"os"
	"sort"
	"strconv"
	"strings"

	"brain-go-grpc/pip"

	"go.mongodb.org/mongo-driver/bson"
	"go.mongodb.org/mongo-driver/mongo"
	"go.mongodb.org/mongo-driver/mongo/options"
	"google.golang.org/grpc"

	"golang.org/x/exp/slices"

	"github.com/neo4j/neo4j-go-driver/v5/neo4j"

	"github.com/redis/go-redis/v9"
)

var mongoDb *mongo.Client
var memgraphDriver neo4j.DriverWithContext

var redisDb *redis.Client

type PipStream interface {
	Send(*Pip) error
	grpc.ServerStream
}
type DatabaseResult struct {
	Id     uint
	Values []float64
}

var simulations = []string{"calcium", "no_network", "disable", "stimulus"}
var aggregations = []string{"max", "min", "avg"}
var zorders = [5]map[int]int{}

func DisconnectDBs() {
	if err := mongoDb.Disconnect(context.TODO()); err != nil {
		panic(err)
	}
	if err := redisDb.Close(); err != nil {
		panic(err)
	}

}

var mongoUri, memgraphUri, redisUri string

const useOdin = false

func SetDbUris() {
	if _, err := os.Stat("/.dockerenv"); err == nil {
		fmt.Println("Running in Docker")
		mongoUri = "mongodb://mongo:27017"
		memgraphUri = "bolt://memgraph:7687"
		redisUri = "redis:6379"
	} else if useOdin {
		mongoUri = "mongodb://localhost:27017"
		memgraphUri = "bolt://localhost:7687"
		redisUri = "localhost:6379"
	} else {
		mongoUri = "mongodb://localhost:27017"
		memgraphUri = "bolt://localhost:7687"
		redisUri = "localhost:6379"
	}
}

func ConnectToMongoDB() {
	// Use the SetServerAPIOptions() method to set the Stable API version to 1
	serverAPI := options.ServerAPI(options.ServerAPIVersion1)
	opts := options.Client().ApplyURI(mongoUri).SetServerAPIOptions(serverAPI).SetMinPoolSize(10)
	// Create a new client and connect to the server
	client, err := mongo.Connect(context.TODO(), opts)
	if err != nil {
		panic(err)
	}

	// Send a ping to confirm a successful connection
	var result bson.M
	if err := client.Database("calcium").RunCommand(context.TODO(), bson.D{{"ping", 1}}).Decode(&result); err != nil {
		panic(err)
	}
	mongoDb = client
	fmt.Println("Pinged your deployment. You successfully connected to MongoDB!")
}

func ConnectToMemgraph() {
	driver, err := neo4j.NewDriverWithContext(memgraphUri, neo4j.BasicAuth("", "", ""))
	if err != nil {
		panic(err)
	}
	memgraphDriver = driver
	fmt.Println("Connected to Memgraph!")

}

func ConnectToRedis() {
	rdb := redis.NewClient(&redis.Options{
		Addr:     redisUri,
		Password: "", // no password set
		DB:       0,  // use default DB
	})
	redisDb = rdb
	if _, err := rdb.Ping(context.TODO()).Result(); err != nil {
		panic(err)
	}
	fmt.Println("Pinged server. Successfully connected to Redis!")
}

// ReadInts reads whitespace-separated ints from r. If there's an error, it
// returns the ints successfully read so far as well as the error value.
func ReadInts(r io.Reader) ([]int, error) {
	scanner := bufio.NewScanner(r)
	scanner.Split(bufio.ScanWords)
	var result []int
	for scanner.Scan() {
		x, err := strconv.Atoi(scanner.Text())
		if err != nil {
			return result, err
		}
		result = append(result, x)
	}
	return result, scanner.Err()
}

func GetNeuronOrder() error {
	for i := 0; i < 5; i++ {
		// open zorder.txt as a reader
		file, err := os.Open(fmt.Sprintf("neuron_order_level_%v.txt", i))
		if err != nil {
			return err
		}
		defer file.Close()
		reader := bufio.NewReader(file)
		ints, err := ReadInts(reader)
		if err != nil {
			return err
		}
		zorders[i] = make(map[int]int, len(ints))

		for j := 0; j < len(ints); j++ {
			zorders[i][ints[j]] = j
		}
	}
	fmt.Println("Read Z Order levels")
	return nil
}

func AggregateOneTimestep(timestep_values []float64, leaves []float64, aggregation string) (float64, error) {
	min_value := timestep_values[0]
	max_value := timestep_values[0]
	sum := 0.0
	leaves_sum := 0.0
	for index, value := range timestep_values {
		if value < min_value {
			min_value = value
		}
		if value > max_value {
			max_value = value
		}
		sum = sum + value
		leaves_sum = leaves_sum + leaves[index]
	}
	aggregation = strings.ToLower(aggregation)
	if aggregation == "min" {
		return min_value, nil
	} else if aggregation == "max" {
		return max_value, nil
	} else if aggregation == "avg" {
		return sum / leaves_sum, nil
	}
	return 0, fmt.Errorf("error aggregating a timestep")
}

func AggregateDataTogether(node_values map[uint][]float64, leaves_result *DatabaseResult, aggregation string, length int) ([]float64, error) {

	number_of_nodes := len(node_values)
	results := make([]float64, length)
	var err error
	for index := 0; index < length; index++ {
		one_timestep_values := make([]float64, number_of_nodes)
		leaves := make([]float64, number_of_nodes)

		i := 0
		for key := range node_values {
			one_timestep_values[i] = node_values[key][index]
			leaves[i] = leaves_result.Values[key]
			i++
		}
		results[index], err = AggregateOneTimestep(one_timestep_values, leaves, aggregation)
		if err != nil {
			return nil, err
		}
	}

	return results, nil
}

func GetCollectionName(collection_type string, granularity int) string {
	return fmt.Sprintf("level_%d_%s", granularity, collection_type)
}

func GetCollection(simulationName string, collectionType string, granularity int) (*mongo.Collection, error) {
	lowercased := strings.ToLower(simulationName)
	if !slices.Contains(simulations, lowercased) {
		return nil, fmt.Errorf("%s is not a valid simulation name. Must be one of %v", lowercased, simulations)
	}
	collection_name := GetCollectionName(collectionType, granularity)
	fmt.Printf("collection_name: %v\n", collection_name)
	fmt.Printf("lowercased: %v\n", lowercased)
	collection := mongoDb.Database(lowercased).Collection(collection_name)

	return collection, nil
}

// server struct that implements the server interface
type Server struct {
	UnimplementedBrainServer
}

func (s *Server) AllSynapsesStream(in *AllSynapsesQuery, stream Brain_AllSynapsesStreamServer) error {

	ctx := context.TODO()
	collection, err := GetCollection(in.Simulation, "edges", 0)
	if err != nil {
		return err
	}
	// previous multiple of 10_000
	floored_timestamp := in.Timestep - (in.Timestep % 10_000)

	sampleStage := bson.D{{"$sample", bson.D{{"size", 10000}}}}
	matchStage := bson.D{{"$match", bson.D{{"step", floored_timestamp}}}}
	unsetStage := bson.D{{"$unset", bson.A{"_id", "edge_id", "step"}}}
	cursor, err := collection.Aggregate(ctx,
		mongo.Pipeline{matchStage, sampleStage, unsetStage},
	)
	if err != nil {
		return err
	}
	defer cursor.Close(ctx)

	for cursor.Next(ctx) {
		var result map[string]float64
		err := cursor.Decode(&result)
		if err != nil {
			return err
		}
		synapse := &Synapse{FromId: uint32(result["from_id"]), Weight: uint32(result["weight"]), ToId: uint32(result["to_id"])}
		if err := stream.Send(synapse); err != nil {
			return err
		}
	}

	return nil
}

func (s *Server) ConsecutiveDeltaSynapsesStream(in *DeltaSynapsesQuery, stream Brain_DeltaSynapsesStreamServer) error {
	var step uint32
	if in.Timestep1 > in.Timestep2 {
		step = in.Timestep1
	} else {
		step = in.Timestep2
	}

	ctx := context.TODO()
	opts := options.Find().SetProjection(bson.D{{"_id", 0}, {"edge_id", 0}, {"step", 0}})
	collection, err := GetCollection(in.Simulation, "deltas", 0)
	if err != nil {
		return err
	}
	cursor, err := collection.Find(ctx,
		bson.D{{"step", step}},
		opts,
	)
	if err != nil {
		return err
	}
	defer cursor.Close(ctx)

	for cursor.Next(ctx) {
		var result map[string]float64
		err := cursor.Decode(&result)
		if err != nil {
			return err
		}
		synapse := &Synapse{FromId: uint32(result["from_id"]), Weight: uint32(result["weight"]), ToId: uint32(result["to_id"])}
		if err := stream.Send(synapse); err != nil {
			return err
		}
	}

	return nil
}

func (s *Server) DeltaSynapsesStream(in *DeltaSynapsesQuery, stream Brain_DeltaSynapsesStreamServer) error {
	t1 := in.Timestep1 / 10_000
	t2 := in.Timestep2 / 10_000
	if t1 == t2-1 || t2 == t1-1 {
		err := s.ConsecutiveDeltaSynapsesStream(in, stream)
		return err
	}
	// read mode.

	return fmt.Errorf("DeltaSynapses must be consecutive timesteps")
}

func ClusterHierarchy(stream Brain_NeuronsStreamServer, proj bson.D) error {
	collection := mongoDb.Database("brain").Collection("hierarchy")
	opts := options.Find().SetProjection(bson.D{{"_id", 0}}).
		SetAllowDiskUse(true).
		SetBatchSize(1000)
	ctx := context.TODO()
	cursor, err := collection.Find(ctx,
		bson.D{{}},
		opts,
	)
	if err != nil {
		return err
	}
	defer cursor.Close(ctx)

	for cursor.Next(ctx) {

		var result map[string]float64
		err := cursor.Decode(&result)
		if err != nil {
			return err
		}

		neuron := GetPopulatedGenericNeuron(result, proj)
		neuron.Id = uint32(result["neuron_id"])

		stream.Send(neuron)

	}
	return nil
}

func (s *Server) NeuronsStream(in *NeuronsQuery, stream Brain_NeuronsStreamServer) error {
	mongoProjection, err := ParseNeuronProjection(in.Projection, in.GimmeEverything)
	if err != nil {
		return err
	}
	fmt.Printf("[NeuronsStream] mongoProjection: %v\n", mongoProjection.Map())

	if fmt.Sprint(mongoProjection.Map()) == fmt.Sprint(map[string]int{"_id": 0, "community_level1": 1, "community_level2": 1, "community_level3": 1, "community_level4": 1, "neuron_id": 1}) {
		fmt.Println("Using ClusterHierarchy")
		return ClusterHierarchy(stream, mongoProjection)
	}

	opts := options.Find().SetProjection(bson.D{{"values", 1}, {"property", 1}, {"_id", 0}}).
		SetAllowDiskUse(true).
		SetBatchSize(1000)
	collection, err := GetCollection(in.Simulation, "nodes_per_step", 0)
	if err != nil {
		return err
	}
	// mongoFilters, err := ParseFilters(in.Filters)
	// if err != nil {
	// 	return err
	// }

	timestep, filterField, filterFunction, err := getTimestepAndFilterFromFilters(in.Filters)
	if err != nil {
		return err
	}

	// fmt.Printf("[NeuronsStream] mongoFilters: %v\n", mongoFilters)
	mongoFilters := bson.D{{"step", timestep}, {"property", bson.M{"$in": in.Projection}}}
	ctx := context.TODO()
	cursor, err := collection.Find(ctx,
		mongoFilters,
		opts,
	)
	if err != nil {
		return err
	}
	defer cursor.Close(ctx)

	allResults := make(map[string][50_000]float64)
	for cursor.Next(ctx) {
		type Reply struct {
			Values   []float64
			Property string
		}
		result := Reply{}
		err := cursor.Decode(&result)
		if err != nil {
			return err
		}
		allResults[result.Property] = [50000]float64(result.Values)
	}

	// get properties from hierarchy collection
	mongoFilters = bson.D{{"property", bson.M{"$in": in.Projection}}}
	cursor, err = mongoDb.Database("brain").Collection("level_0").Find(ctx, mongoFilters, opts)
	if err != nil {
		return err
	}
	defer cursor.Close(ctx)

	for cursor.Next(ctx) {
		type Reply struct {
			Values   []float64
			Property string
		}
		result := Reply{}
		err := cursor.Decode(&result)
		if err != nil {
			return err
		}
		allResults[result.Property] = [50000]float64(result.Values)
	}

	for i := 0; i < 50_000; i++ {
		if val, ok := allResults[filterField]; ok && !filterFunction(val[i]) {
			continue
		}
		neuron := GenericNeuron{}
		for property := range allResults {
			neuron = PopulateGenericNeuron(property, allResults[property][i], neuron)
		}
		neuron.Id = uint32(i)

		stream.Send(&neuron)
	}
	return nil
}

func (s *Server) Stratum(in *StratumQuery, stream Brain_StratumServer) error {
	hierarchy_attributes := []string{"community_level1", "community_level2", "community_level3", "community_level4", "leaves_count", "children_count"}

	opts := options.Find().SetProjection(bson.D{{"_id", 0}, {"values", 1}}).
		SetAllowDiskUse(true)
	ctx := context.TODO()

	var collection *mongo.Collection
	var mongoFilters bson.M
	var err error
	attribute := in.Attribute

	if slices.Contains(hierarchy_attributes, attribute) {

		collection = mongoDb.Database("brain").Collection(fmt.Sprintf("level_%d", int(in.Granularity)))
		mongoFilters = bson.M{"property": attribute}
	} else {
		collection, err = GetCollection(in.Simulation, "nodes_per_step", int(in.Granularity))
		if err != nil {
			return err
		}
		mongoFilters = bson.M{"step": in.Timestep, "property": attribute}
	}

	var filter Filter
	if len(in.Filter) < 1 {
		filter = func(a float64) bool { return true }
	} else {
		filter, err = ParseSingleFilter(in.Filter)
	}
	if err != nil {
		return err
	}

	cursor, err := collection.Find(ctx,
		mongoFilters,
		opts,
	)
	if err != nil {
		return err
	}
	defer cursor.Close(ctx)

	// send the data
	for cursor.Next(ctx) {
		var results map[string][]float64
		// results := make(map[string][50000]float64)
		err := cursor.Decode(&results)
		if err != nil {
			return err
		}

		for i, value := range results["values"] {
			if !filter(value) {
				continue
			}
			node := &Node{Id: uint32(i), Value: float32(value)}
			stream.Send(node)
		}

	}
	return nil
}

func (s *Server) ClusterEdges(in *ClusterEdgesQuery, stream Brain_ClusterEdgesServer) error {
	ctx := context.TODO()

	collection, err := GetCollection(in.Simulation, "edges", int(in.Granularity))
	if err != nil {
		return err
	}

	// previous multiple of 10_000
	flooredTimestamp := in.Timestep - (in.Timestep % 10_000)

	opts := options.Find().SetProjection(bson.D{{"_id", 0}, {"to_id", 1}, {"from_id", 1}, {"weight", 1}})
	cursor, err := collection.Find(ctx,
		bson.D{{"step", flooredTimestamp}},
		opts,
	)

	if err != nil {
		return err
	}
	defer cursor.Close(ctx)

	for cursor.Next(ctx) {
		var result map[string]float64
		err := cursor.Decode(&result)
		if err != nil {
			return err
		}
		edge := &ClusterEdge{FromId: uint32(result["from_id"]), Weight: uint32(result["weight"]), ToId: uint32(result["to_id"]), Granularity: in.Granularity}
		if err := stream.Send(edge); err != nil {
			return err
		}
	}
	return nil
}

func (s *Server) Clusters(in *ClustersQuery, stream Brain_ClustersServer) error {
	ctx := context.TODO()

	projection, err := ParseClusterProjection(in.Projection)
	if err != nil {
		return err
	}

	collection, err := GetCollection(in.Simulation, "nodes", int(in.Granularity))
	if err != nil {
		return err
	}

	// previous multiple of 100
	flooredTimestamp := in.Timestep - (in.Timestep % 100)

	opts := options.Find().SetProjection(projection)

	cursor, err := collection.Find(ctx,
		bson.D{{"step", flooredTimestamp}},
		opts,
	)

	if err != nil {
		return err
	}
	defer cursor.Close(ctx)

	for cursor.Next(ctx) {
		var result map[string]float64
		err := cursor.Decode(&result)
		if err != nil {
			return err
		}
		cluster := GetPopulatedCluster(result, projection)
		if err := stream.Send(cluster); err != nil {
			return err
		}
	}
	return nil

}

func (s *Server) Leaves(in *LeavesQuery, stream Brain_LeavesServer) error {
	ctx := context.TODO()

	if in.Granularity == 0 {
		return fmt.Errorf("cannot ask for leaves of a granularity 0 cluster (it is already a neuron leaf)")
	}

	collection := mongoDb.Database("brain").Collection("hierarchy")
	// collection, err := GetCollection(in.Simulation, "nodes", 0)
	// if err != nil {
	// 	return err
	// }

	clusterIds := in.ClusterIds
	granularity := in.Granularity

	property := fmt.Sprintf("community_level%d", granularity)

	fmt.Printf("Looking at %s\n", property)

	opts := options.Find().SetProjection(bson.D{{"_id", 0}, {"neuron_id", 1}})
	cursor, err := collection.Find(ctx,
		bson.D{{property, bson.M{"$in": clusterIds}}}, opts)
	if err != nil {
		return err
	}
	defer cursor.Close(ctx)

	for cursor.Next(ctx) {
		var result map[string]float64
		err := cursor.Decode(&result)
		if err != nil {
			return err
		}
		neuron := &GenericNeuron{Id: uint32(result["neuron_id"])}
		if err := stream.Send(neuron); err != nil {
			return err
		}
	}
	return nil

}

func (s *Server) EdgesInCluster(in *EdgesInClusterQuery, stream Brain_EdgesInClusterServer) error {
	ctx := context.TODO()

	clusterId := in.ClusterId

	if in.Granularity == 0 {
		return fmt.Errorf("cannot ask for edges inside a granularity 0 cluster")
	} else if in.Granularity > 4 {
		return fmt.Errorf("granularity cannot be %d, must be smaller  than 5", in.Granularity)
	}

	collection, err := GetCollection(in.Simulation, "edges", int(in.Granularity)-1)
	if err != nil {
		return err
	}

	// previous multiple of 10_000
	flooredTimestamp := in.Timestep - (in.Timestep % 10_000)

	opts := options.Find().SetProjection(bson.D{{"_id", 0}, {"to_id", 1}, {"from_id", 1}, {"weight", 1}})
	cursor, err := collection.Find(ctx,
		bson.D{{"step", flooredTimestamp}, {"from_community", clusterId}, {"to_community", clusterId}},
		opts,
	)

	if err != nil {
		return err
	}
	defer cursor.Close(ctx)

	for cursor.Next(ctx) {
		var result map[string]float64
		err := cursor.Decode(&result)
		if err != nil {
			return err
		}
		edge := &ClusterEdge{FromId: uint32(result["from_id"]), Weight: uint32(result["weight"]), ToId: uint32(result["to_id"]), Granularity: in.Granularity - 1}
		if err := stream.Send(edge); err != nil {
			return err
		}
	}
	return nil
}

func (s *Server) NodesInCluster(in *NodesInClusterQuery, stream Brain_NodesInClusterServer) error {
	ctx := context.TODO()

	clusterId := in.ClusterId
	// attribute := in.Projection[0]

	if in.Granularity < 2 {
		return fmt.Errorf("cannot ask for nodes inside a granularity 0 or 1 cluster, maybe use stratum?")
	} else if in.Granularity > 4 {
		return fmt.Errorf("granularity cannot be %d, must be smaller  than 5", in.Granularity)
	}

	// first find neurons ids that are in this cluster:
	var this_community_string string
	hierarchy_collection := mongoDb.Database("brain").Collection("hierarchy")
	parent_community_string := fmt.Sprintf("community_level%d", int(in.Granularity))

	if in.Granularity == 1 {
		this_community_string = "neuron_id"
	} else {
		this_community_string = fmt.Sprintf("community_level%d", int(in.Granularity)-1)
	}
	hierarchy_opts := options.Find().SetProjection(bson.D{{"_id", 0}, {this_community_string, 1}})

	cursor, err := hierarchy_collection.Find(ctx, bson.D{{parent_community_string, clusterId}}, hierarchy_opts)
	if err != nil {
		return err
	}

	var child_nodes = make(map[int]struct{})
	for cursor.Next(ctx) {
		var res map[string]int
		if err := cursor.Decode(&res); err != nil {
			return err
		}
		child_nodes[res[this_community_string]] = struct{}{}
	}

	// then find the property of those child nodes:
	collection, err := GetCollection(in.Simulation, "nodes_per_step", int(in.Granularity)-1)
	if err != nil {
		return err
	}

	all_values := map[string][50_000]float64{}
	// previous multiple of 100
	flooredTimestamp := in.Timestep - (in.Timestep % 100)

	opts := options.Find().SetProjection(bson.D{{"_id", 0}, {"values", 1}})
	for _, attribute := range in.Projection {

		cursor, err = collection.Find(ctx, bson.D{{"step", flooredTimestamp}, {"property", attribute}}, opts)
		defer cursor.Close(ctx)

		cluster_results := map[string][50_000]float64{}
		if !cursor.Next(ctx) {
			return fmt.Errorf("some error occured while getting node data from database")
		}
		if err := cursor.Decode(&cluster_results); err != nil {
			return err
		}

		all_values[attribute] = cluster_results["values"]

	}

	for node_id := range child_nodes {
		cluster := Cluster{}
		// assemble the cluster and send back
		for _, attribute := range in.Projection {
			cluster = GetPopulatedClusterSingleValue(all_values[attribute][node_id], attribute, cluster)
		}
		cluster.ClusterId = uint32(node_id)

		if err := stream.Send(&cluster); err != nil {
			return err
		}
	}

	return nil

}

func PipsForNeuron(stream PipStream, collection *mongo.Collection, ctx context.Context, opts *options.FindOptions, desc *PipQueryDescription, zorder int) error {
	queryString := EncodePipsQuery(desc)

	val, err := redisDb.Get(ctx, queryString).Result()

	if err == nil { // cache hit
		pips := FromGOB64[[]Pip](val)
		for _, pip := range pips {
			// since we only saved pips inside redis with timestep,value and id,
			// we must also populate the other fields:
			pip.Attribute = desc.Attribute
			pip.Simulation = desc.Simulation
			pip.Granularity = desc.Granularity
			pip.ZOrder = uint32(zorder)

			stream.Send(&pip)
		}
		return nil
	}
	if err != redis.Nil { // an error that is not a cache miss
		panic(err)
	}

	var values [10_000]float64

	if len(desc.Ids) == 1 {
		cursor, err := collection.Find(ctx,
			bson.D{{"id", desc.Ids[0]}, {"property", desc.Attribute}},
			opts,
		)
		if err != nil {
			return err
		}
		defer cursor.Close(ctx)
		cursor.Next(ctx)
		result := DatabaseResult{}
		err = cursor.Decode(&result)
		if err != nil {
			return err
		}
		if cursor.Next(ctx) {
			return fmt.Errorf("more than one result")
		}

		values = [10000]float64(result.Values)
	} else {
		node_values := make(map[uint][]float64)

		// get leaf number
		collection_name := fmt.Sprintf("level_%d", desc.Granularity)
		fmt.Printf("collection_name: %v\n", collection_name)
		cursor, err := mongoDb.Database("brain").Collection(collection_name).Find(
			ctx, bson.D{{"property", "leaves_count"}}, opts,
		)
		leaves_result := DatabaseResult{}

		if !cursor.Next(ctx) {
			return fmt.Errorf("some error occured while leaf data from database")
		}
		if err := cursor.Decode(&leaves_result); err != nil {
			return err
		}

		// get all data:
		cursor, err = collection.Find(ctx, bson.D{{"id", bson.D{{"$in", desc.Ids}}}, {"property", desc.Attribute}}, opts)
		if err != nil {
			return err
		}
		defer cursor.Close(ctx)

		var length_of_data int
		for cursor.Next(ctx) {
			res := DatabaseResult{}
			cursor.Decode(&res)
			node_values[res.Id] = res.Values
			length_of_data = len(res.Values)
		}

		// aggregate data together:
		aggregated_values, err := AggregateDataTogether(node_values, &leaves_result, desc.Aggregation, length_of_data)
		if err != nil {
			return err
		}
		values = [10_000]float64(aggregated_values)
	}

	// list of pips sorted by importance
	pips := pip.Pips(values[:], int(desc.NPips))
	// sort by timestep
	sort.Slice(pips, func(i, j int) bool {
		return pips[i].X < pips[j].X
	})

	returnPips := []Pip{}
	for _, pip := range pips {
		returnPip := &Pip{}
		returnPip.Timestep = uint32(pip.X * 100)
		returnPip.Value = float32(values[pip.X])
		if len(desc.Ids) == 1 {
			returnPip.Id = uint32(desc.Ids[0])
		}

		// append only the pip with timestep, value and id:
		returnPips = append(returnPips, *returnPip)

		// then add the other values to send back:
		returnPip.Attribute = desc.Attribute
		returnPip.Simulation = desc.Simulation
		returnPip.Granularity = uint32(desc.Granularity)
		returnPip.ZOrder = uint32(zorder)

		stream.Send(returnPip)
	}
	redisDb.Set(ctx, EncodePipsQuery(desc), ToGOB64(returnPips), 0)
	return nil
}

func (s *Server) Pips(in *PipsQuery, stream Brain_PipsServer) error {

	ctx := context.TODO()

	collection, err := GetCollection(in.Simulation, "nodes_per_id", int(in.Granularity))
	if err != nil {
		return err
	}

	if in.Granularity > 4 {
		return fmt.Errorf("granularity must less than 5")
	}

	var field string
	var mongoProjection bson.D
	if in.Granularity == 0 {
		field = in.Attribute
	} else {
		field = fmt.Sprintf("%v_%v", in.Aggregation, in.Attribute)
	}
	fmt.Printf("field: %v\n", field)

	mongoProjection = bson.D{{"_id", 0}, {"values", 1}}

	if err != nil {
		return err
	}

	number_of_docs, err := collection.EstimatedDocumentCount(ctx)
	if err != nil {
		return err
	}
	var number_of_nodes int
	if in.Granularity == 0 {
		number_of_nodes = int(number_of_docs / 16) // the number of attributes per node
	} else {
		number_of_nodes = int(number_of_docs / 36) // (16 - 3) * 3; -3 for x, y, z; *3 for aggregation
	}

	fmt.Printf("mongoProjection: %v\n", mongoProjection)
	opts := options.Find().SetProjection(mongoProjection)

	sample := make([]int, number_of_nodes)
	for i := 0; i < number_of_nodes; i++ {
		sample[i] = i
	}
	// sort sample according to z order
	zorder := zorders[in.Granularity]
	sort.Slice(sample, func(i, j int) bool {
		return zorder[sample[i]] < zorder[sample[j]]
	})
	for i := 0; i < number_of_nodes; i++ {
		node_id := sample[i]

		desc := &PipQueryDescription{[]uint32{uint32(node_id)}, field, in.Granularity, in.Simulation, in.NPips, in.Aggregation}
		err = PipsForNeuron(stream, collection, ctx, opts, desc, i)
		if err != nil {
			return err
		}
	}

	return nil
}

func (s *Server) Billboard(in *BillboardQuery, stream Brain_BillboardServer) error {
	ctx := context.TODO()

	var attribute string

	if !slices.Contains(aggregations, in.Aggregation) {
		return fmt.Errorf("%s is not a valid aggregation. Must be one of %v", in.Aggregation, aggregations)
	}
	// if len(in.ClusterIds) != 1 {
	// 	return fmt.Errorf("multiple clusters not yet implemented, try again later")
	// }

	collection, err := GetCollection(in.Simulation, "nodes_per_id", int(in.Granularity))
	if err != nil {
		return err
	}

	// var communityProperty string
	if in.Granularity > 0 {
		attribute = fmt.Sprintf("%s_%s", in.Aggregation, in.Attribute)
	} else {
		attribute = in.Attribute
	}

	projection := bson.D{{"_id", 0}, {"values", 1}, {"id", 1}}
	opts := options.Find().SetProjection(projection)
	desc := &PipQueryDescription{in.ClusterIds, attribute, in.Granularity, in.Simulation, in.NPips, in.Aggregation}
	err = PipsForNeuron(stream, collection, ctx, opts, desc, 0)
	return err
}

type SplinesReply struct {
	ClusterId int
	Timestep  int
	Splines   []struct {
		Source string
		Target string
		Weight int
		Spline string
		// Spline []struct {
		// 	X float64
		// 	Y float64
		// 	Z float64
		// }
	}
}

func (s *Server) Splines(in *SplinesQuery, stream Brain_SplinesServer) error {

	collection, err := GetCollection(in.Simulation, "splines", int(in.Granularity))
	if err != nil {
		return err
	}
	// round down to closest 10_000
	timestep := in.Timestep - (in.Timestep % 10_000)
	fmt.Printf("getting splines for timestep %d cluster %d\n", timestep, in.ClusterId)
	projection := bson.D{{"_id", 0}}
	filter := bson.D{{"timestep", timestep}, {"cluster_id", in.ClusterId}}
	opts := options.Find().SetProjection(projection)
	cursor, err := collection.Find(context.TODO(),
		filter,
		opts,
	)
	if err != nil {
		return err
	}
	defer cursor.Close(context.Background())

	for cursor.Next(context.Background()) {
		var result SplinesReply
		err := cursor.Decode(&result)
		if err != nil {
			return err
		}
		for _, spline := range result.Splines {
			splineReply := &Spline{
				FromId: spline.Source,
				ToId:   spline.Target,
				Weight: uint32(spline.Weight),
				Points: spline.Spline,
			}
			// for _, point := range spline.Spline {
			// 	splineReply.Points = append(splineReply.Points, &Point{X: float32(point.X), Y: float32(point.Y), Z: float32(point.Z)})
			// }
			stream.Send(splineReply)
		}
	}
	return nil
}
