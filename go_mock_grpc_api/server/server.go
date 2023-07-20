package server

import (
	"context"
	"math/rand"
)

var bigValues []float32

func Setup() {
	//create a slice of float32
	bigValues = make([]float32, 1000000)

	//populate the slice with random values
	for i := 0; i < len(bigValues); i++ {
		bigValues[i] = rand.Float32()
	}

}

type Server struct {
	UnimplementedMockServer
}

func (s *Server) Stratum(ctx context.Context, in *StratumQuery) (*Response, error) {
	println("Stratum called")
	// for _, v := range bigValues {
	// 	stream.Send(&Node{Value: float32(v)})
	// }
	return &Response{Data: bigValues}, nil
}
