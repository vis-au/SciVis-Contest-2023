package pipsBenchmark

import (
	"brain-go-grpc/pip"
	"bufio"
	"fmt"
	"math"
	"math/rand"
	"os"
	"strconv"
	"time"
)

func Benchmark(sample []float64) {
	resultfile, err := os.Create("result_pq.txt")
	if err != nil {
		panic(err)
	}
	defer resultfile.Close()

	for i := 1; i < len(sample); i += len(sample) / 20 {
		P := sample[:i]
		t1 := time.Now()
		pips := pip.Pips(P[:], i)
		delta := time.Since(t1)
		fmt.Fprintf(resultfile, "%v,%v\n", i, delta.Milliseconds())
		fmt.Printf("Pips took %v\n", delta)
		fmt.Printf("got %v pips\n", len(pips))
	}

}

func Testpip(sampleMethod string) {
	sample := [100_000]float64{}
	r := rand.New(rand.NewSource(0))
	seed := time.Now().UnixNano()
	fmt.Printf("seed: %v\n", seed)
	for i := 0; i < len(sample); i++ {
		if sampleMethod == "sin" {
			j := float64(i)*0.006 + 50
			v := 0.0
			P := rand.New(rand.NewSource(seed))
			for k := 0; k < 100; k++ {
				v += math.Cos(P.Float64()*10 + (j+0.5)/(1+P.Float64()))
			}
			sample[i] = v
		} else if sampleMethod == "random" {
			sample[i] = r.NormFloat64() * 10
		}
	}
	if sampleMethod == "file" || sampleMethod == "benchmark" {
		fmt.Printf("reading data.txt\n")
		file, err := os.Open("data.txt")
		if err != nil {
			panic(err)
		}
		defer file.Close()

		reader := bufio.NewReader(file)
		scanner := bufio.NewScanner(reader)
		scanner.Split(bufio.ScanWords)
		for i := 0; scanner.Scan() && i < len(sample); i++ {
			x, err := strconv.ParseFloat(scanner.Text(), 64)
			if err != nil {
				panic(err)
			}
			sample[i] = x
		}
	}
	if sampleMethod == "benchmark" {
		Benchmark(sample[:])
		return
	}
	P := sample[:]
	// P := []float64{1, 1, 100, 101, 100}
	t1 := time.Now()

	// get os.args[2] as an int
	n, _ := strconv.Atoi(os.Args[3])

	pips := pip.Pips(P[:], n)
	delta := time.Since(t1)
	fmt.Printf("Pips took %v\n", delta)
	fmt.Printf("got %v pips\n", len(pips))
	dataf, _ := os.Create("data.csv")
	defer dataf.Close()
	for i, v := range P {
		fmt.Fprintf(dataf, "%v,%v\n", i, v)
	}
	pipsf, _ := os.Create("pips.csv")
	defer pipsf.Close()
	for _, p := range pips {
		fmt.Fprintf(pipsf, "%v,%v\n", p.X, p.Y)
	}
}
