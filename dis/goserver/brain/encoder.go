package brain

import (
	"bytes"
	"encoding/base64"
	"encoding/gob"
	"fmt"
)

type PipQueryDescription struct {
	Ids         []uint32
	Attribute   string
	Granularity uint32
	Simulation  string
	NPips       uint32
	Aggregation string
}

func EncodePipsQuery(desc *PipQueryDescription) string {
	return fmt.Sprintf("pips,%v,%v,%v,%v,%v,%v", desc.Ids, desc.Attribute, desc.NPips, desc.Granularity, desc.Simulation, desc.Aggregation)
}

// go binary encoder
func ToGOB64(m any) string {
	b := bytes.Buffer{}
	e := gob.NewEncoder(&b)
	err := e.Encode(m)
	if err != nil {
		fmt.Println(`failed gob Encode`, err)
	}
	return base64.StdEncoding.EncodeToString(b.Bytes())
}

// go binary decoder
func FromGOB64[T any](str string) T {
	m := make([]T, 1)[0]
	by, err := base64.StdEncoding.DecodeString(str)
	if err != nil {
		fmt.Println(`failed base64 Decode`, err)
	}
	b := bytes.Buffer{}
	b.Write(by)
	d := gob.NewDecoder(&b)
	err = d.Decode(&m)
	if err != nil {
		fmt.Println(`failed gob Decode`, err)
	}
	return m
}
