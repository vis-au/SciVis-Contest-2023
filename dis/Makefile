SHELL:=/bin/bash

PORT=3000


tunnel:
	ssh -l $(user) localhost -N -f -L $(PORT):localhost:3000

list_tunnels:
	ps aux | grep -E "ssh -l ([^\s]+) localhost -N -f -L [0-9]+:localhost:3000" | awk '{$$1=$$3=$$4=$$5=$$6=$$7=$$8=$$9=$$10=""; print $$0}'

start_container:
	docker run -it -v mg_lib:/var/lib/memgraph -v mg_log:/var/log/memgraph -v mg_etc:/etc/memgraph -v memgraph_storage:/app --name memgraph_container -p 7687:7687 -p 7444:7444 -p 3000:3000 -e MEMGRAPH="--memory-limit=64000 --log-level=TRACE" memgraph/memgraph-platform

cp_data:
	docker cp data/neuron_positions.csv memgraph_container:/app/
	docker cp data/neuron_properties_step_0.csv memgraph_container:/app/
	docker cp data/edges_step_0.csv memgraph_container:/app/

proto:
	pipenv run python -m grpc_tools.protoc -I./goserver/brain/ --python_out=./server/ --pyi_out=./server/ --grpc_python_out=./server/ ./goserver/brain/brain.proto

# Automatically generate docs for proto
create_protodocs:
	stdbuf -oL \
	docker run --rm \
  	-v $(PWD)/docs:/out \
  	-v $(PWD)/goserver/brain:/protos \
  	pseudomuto/protoc-gen-doc --doc_opt=markdown,docs.md


# to remove the unneccesary table at the end
filter_protodocsdocs:
	head -n $(shell grep -n '\#\# Scalar Value Types' docs/docs.md | cut -d: -f 1 | tr -d '\n' | cat - <(echo '-1') | bc) docs/docs.md > docs/docstmp.md && mv docs/docstmp.md docs/docs.md

protodocs: create_protodocs filter_protodocsdocs

goproto:
	protoc --go_out=. --go_opt=paths=source_relative \
	--go-grpc_out=. --go-grpc_opt=paths=source_relative \
	./goserver/brain/*.proto
