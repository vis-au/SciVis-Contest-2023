package main

import (
	"encoding/json"
	"fmt"
	"math/rand"
	"net/http"

	"github.com/gorilla/mux"
)

var bigValues []float32

func setup() {
	//create a slice of float32
	bigValues = make([]float32, 1000000)

	//populate the slice with random values
	for i := 0; i < len(bigValues); i++ {
		bigValues[i] = rand.Float32()
	}
}

func main() {

	setup()

	//create a new router
	router := mux.NewRouter()

	//specify endpoints, handler functions and HTTP method
	router.HandleFunc("/stratum", stratum).Methods("GET")

	http.Handle("/", router)
	fmt.Printf("Server is running on port 8080\n")

	//start and listen to requests
	http.ListenAndServe(":8080", router)

}

func stratum(w http.ResponseWriter, r *http.Request) {
	w.Header().Set("Content-Type", "application/json")

	//specify HTTP status code
	w.WriteHeader(http.StatusOK)

	jsonResponse, err := json.Marshal(bigValues)
	if err != nil {
		return
	}

	//update response
	w.Write(jsonResponse)
}
