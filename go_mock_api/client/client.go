package main

import (
	"log"
	"net/http"
	"time"
)

func main() {
	data := make([]byte, 10628807)
	start := time.Now()
	result, err := http.Get("http://localhost:8080/stratum?timestep=10000")
	if err != nil {
		log.Fatal(err)
	}
	defer result.Body.Close()
	elapsed := time.Since(start).Milliseconds()

	n, _ := result.Body.Read(data)
	log.Println("data", string(data))
	log.Println("n", n)

	log.Printf("%v ms\n", elapsed)

	log.Println(result.StatusCode)
}
