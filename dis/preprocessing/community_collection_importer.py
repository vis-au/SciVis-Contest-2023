import pandas as pd
from tqdm import tqdm
from pymongo import MongoClient
from pathlib import Path

MONITORS_COLUMNS = [
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


def get_mongo_collection(mongo_db, database_name, collection_name):
    # mongo_db = MongoClient("mongodb://localhost:27017")
    return mongo_db[database_name][collection_name]


mongo_db = MongoClient("mongodb://localhost:27017")

community_collections_path = Path("./data/community_collections")
community_collections_path.mkdir(parents=True, exist_ok=True)

df = pd.read_csv(
    community_collections_path / f"calcium_level_0_nodes.csv",
).set_index("id")

collection = "calcium"
for step in tqdm(range(0, 1_000_000, 100)):
    cursor = get_mongo_collection(mongo_db, collection, "level_0_nodes")
    empty_level_nodes = [None] * 5

    current_nodes = pd.DataFrame(
        cursor.find(
            {"step": step}, {"_id": 0, **{field: 1 for field in MONITORS_COLUMNS}}
        )
    )
    empty_level_nodes[1] = (
        pd.DataFrame(
            get_mongo_collection(mongo_db, collection, "level_1_nodes").find(
                {}, {"_id": 0}
            )
        )
        .set_index("id")
        .sort_index()
    )
    empty_level_nodes[2] = (
        pd.DataFrame(
            get_mongo_collection(mongo_db, collection, "level_2_nodes").find(
                {}, {"_id": 0}
            )
        )
        .set_index("id")
        .sort_index()
    )
    empty_level_nodes[3] = (
        pd.DataFrame(
            get_mongo_collection(mongo_db, collection, "level_3_nodes").find(
                {}, {"_id": 0}
            )
        )
        .set_index("id")
        .sort_index()
    )
    empty_level_nodes[4] = (
        pd.DataFrame(
            get_mongo_collection(mongo_db, collection, "level_4_nodes").find(
                {}, {"_id": 0}
            )
        )
        .set_index("id")
        .sort_index()
    )

    current_nodes_with_communities = df.join(current_nodes)

    for level in range(1, 5):
        output_dir = community_collections_path / "new" / f"level{level}"
        output_dir.mkdir(parents=True, exist_ok=True)
        grouped = current_nodes_with_communities.groupby(f"community_level{level}")
        empty_level_nodes[level]["step"] = step
        for property in MONITORS_COLUMNS:
            # get min max avg for all properties:
            # print(f"doing {property}....")
            empty_level_nodes[level] = (
                empty_level_nodes[level]
                .join(grouped[property].mean())
                .rename({property: f"avg_{property}"}, axis=1)
            )
            empty_level_nodes[level] = (
                empty_level_nodes[level]
                .join(grouped[property].min())
                .rename({property: f"min_{property}"}, axis=1)
            )
            empty_level_nodes[level] = (
                empty_level_nodes[level]
                .join(grouped[property].max())
                .rename({property: f"max_{property}"}, axis=1)
            )

        get_mongo_collection(
            mongo_db, collection, f"level_{level}_nodes_aggregated"
        ).insert_many(empty_level_nodes[level].reset_index().to_dict("records"))
