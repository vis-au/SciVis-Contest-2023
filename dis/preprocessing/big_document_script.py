from tqdm import tqdm
from pymongo import MongoClient


def get_mongo_collection(mongo_db, database_name, collection_name):
    # mongo_db = MongoClient("mongodb://localhost:27017")
    return mongo_db[database_name][collection_name]


mongo_db = MongoClient("mongodb://localhost:27017")

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
]

# will run tonight:

read_from_collection = get_mongo_collection(mongo_db, "calcium", "level_0_nodes")
write_to_collection = get_mongo_collection(mongo_db, "test", "test")

for current_step in tqdm(range(0, 1_000_000, 100)):
    results = list(
        read_from_collection.find(
            {"step": current_step},
            {
                "_id": 0,
                "neuron_id": 1,
                **{current_property: 1 for current_property in PROPERTIES},
            },
        )
    )
    results.sort(key=lambda x: x["neuron_id"])

    big_documents = []
    for current_property in PROPERTIES:
        big_documents.append(
            {
                "step": current_step,
                "property": current_property,
                "values": list(map(lambda x: x[current_property], results)),
            }
        )
    insertion_res = write_to_collection.insert_many(big_documents)
