import pandas as pd
import numpy as np
from pathlib import Path
from zipfile import ZipFile
from tqdm import tqdm
import argparse
import os

from typing import Dict, Tuple
import json


class Extractor:
    """
    `self.simulation_path` it should be the path to a root folder of a simulation
    Writes the data into `self.destination_path`
    """

    ENCODING = "utf-8"
    MONITORS_COLUMNS = [
        "neuron_id",
        "step",
        "fired",
        "fired_fraction",
        "activity",
        "dampening",
        "current_calcium",
        "target_calcium",
        "synaptic_input",
        "background_input",
        "grown_axons",
        "connected_axons",
        "grown_dendrites",
        "connected_dendrites",
    ]

    def __init__(self, simulation_path, destination_path):
        self.simulation_path = simulation_path
        self.destination_path = destination_path

        if not os.path.exists(self.destination_path):
            # Create a new directory because it does not exist
            os.mkdir(self.destination_path)

    def extract_neuron_properties(self):
        """writes a csv with the neuron properties
        For now it only returns the properties for neuron id=0 for all timesteps"""
        monitors_zip_path = Path(f"{self.simulation_path}/monitors.zip")
        if not monitors_zip_path.is_file():
            raise FileNotFoundError("Could not find a monitors.zip file")

        print("Extracting neuron properties for step 0...")
        result_lines = []

        with ZipFile(monitors_zip_path) as monitors_folder:
            number_of_neurons = len(monitors_folder.namelist())
            for neuron_id in tqdm(range(0, number_of_neurons)):
                neuron_file = f"0_{neuron_id}.csv"

                with monitors_folder.open(neuron_file, mode="r") as csv_file:
                    first_row = csv_file.readline().decode(Extractor.ENCODING).strip()
                    result_lines.append(f"{neuron_id};{first_row}")

        with open(
            f"{self.destination_path}/neuron_properties_step_0.csv", "w"
        ) as output_file:
            output_file.write(";".join(Extractor.MONITORS_COLUMNS) + "\n")
            output_file.write("\n".join(result_lines))

    @staticmethod
    def get_area_id(area_string):
        """
        `area_string` is of the form `area_32`
        The function returns only the id, in this case 32
        """
        underscore_index = area_string.find("_")
        return int(area_string[underscore_index + 1 :])

    def extract_neuron_positions(self):
        """
        writes a csv with the neuron positions
        """
        positions_file = Path(f"{self.simulation_path}/positions/rank_0_positions.txt")

        if not positions_file.is_file():
            raise FileNotFoundError(
                f"Could not find a rank_0_positions.txt file in {self.simulation_path}/positions/"
            )

        print("Extracting neuron positions...")

        positions_df = pd.read_csv(
            positions_file,
            skiprows=8,
            sep=" ",
            header=None,
            names=["id", "x", "y", "z", "area_id", "type"],
            dtype={
                "id": np.int32,
                "x": np.float64,
                "y": np.float64,
                "z": np.float64,
                "area_id": str,
                "type": str,
            },
        )

        positions_df.id = positions_df.id - 1
        positions_df = positions_df.drop(labels=["type"], axis=1)
        positions_df.area_id = positions_df.area_id.apply(self.get_area_id)

        positions_df.to_csv(
            f"{self.destination_path}/neuron_positions.csv", sep=",", index=False
        )

    def extract_some_neuron_edges(self):
        """
        writes a csv with the neuron edges (now only for the initial timestep)
        """
        timestep = 0

        first_step_edges_file = Path(
            f"{self.simulation_path}/network/rank_0_step_{timestep}_in_network.txt"
        )

        if not first_step_edges_file.is_file():
            raise FileNotFoundError(
                f"Could not find a rank_0_step_{timestep}_in_network.txt file in {self.simulation_path}/network"
            )

        print(f"Extracting neuron edges for step {timestep}...")

        edges_df = pd.read_csv(
            first_step_edges_file,
            skiprows=5,
            sep="\s+",
            header=None,
            names=["target_rank", "target_id", "source_rank", "source_id", "weight"],
            dtype=np.int32,
        )

        edges_df = edges_df.drop(labels=["target_rank", "source_rank"], axis=1)
        edges_df.target_id = edges_df.target_id - 1
        edges_df.source_id = edges_df.source_id - 1

        edges_df.to_csv(
            f"{self.destination_path}/edges_step_{timestep}.csv", sep=",", index=False
        )

    def extract_all_neuron_edges(self):
        """
        writes a big csv with the neuron edges for all the timesteps
        """
        timestep = 0
        all_edges = pd.DataFrame(dtype=pd.UInt32Dtype)

        for timestep in tqdm(range(0, 1_000_000 + 1, 10_000)):
        # for timestep in tqdm(range(0, 50001, 10_000)):

            step_edges_file = Path(
                f"{self.simulation_path}/network/rank_0_step_{timestep}_in_network.txt"
            )

            if not step_edges_file.is_file():
                raise FileNotFoundError(
                    f"Could not find a rank_0_step_{timestep}_in_network.txt file in {self.simulation_path}/network"
                )

            edges_df = pd.read_csv(
                step_edges_file,
                skiprows=5,
                sep="\s+",
                header=None,
                names=[
                    "target_rank",
                    "target_id",
                    "source_rank",
                    "source_id",
                    "weight",
                ],
                dtype=np.uint32,
            )

            edges_df = edges_df.drop(labels=["target_rank", "source_rank"], axis=1)
            edges_df.target_id = edges_df.target_id - 1
            edges_df.source_id = edges_df.source_id - 1
            edges_df["step"] = timestep
            all_edges = pd.concat([all_edges, edges_df])

        pairing = lambda x, y: (y*y + x) * (y > x) + (x*x + x + y) * (y <= x)
        all_edges.rename(columns={"source_id": "from_id", "target_id": "to_id"}, inplace=True)
        all_edges['edge_id'] = pairing(all_edges["from_id"], all_edges["to_id"])
        all_edges.to_csv(f"{self.destination_path}/no_network_edges_all.csv", sep=",", index=False)

    def extract_all_neuron_edges_smart(self):
        """
        writes a big csv with the neuron edges for all the timesteps in a smart way
        This creates a csv with a separate w column for each timestep (100 properties)
        """
        TIMESTEPS = 101
        current_timestep = 0
        all_edges = pd.DataFrame()

        edge_dict: dict[tuple, list] = {}
        edge_to_id_dict: dict[(tuple), int] = {}
        next_id = 0

        for current_timestep in tqdm(range(0, TIMESTEPS)):

            actual_timestep = current_timestep * 10_000

            step_edges_file = Path(
                f"{self.simulation_path}/network/rank_0_step_{actual_timestep}_in_network.txt"
            )

            if not step_edges_file.is_file():
                raise FileNotFoundError(
                    f"Could not find a rank_0_step_{actual_timestep}_in_network.txt file in {self.simulation_path}/network"
                )

            edges_df = pd.read_csv(
                step_edges_file,
                skiprows=5,
                sep="\s+",
                header=None,
                names=[
                    "target_rank",
                    "target_id",
                    "source_rank",
                    "source_id",
                    "weight",
                ],
                dtype=np.int32,
            )

            edges_df = edges_df.drop(labels=["target_rank", "source_rank"], axis=1)
            edges_df.target_id = edges_df.target_id - 1
            edges_df.source_id = edges_df.source_id - 1

            for row in edges_df.itertuples():
                if edge_dict.get((row.source_id, row.target_id)) is None:
                    weight_timeline = [0 for _ in range(TIMESTEPS)]
                    weight_timeline[current_timestep] = row.weight

                    edge_dict[(row.source_id, row.target_id)] = weight_timeline
                    edge_to_id_dict[(row.source_id, row.target_id)] = next_id
                    next_id += 1
                else:
                    edge_dict[(row.source_id, row.target_id)][
                        current_timestep
                    ] = row.weight

        with open(
            f"{self.destination_path}/edges_100_properties.csv", "w"
        ) as edge_file:
            w_string = "".join(f",w{i}" for i in range(TIMESTEPS))
            edge_file.write("id,source_id,target_id" + w_string + "\n")
            for id, ((source_id, target_id), step_list) in enumerate(edge_dict.items()):
                w_string_populated = ",".join(map(str, step_list))
                edge_file.write(f"{id},{source_id},{target_id},{w_string_populated}\n")

    def extract_all_edges_batched(self, batches=10):
        """
        writes a big csv with unique synapses and `batches` jsons with the timesteps
        """

        TIMESTEPS = 101
        current_timestep = 0
        # all_edges = pd.DataFrame()

        edge_dict: dict[tuple, list] = {}
        edge_to_id_dict: dict[(tuple), int] = {}
        next_id = 0

        for current_timestep in tqdm(range(0, TIMESTEPS)):

            actual_timestep = current_timestep * 10_000

            step_edges_file = Path(
                f"{self.simulation_path}/network/rank_0_step_{actual_timestep}_in_network.txt"
            )

            if not step_edges_file.is_file():
                raise FileNotFoundError(
                    f"Could not find a rank_0_step_{actual_timestep}_in_network.txt file in {self.simulation_path}/network"
                )

            edges_df = pd.read_csv(
                step_edges_file,
                skiprows=5,
                sep="\s+",
                header=None,
                names=[
                    "target_rank",
                    "target_id",
                    "source_rank",
                    "source_id",
                    "weight",
                ],
                dtype=np.int32,
            )

            edges_df = edges_df.drop(labels=["target_rank", "source_rank"], axis=1)
            edges_df.target_id = edges_df.target_id - 1
            edges_df.source_id = edges_df.source_id - 1

            for row in edges_df.itertuples():
                if edge_dict.get((row.source_id, row.target_id)) is None:
                    weight_timeline = [0 for _ in range(TIMESTEPS)]
                    weight_timeline[current_timestep] = row.weight

                    edge_dict[(row.source_id, row.target_id)] = weight_timeline
                    edge_to_id_dict[(row.source_id, row.target_id)] = next_id
                    next_id += 1
                else:
                    edge_dict[(row.source_id, row.target_id)][
                        current_timestep
                    ] = row.weight

        print("length of edge_to_id_dict:", len(list(edge_to_id_dict.keys())))
        print("length of edge_dict:", len(list(edge_dict.keys())))

        print("a taste of the edges:", list(edge_dict.items())[:5])

        unique_edges = len(list(edge_dict.keys()))
        one_part = unique_edges // batches
        id_to_step_mappings = [[] for _ in range(batches)]

        print("Saving unique edges to csv...")
        with open(
            f"{self.destination_path}/id_to_edge_mapping.csv", "w"
        ) as mapping_destination:
            mapping_destination.write("synapse_id,source_id,target_id\n")
            for id, ((source_id, target_id), steps) in enumerate(edge_dict.items()):
                mapping_destination.write(f"{id},{source_id},{target_id}\n")

                mapping_index = max(min(id // one_part, batches - 1), 0)
                id_to_step_mappings[mapping_index].append(
                    {
                        "id": id,
                        "steps": steps,
                    }
                )

        for mapping_index in range(batches):
            print(f"Saving batch {mapping_index}...")
            with open(
                f"{self.destination_path}/all_edges_part{mapping_index}.json", "w"
            ) as json_destination:
                json.dump(id_to_step_mappings[mapping_index], json_destination)

    def append_edges_to_cypherl(self):
        """
        Appends to the cypherl file edge creation queries
        """
        print("Generating edge creation queries...")

        TIMESTEPS = 101
        current_timestep = 0
        edge_dict: dict[tuple, list] = {}
        edge_to_id_dict: dict[(tuple), int] = {}
        next_id = 0

        for current_timestep in tqdm(range(0, TIMESTEPS)):

            actual_timestep = current_timestep * 10_000

            step_edges_file = Path(
                f"{self.simulation_path}/network/rank_0_step_{actual_timestep}_in_network.txt"
            )

            if not step_edges_file.is_file():
                raise FileNotFoundError(
                    f"Could not find a rank_0_step_{actual_timestep}_in_network.txt file in {self.simulation_path}/network"
                )

            edges_df = pd.read_csv(
                step_edges_file,
                skiprows=5,
                sep="\s+",
                header=None,
                names=[
                    "target_rank",
                    "target_id",
                    "source_rank",
                    "source_id",
                    "weight",
                ],
                dtype=np.int32,
            )

            edges_df = edges_df.drop(labels=["target_rank", "source_rank"], axis=1)
            edges_df.target_id = edges_df.target_id - 1
            edges_df.source_id = edges_df.source_id - 1

            for row in edges_df.itertuples():
                if edge_dict.get((row.source_id, row.target_id)) is None:
                    weight_timeline = [0 for _ in range(TIMESTEPS)]
                    weight_timeline[current_timestep] = row.weight

                    edge_dict[(row.source_id, row.target_id)] = weight_timeline
                    edge_to_id_dict[(row.source_id, row.target_id)] = next_id
                    next_id += 1
                else:
                    edge_dict[(row.source_id, row.target_id)][
                        current_timestep
                    ] = row.weight

        # print("edge to id dict:", len(list(edge_to_id_dict.keys())))
        # print("edge_dict:", len(list(edge_dict.keys())))
        with open(
            f"{self.destination_path}/import_data.cypherl", "a"
        ) as cypher_destination:
            for edge, step_list in edge_dict.items():
                source_id = edge[0]
                target_id = edge[1]
                synapse_id = edge_to_id_dict.get(edge)
                cypher_destination.write(
                    f"""MATCH (source_neuron:Neuron), (target_neuron:Neuron) WHERE source_neuron.id = {source_id} AND target_neuron.id = {target_id} CREATE (source_neuron)-[r:SYNAPSE {{id: {synapse_id}, steps: {step_list}}}]->(target_neuron);\n"""
                )

    def add_neuron_positions_to_cypherl(self):
        """Writes to the cypherl file edge creation queries"""

        positions_file = Path(f"{self.simulation_path}/positions/rank_0_positions.txt")

        if not positions_file.is_file():
            raise FileNotFoundError(
                f"Could not find a rank_0_positions.txt file in {self.simulation_path}/positions/"
            )

        print("Generating neuron creation queries...")

        positions_df = pd.read_csv(
            positions_file,
            skiprows=8,
            sep=" ",
            header=None,
            names=["id", "x", "y", "z", "area_id", "type"],
            dtype={
                "id": np.int32,
                "x": np.float64,
                "y": np.float64,
                "z": np.float64,
                "area_id": str,
                "type": str,
            },
        )

        positions_df.id = positions_df.id - 1
        positions_df = positions_df.drop(labels=["type"], axis=1)
        positions_df.area_id = positions_df.area_id.apply(self.get_area_id)

        with open(
            f"{self.destination_path}/import_data.cypherl", "w"
        ) as cypher_destination:
            for row in positions_df.itertuples():
                # row tuple has form (index, id, x, y, z, area_id)
                id = row[1]
                x = row[2]
                y = row[3]
                z = row[4]
                area_id = row[5]

                cypher_destination.write(
                    f"""CREATE (n:Neuron {{id: ToInteger({id}), x: ToFloat({x}), y: ToFloat({y}), z: ToFloat({z}), area_id: ToInteger({area_id})}});\n"""
                )

            cypher_destination.write("CREATE INDEX ON :Neuron(id);\n")

    def create_cypherl_file(self):
        """Creates a cypherl file to import all the data"""

        # first add neuron creation queries
        self.add_neuron_positions_to_cypherl()

        # add edge creation queries
        self.append_edges_to_cypherl()

    @staticmethod
    def minMax(x):
        """Helper function for `get_min_max`"""
        return pd.Series(index=["min", "max"], data=[x.min(), x.max()])

    def get_min_max(self):
        """
        Extracts min and max for all fields in monitors.
        Expects the simualtion path to have an unzipped monitors folder.
        """
        NUMBER_OF_NEURONS = 50_000

        simulation_name = str(Path(self.simulation_path.strip("/")).name)
        all_fields = [
            column
            for column in self.MONITORS_COLUMNS
            if column not in ("neuron_id", "step")
        ]

        global_extremes = {f"max_{field}": 0 for field in all_fields}
        global_extremes.update({f"min_{field}": 0 for field in all_fields})

        monitors_path = Path(f"{self.simulation_path}/monitors")
        if not monitors_path.is_dir():
            raise FileNotFoundError(
                f"Could not find a monitors folder in {monitors_path}"
            )

        for neuron_id in tqdm(range(0, NUMBER_OF_NEURONS)):
            neuron_file = monitors_path / f"0_{neuron_id}.csv"

            neuron_df = pd.read_csv(neuron_file, sep="\t")
            extremes = neuron_df.apply(self.minMax)

            if neuron_id == 0:
                for field in all_fields:
                    global_extremes[f"min_{field}"] = extremes[field]["min"]
                    global_extremes[f"max_{field}"] = extremes[field]["max"]
            else:
                for field in all_fields:
                    global_extremes[f"min_{field}"] = min(
                        global_extremes[f"min_{field}"], extremes[field]["min"]
                    )
                    global_extremes[f"max_{field}"] = max(
                        global_extremes[f"max_{field}"], extremes[field]["max"]
                    )

        values_df = pd.DataFrame(global_extremes, index=[0])
        values_df.to_csv(
            f"{self.destination_path}/{simulation_name}_min_max.csv", index=False
        )
