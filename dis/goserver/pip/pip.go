package pip

import (
	"container/heap"
)

// Defines a line between two PiPs
// Begin and End are the index of the PiPs
// MaxPoint is the index of the point with the largest MaxValue between Begin and End
// MaxValue is the vertical distance between the line and MaxPoint
type Line struct {
	Begin    int
	End      int
	MaxPoint int
	MaxValue float64
}

type PiP struct {
	X int
	Y float64
}

// FindPip finds the point with the largest vertical distance between a line and the points between Begin and End
func FindPip(line *Line, P []float64) {
	MaxValue := -1.0
	MaxPoint := -1
	i := line.Begin + 1
	for i <= line.End-2 {
		d := EuclideanDist(float64(i), P[i], float64(line.Begin), P[line.Begin], float64(line.End-1), P[line.End-1])
		if d > MaxValue {
			MaxValue = d
			MaxPoint = i
		}
		i++
	}
	line.MaxPoint = MaxPoint
	line.MaxValue = MaxValue
}

// Pips finds the n most important points in a time series P
// https://ieeexplore.ieee.org/stamp/stamp.jsp?arnumber=8279589 (Splitting Algorithm)
func Pips(P []float64, n int) []PiP {
	m := len(P)

	if n > m {
		n = m
	}

	// L is the list of n most important points, sorted by importance
	L := []PiP{{0, P[0]}, {m - 1, P[m-1]}}
	FullLine := &Line{0, m, -1, -1}
	FindPip(FullLine, P)

	var MaxNode *Line

	// Priority queue sorted by MaxValue
	pq := make(PriorityQueue, 1)
	pq[0] = &Item{value: FullLine, priority: FullLine.MaxValue}
	heap.Init(&pq)

	for i := 0; i < n-2; i++ {

		MaxNode = heap.Pop(&pq).(*Item).value.(*Line)

		// Add new point
		pip := PiP{MaxNode.MaxPoint, P[MaxNode.MaxPoint]}
		L = append(L, pip)

		// Split MaxNode into 2 nodes HeadNode and TailNode
		HeadNode := &Line{MaxNode.Begin, MaxNode.MaxPoint + 1, -1, -1}
		TailNode := &Line{MaxNode.MaxPoint, MaxNode.End, -1, -1}

		// Find MaxValue and MaxPoint for HeadNode
		FindPip(HeadNode, P)

		// Find MaxValue and MaxPoint for TailNode
		FindPip(TailNode, P)

		// Push HeadNode and TailNode to pq
		heap.Push(&pq, &Item{value: HeadNode, priority: HeadNode.MaxValue})
		heap.Push(&pq, &Item{value: TailNode, priority: TailNode.MaxValue})

	}

	return L
}
