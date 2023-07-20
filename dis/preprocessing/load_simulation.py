from pathlib import Path
import pandas as pd
import numpy as np
from pymongo import MongoClient
from pymongo import ASCENDING as pymongo_ASCENDING
import argparse
from tqdm import tqdm
from io import StringIO
import time


class Loader:
    NUMBER_OF_NEURONS = 50_000

    PROPERTIES = [
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
        # "community_level1",
        # "community_level2",
        # "community_level3",
        # "community_level4",
    ]

    MONITOR_COLUMNS = [
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

    def __init__(self, simulations_path, simulation_name, mongo_string):
        self.simulations_path = Path(simulations_path)
        self.simulation_name = simulation_name
        self.simulation_path = self.simulations_path / self.simulation_name
        # for folder in ("monitors", "network"):
        #     if not (self.simulation_path / folder).is_dir():
        #         raise FileNotFoundError(
        #             f"Could not find folder {folder} in {self.simulation_path}"
        #         )

        self.mongo = MongoClient(mongo_string)
        if self.mongo.admin.command("ping")["ok"] != 1.0:
            raise ConnectionError(f"Could not connect to database on {mongo_string}")
        else:
            print("Connected to Mongo!")

        df_path = Path("./coms_df.csv")
        if not df_path.is_file():
            raise FileNotFoundError("No coms_df.csv file found in the current folder.")

        self.hierarchy = self.get_hierarchy(df_path)
        print("Loaded hierarchy dataframe!")

    def get_hierarchy(self, df_path):
        full_df = pd.DataFrame({"neuron_id": range(50_000)})
        full_df["community_level1"] = full_df.index // 10
        blobbed_df = (
            pd.read_csv(df_path)
            .rename(columns={f"level_{i}": f"community_level{i}" for i in range(1, 5)})
            .set_index("id")
        )
        full_df = pd.merge(blobbed_df, full_df, on="community_level1").sort_values(
            "neuron_id"
        )
        return full_df

    def load_neuron_properties_mongo_per_step_lowest_level(self, batchsize=500):
        """
        Creates the 0 level per step collections
        """

        monitors_path = self.simulation_path / "monitors"
        node_collection = self.mongo[self.simulation_name]["level_0_nodes_per_step"]

        print(f"Using batchsize {batchsize}...")

        all_timesteps = list(range(10_000))
        for i in tqdm(range(0, len(all_timesteps), batchsize)):
            myio = StringIO()
            batch_timesteps = all_timesteps[i : i + batchsize]
            for neuron_id in range(50_000):
                with open(monitors_path / f"0_{neuron_id}.csv") as monitor_file:
                    lines = monitor_file.readlines()
                    for batched_timestep in batch_timesteps:
                        myio.write(
                            f"{neuron_id};{batched_timestep*100};{lines[batched_timestep]}"
                        )

            myio.seek(0)
            big_df = pd.read_csv(
                myio,
                sep=";",
                names=["neuron_id", "correct_timestep", *self.MONITOR_COLUMNS],
            )
            # the old timestep in the csv was off, we added the right one in the call to `myio.write`
            # so we delete the old one and rename the right one to be the only one:
            del big_df["step"]
            big_df.rename(columns={"correct_timestep": "step"}, inplace=True)
            for truncated_timestep in batch_timesteps:
                real_timestep = truncated_timestep * 100
                step_df = big_df[big_df["step"] == real_timestep]

                documents = []
                for property_name in self.PROPERTIES:
                    if property_name.startswith("community_"):
                        values_list = self.hierarchy[property_name].to_list()
                        documents.append(
                            {
                                "property": property_name,
                                "step": real_timestep,
                                "values": values_list,
                            }
                        )
                    else:
                        values_list = step_df[property_name].to_list()
                        documents.append(
                            {
                                "property": property_name,
                                "step": real_timestep,
                                "values": values_list,
                            }
                        )
                node_collection.insert_many(documents)

        print(f"Loaded {self.simulation_name}/level_0_nodes_per_step !")

        node_collection.create_index([("step", pymongo_ASCENDING)])
        print(f"Created index on {self.simulation_name}/level_0_nodes_per_step")

    def load_neuron_properties_mongo_per_id_lowest_level(self):
        """
        Creates the 0 level per neuron collections
        """

        monitors_path = self.simulation_path / "monitors"
        node_collection = self.mongo[self.simulation_name]["level_0_nodes_per_id"]

        for neuron_id in tqdm(range(0, self.NUMBER_OF_NEURONS)):
            neuron_file = monitors_path / f"0_{neuron_id}.csv"

            df = pd.read_csv(neuron_file, sep=";", names=self.MONITOR_COLUMNS)
            df.step = df.index * 100
            df["neuron_id"] = neuron_id
            df.sort_values("step", inplace=True)
            # add communities for this neuron:
            df["community_level1"] = int(
                self.hierarchy[self.hierarchy["neuron_id"] == neuron_id][
                    "community_level1"
                ].iloc[0]
            )
            df["community_level2"] = int(
                self.hierarchy[self.hierarchy["neuron_id"] == neuron_id][
                    "community_level2"
                ].iloc[0]
            )
            df["community_level3"] = int(
                self.hierarchy[self.hierarchy["neuron_id"] == neuron_id][
                    "community_level3"
                ].iloc[0]
            )
            df["community_level4"] = int(
                self.hierarchy[self.hierarchy["neuron_id"] == neuron_id][
                    "community_level4"
                ].iloc[0]
            )

            # this loop inserts documents for the `per neuron` collection
            documents = []
            for property in self.PROPERTIES:
                values_list = df[property].to_list()
                documents.append(
                    {
                        "id": neuron_id,
                        "property": property,
                        "values": values_list,
                    }
                )

            node_collection.insert_many(documents)

        print(f"Loaded {self.simulation_name}/level_0_nodes_per_id !")

        node_collection.create_index([("id", pymongo_ASCENDING)])
        print(f"Created index on {self.simulation_name}/level_0_nodes_per_id")

    def load_neuron_properties_mongo_per_step_upper_levels(self):
        lowest_level_collection = self.mongo[self.simulation_name][
            "level_0_nodes_per_step"
        ]

        for step in tqdm(range(0, 1_000_000, 100)):
            res = list(lowest_level_collection.find({"step": step}))

            for found_document in res:
                property_name = found_document["property"]
                if property_name not in self.PROPERTIES:
                    continue
                all_values_per_step = np.array(found_document["values"])

                for current_level in range(1, 5):
                    current_level_string = f"community_level{current_level}"
                    current_level_collection = self.mongo[self.simulation_name][
                        f"level_{current_level}_nodes_per_step"
                    ]

                    max_values = []
                    min_values = []
                    avg_values = []

                    gb = self.hierarchy.groupby(current_level_string)["neuron_id"]
                    for community_id, neurons in gb:
                        values_in_current_community = all_values_per_step[
                            neurons.to_list()
                        ]
                        avg_values.append(float(np.mean(values_in_current_community)))
                        max_values.append(float(values_in_current_community.max()))
                        min_values.append(float(values_in_current_community.min()))

                    # add new values to mongo collection
                    documents = [
                        {
                            "step": step,
                            "property": f"min_{property_name}",
                            "values": min_values,
                        },
                        {
                            "step": step,
                            "property": f"max_{property_name}",
                            "values": max_values,
                        },
                        {
                            "step": step,
                            "property": f"avg_{property_name}",
                            "values": avg_values,
                        },
                    ]
                    current_level_collection.insert_many(documents)

        print(
            "Loaded ",
            ", ".join(
                [
                    f"{self.simulation_name} / level_{level}_nodes_per_step"
                    for level in range(1, 5)
                ]
            ),
        )
        for level in range(1, 5):
            self.mongo[self.simulation_name][
                f"level_{level}_nodes_per_step"
            ].create_index([("step", pymongo_ASCENDING)])
            print(
                f"Created index on {self.simulation_name} / level_{level}_nodes_per_step!"
            )

    def load_neuron_properties_mongo_per_id_upper_levels(self):
        lower_collection = self.mongo[self.simulation_name]["level_0_nodes_per_id"]
        for level in range(1, 5):
            node_collection = self.mongo[self.simulation_name][
                f"level_{level}_nodes_per_id"
            ]
            current_level = f"community_level{level}"
            unique_communities = self.hierarchy[current_level].unique()
            for community in tqdm(unique_communities):
                nodes = self.hierarchy[self.hierarchy[current_level] == community]
                for property_name in self.PROPERTIES:
                    res = list(
                        lower_collection.find(
                            {
                                "id": {"$in": nodes.neuron_id.to_list()},
                                "property": property_name,
                            }
                        )
                    )
                    all_values = np.array([res[i]["values"] for i in range(len(res))])
                    min_values = np.amin(all_values, axis=0)
                    max_values = np.amax(all_values, axis=0)
                    avg_values = np.average(all_values, axis=0)

                    documents = [
                        {
                            "id": int(community),
                            "property": f"min_{property_name}",
                            "values": min_values.tolist(),
                        },
                        {
                            "id": int(community),
                            "property": f"max_{property_name}",
                            "values": max_values.tolist(),
                        },
                        {
                            "id": int(community),
                            "property": f"avg_{property_name}",
                            "values": avg_values.tolist(),
                        },
                    ]
                    node_collection.insert_many(documents)
            print(f"Loaded {self.simulation_name} / level_{level}_nodes_per_id!")

        print("Loaded all properties per id upper levels!")

        for level in range(1, 5):
            self.mongo[self.simulation_name][
                f"level_{level}_nodes_per_id"
            ].create_index([("id", pymongo_ASCENDING)])
            print(
                f"Created index on {self.simulation_name} / level_{level}_nodes_per_id!"
            )

    # def get_communities(self):
    #     """Returns a df for each level != 0 containing children_count, leaves_count and cluster assignment for higher levels"""
    #     nodes_1 = (
    #         self.hierarchy.groupby(self.hierarchy["neuron_id"] // 10)
    #         .mean()
    #         .drop(columns=["community_level1", "neuron_id"])
    #     )
    #     nodes_1["leaves_count"] = 10
    #     nodes_1["children_count"] = 10

    #     nodes_2 = (
    #         nodes_1.groupby("community_level2").mean().rename_axis("id", axis="index")
    #     )
    #     nodes_2.index = nodes_2.index.astype(int)
    #     nodes_2["children_count"] = nodes_1.groupby("community_level2").size()
    #     nodes_2["leaves_count"] = nodes_1.groupby("community_level2")[
    #         "leaves_count"
    #     ].sum()

    #     nodes_3 = (
    #         nodes_2.groupby("community_level3").mean().rename_axis("id", axis="index")
    #     )
    #     nodes_3.index = nodes_3.index.astype(int)
    #     nodes_3["children_count"] = nodes_2.groupby("community_level3").size()
    #     nodes_3["leaves_count"] = nodes_2.groupby("community_level3")[
    #         "leaves_count"
    #     ].sum()

    #     nodes_4 = (
    #         nodes_3.groupby("community_level4").mean().rename_axis("id", axis="index")
    #     )
    #     nodes_4.index = nodes_4.index.astype(int)
    #     nodes_4["children_count"] = nodes_3.groupby("community_level4").size()
    #     nodes_4["leaves_count"] = nodes_3.groupby("community_level4")[
    #         "leaves_count"
    #     ].sum()

    #     nodes_1["step"] = 0
    #     nodes_2["step"] = 0
    #     nodes_3["step"] = 0
    #     nodes_4["step"] = 0

    #     return [nodes_1, nodes_2, nodes_3, nodes_4]

    def get_communities(self, hierarchy_df):
        """Returns a df for each level != 0 containing children_count, leaves_count and cluster assignment for higher levels"""
        nodes_1 = (
            hierarchy_df.groupby(hierarchy_df["neuron_id"] // 10)
            .mean()
            .drop(columns=["community_level1", "neuron_id"])
        )
        nodes_1["leaves_count"] = 10
        nodes_1["children_count"] = 10

        nodes_2 = (
            nodes_1.groupby("community_level2").mean().rename_axis("id", axis="index")
        )
        nodes_2.index = nodes_2.index.astype(int)
        nodes_2["children_count"] = nodes_1.groupby("community_level2").size()
        nodes_2["leaves_count"] = nodes_1.groupby("community_level2")[
            "leaves_count"
        ].sum()

        nodes_3 = (
            nodes_2.groupby("community_level3").mean().rename_axis("id", axis="index")
        )
        nodes_3.index = nodes_3.index.astype(int)
        nodes_3["children_count"] = nodes_2.groupby("community_level3").size()
        nodes_3["leaves_count"] = nodes_2.groupby("community_level3")[
            "leaves_count"
        ].sum()

        nodes_4 = (
            nodes_3.groupby("community_level4").mean().rename_axis("id", axis="index")
        )
        nodes_4.index = nodes_4.index.astype(int)
        nodes_4["children_count"] = nodes_3.groupby("community_level4").size()
        nodes_4["leaves_count"] = nodes_3.groupby("community_level4")[
            "leaves_count"
        ].sum()

        nodes_1["step"] = 0
        nodes_2["step"] = 0
        nodes_3["step"] = 0
        nodes_4["step"] = 0

        return [hierarchy_df, nodes_1, nodes_2, nodes_3, nodes_4]

    def get_edge_df(self, timestep):
        edges_file = (
            self.simulation_path / "network" / f"rank_0_step_{timestep}_in_network.txt"
        )
        edges_df = pd.read_csv(
            edges_file,
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
        edges_df = edges_df.drop(columns=["target_rank", "source_rank"])
        edges_df = edges_df.rename(
            columns={"target_id": "to_id", "source_id": "from_id"}
        )
        edges_df.to_id = edges_df.to_id - 1
        edges_df.from_id = edges_df.from_id - 1

        return edges_df

    def load_all_edges_mongo(self, batchsize=5000):
        TIMESTEPS = 101
        for current_timestep in tqdm(range(0, TIMESTEPS)):
            actual_timestep = current_timestep * 10_000

            edges_df = self.get_edge_df(actual_timestep)

            for level in range(0, 5):
                current_level_collection = self.mongo[self.simulation_name][
                    f"level_{level}_edges"
                ]
                current_community_string = f"community_level{level}"
                if level == 0:
                    current_community_string = "neuron_id"
                parent_community_string = f"community_level{level+1}"

                temp_df = edges_df
                if level > 0:
                    # replace from_id and to_id with id's of the nodes at the current level:
                    current_hierarchy = self.hierarchy[
                        [current_community_string, "neuron_id"]
                    ]
                    temp_df = pd.merge(
                        current_hierarchy,
                        edges_df,
                        left_on="neuron_id",
                        right_on="to_id",
                    )
                    temp_df = temp_df.drop(columns=["neuron_id", "to_id"]).rename(
                        columns={current_community_string: "to_id"}
                    )

                    temp_df = pd.merge(
                        current_hierarchy,
                        temp_df,
                        left_on="neuron_id",
                        right_on="from_id",
                    )
                    temp_df = temp_df.drop(columns=["neuron_id", "from_id"]).rename(
                        columns={current_community_string: "from_id"}
                    )

                    temp_df = temp_df.groupby(["from_id", "to_id"], as_index=False)[
                        "weight"
                    ].sum()

                # add parent communities
                if level < 4:
                    parent_hierarchy = self.hierarchy[
                        [parent_community_string, current_community_string]
                    ].drop_duplicates()
                    temp_df = pd.merge(
                        parent_hierarchy,
                        temp_df,
                        left_on=current_community_string,
                        right_on="to_id",
                    )
                    temp_df = temp_df.drop(columns=[current_community_string]).rename(
                        columns={parent_community_string: "to_community"}
                    )

                    temp_df = pd.merge(
                        parent_hierarchy,
                        temp_df,
                        left_on=current_community_string,
                        right_on="from_id",
                    )
                    temp_df = temp_df.drop(columns=[current_community_string]).rename(
                        columns={parent_community_string: "from_community"}
                    )

                temp_df["step"] = actual_timestep

                # insert into mongo

                documents = temp_df.to_dict("records")
                for i in range(0, len(documents), batchsize):
                    batch = documents[i : i + batchsize]
                    current_level_collection.insert_many(batch)
        print("Loaded all edges!")

        for level in range(0, 5):
            self.mongo[self.simulation_name][f"level_{level}_edges"].create_index(
                [("step", pymongo_ASCENDING)]
            )
            print(f"Created index on {self.simulation_name} / level_{level}_edges!")

    def load_hierarchy_mongo(self, batchsize=5000):
        hierarchy_collection = self.mongo["brain"]["hierarchy"]
        documents = self.hierarchy.to_dict("records")
        for i in range(0, len(documents), batchsize):
            batch = documents[i : i + batchsize]
            hierarchy_collection.insert_many(batch)

        # load properties for all neurons:
        communities = self.get_communities(self.hierarchy)
        for level in range(0, 5):
            per_step_collection = self.mongo["brain"][f"level_{level}"]
            documents_per_step = []
            for inner_level in range(level + 1, 5):
                documents_per_step.append(
                    {
                        "property": f"community_level{inner_level}",
                        "values": communities[level][
                            f"community_level{inner_level}"
                        ].to_list(),
                    }
                )
            if level > 0:
                documents_per_step.append(
                    {
                        "property": "leaves_count",
                        "values": communities[level]["leaves_count"].to_list(),
                    }
                )
                documents_per_step.append(
                    {
                        "property": "children_count",
                        "values": communities[level]["children_count"].to_list(),
                    }
                )
            else:
                documents_per_step.append(
                    {"property": "leaves_count", "values": [1 for _ in range(50_000)]}
                )

            per_step_collection.insert_many(documents_per_step)

        print("Loaded hierarchy to brain db!")

    def load_lowest_level(self):
        self.load_neuron_properties_mongo_per_id_lowest_level()
        self.load_neuron_properties_mongo_per_step_lowest_level()

    def load_upper_levels(self):
        self.load_neuron_properties_mongo_per_id_upper_levels()
        self.load_neuron_properties_mongo_per_step_upper_levels()


if __name__ == "__main__":
    argument_parser = argparse.ArgumentParser(
        prog="load_simulation",
        description="""Script takes as input a simulation folder, preprocesses the data and loads it into the specified mongo database
            Must be run from the dis folder.""",
    )

    argument_parser.add_argument("simulation_path")
    argument_parser.add_argument("simulation_name")
    argument_parser.add_argument(
        "mongo_uri", nargs="?", default="mongodb://localhost:27017"
    )
    argument_parser.add_argument("--edges", action="store_true")
    argument_parser.add_argument("--nodes", action="store_true")
    argument_parser.add_argument("--all", action="store_true")
    argument_parser.add_argument("--hierarchy", action="store_true")
    argument_parser.add_argument("--lowest", action="store_true")
    argument_parser.add_argument("--upper", action="store_true")

    args = argument_parser.parse_args()

    print(f"Looking into {args.simulation_path} for {args.simulation_name}...")
    print(f"mongo uri: {args.mongo_uri}")
    loader = Loader(args.simulation_path, args.simulation_name, args.mongo_uri)

    if not args.all and not args.edges and not args.nodes and not args.hierarchy:
        print("Choose something to do")
    else:
        if args.all:
            loader.load_lowest_level()
            loader.load_upper_levels()

            loader.load_all_edges_mongo()
        else:
            if args.lowest:
                loader.load_lowest_level()

            if args.upper:
                loader.load_upper_levels()

            if args.nodes:
                loader.load_lowest_level()
                loader.load_upper_levels()

            if args.edges:
                loader.load_all_edges_mongo()

            if args.hierarchy:
                loader.load_hierarchy_mongo()
