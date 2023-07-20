package pip

import "math"

// Squared distance between a point (x, y) and a line segment defined by two points (x1, y1) and (x2, y2)
func EuclideanDist(x, y, x1, y1, x2, y2 float64) float64 {
	A := x - x1
	B := y - y1
	C := x2 - x1
	D := y2 - y1

	dot := A*C + B*D
	len_sq := C*C + D*D
	var param float64
	if len_sq != 0 {
		param = dot / len_sq
	}

	var xx, yy float64
	if param < 0 {
		xx = x1
		yy = y1
	} else if param > 1 {
		xx = x2
		yy = y2
	} else {
		xx = x1 + param*C
		yy = y1 + param*D
	}

	dx := x - xx
	dy := y - yy
	return dx*dx + dy*dy
}

// Vertical distance between a point (p1, p2) and a line befined by two points (x1, y1) and (x2, y2)
func VerticalDist(p1, p2, x1, y1, x2, y2 float64) float64 {
	return math.Abs((y1 + (y2-y1)*(p1-x1)/(x2-x1)) - p2)
}
