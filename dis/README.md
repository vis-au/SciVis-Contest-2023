# Data-Intencive Systems

### Requirements:

1. Docker

### Installation:

To start the servers run `docker compose up`. This will start a Go server at port 50052 and a MongoDB server at 27017.

### Populating the database

TODO: Find a simple way of doing this.

## Installation

To get started, install the required libraries with `pipenv install`

### To preprocess the raw data:

1. `cd dis`
2. `pipenv run python ./preprocessing/extract_loadable_data.py {path_to_simulation} {destination folder}`, where `path_to_simulation` can be e.g. path to `viz-calcium`
3. Output data will be written to the destination folder (default `data/`)

### To start and populate the database:

1. Start the database `docker run -it -v mg_lib:/var/lib/memgraph -v mg_log:/var/log/memgraph -v mg_etc:/etc/memgraph -v memgraph_storage:/app --name memgraph_container -p 7687:7687 -p 7444:7444 -p 3000:3000 memgraph/memgraph-platform`
2. Copy the files to the docker container: 
```
docker cp data/neuron_positions.csv {container id}:/app/
docker cp data/neuron_properties_step_0.csv {container id}:/app/
docker cp data/edges_step_0.csv {container id}:/app/
```
3. Load the data files with `pipenv run python server/load_data`

### To start the server, follow these steps:

1. Navigate to the `server` directory
2. Compile the grpc files `./compile.sh`
3. Start the server `pipenv run python server.py`

Voilà!

You can test the server with a dummy client by running `pipenv run python dummy_client.py`

If you're running the server through ssh, it might be useful to forward the port used by memgraph. You can do this with the command: `ssh -l {USERNAME} {HOST} -N -f -L 3000:{HOST}:3000`

