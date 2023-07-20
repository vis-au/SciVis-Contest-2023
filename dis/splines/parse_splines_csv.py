import csv
import os
import os.path
import ast
from pprint import pprint
from pathlib import Path
from tqdm import tqdm

from pymongo import MongoClient, ASCENDING

# Connect to the MongoDB, change the connection string per your MongoDB environment

client = MongoClient("mongodb://localhost:27017")


simulations_path = Path(r"C:\Users\conta\Downloads\Bundling Output\Final Output Data\Bundling Precompute\Database Output Files")
simulation_name = "no_network"
splines_path = simulations_path / simulation_name

# Read in the csv file
# df = pd.read_csv('lines_out.csv', header=None)
# print(df)
# records = df.to_records(index=False).tolist()

def load_splines_for_timestep(timestep_directory):
    dir_name = os.path.basename(os.path.normpath(timestep_directory))
    timestep = int(dir_name.split("_")[1])
    total_weight = 0
    for filename in os.listdir(timestep_directory):
        filename_path = timestep_directory / filename
        # print(filename_path)
        if os.path.isfile(filename_path) and filename.endswith(".csv"):
            filename_cut = filename[:-4]
            parts = [part for part in filename_cut.split("_") if part != ""]
            cluster_id = int(parts[-1])
            granularity = int(parts[-3])

            if cluster_id == -1:
                cluster_id = 0

            # if granularity not in [4, 5]:
            #     continue

            # print(cluster_id, granularity, timestep)
            # print(filename)

            collection = client[simulation_name][f"level_{granularity}_splines"]
            weight_sum = insert_big_document(filename_path, collection, timestep, cluster_id)
            total_weight += weight_sum
    print(f"Total weight for timestep {timestep}: {total_weight}")

def insert_big_document(filename, db, timestep, cluster_id):
    with open(filename, newline='') as csvfile:
        reader = csv.reader(csvfile, delimiter=',', quotechar='"')
        records = [row for row in reader]

    parsed_records = []
    weight_sum = 0
    for record in records:
        document = {}
        document["source"] = record[0]
        document["target"] = record[1]
        document["weight"] = int(record[2])
        weight_sum += document["weight"]

        # splines = [dict(zip("xyz", ast.literal_eval(x))) for x in record[3:]]
        # document["spline"] = splines
        # changed it so that splines are strings:
        document["spline"] = ";".join(record[3:])
        parsed_records.append(document)


    big_document = {"timestep": timestep, "cluster_id": cluster_id, "splines": parsed_records}
    db.insert_one(big_document)
    return weight_sum

if __name__ == "__main__":
    for i in range(2, 6):
        collection = client[simulation_name][f"level_{i}_splines"]
        collection.drop()
    for entry in tqdm(os.listdir(splines_path)):
        timestep_directory = splines_path / entry
        if os.path.isdir(timestep_directory):
            # print(timestep_directory)
            # enter directory and load all splines for this specific timestep:
            load_splines_for_timestep(timestep_directory)
    for i in range(2, 6):
        collection = client[simulation_name][f"level_{i}_splines"]
        collection.create_index([("timestep", ASCENDING), ("cluster_id", ASCENDING)])