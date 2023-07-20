from gqlalchemy import Memgraph
from tqdm import tqdm
import pandas as pd
from collections import Counter
from pprint import pprint

from scipy.optimize import linear_sum_assignment
import numpy as np

# Make a connection to the database

steps = 101

# df.c1 += 2
# df.c2 += 3

def amalgamate(df, start=1, end=101):
    new_df = df.copy()
    for step in range(start, end):
        # print("step", step)
        curr = new_df[f"c{step}"]

        unique_coms = sorted(set(curr))
        prev_unique = sorted(set(new_df[f"c{step - 1}"]))
        # print(unique_coms)
        # print(prev_unique)
        cost_matrix = np.zeros((len(unique_coms), len(prev_unique)))

        for i in range(len(unique_coms)):
            community = unique_coms[i]
            # print("community", community)
            prev = new_df[curr == community][f"c{step - 1}"]

            com_counts = prev.value_counts()
            # print(com_counts.to_dict())
            for (k, v) in com_counts.to_dict().items():
                other_community_idx = prev_unique.index(k)
                cost_matrix[i, other_community_idx] = v

        assignement = linear_sum_assignment(cost_matrix, maximize=True)
        mapping = {}


        for to_idx, from_idx in zip(*assignement):
            to = unique_coms[to_idx]
            from_ = prev_unique[from_idx]
            mapping[to] = from_
        # print(mapping)
        new_df[f"c{step}"] = new_df[f"c{step}"].map(lambda x: mapping.get(x, x))

    return new_df


# pprint(mappings)

if __name__ == "__main__":
    memgraph = Memgraph(host="localhost", port=7687)
    nodes_query = f"""MATCH (n:Neuron)
        WITH n, rand() as r
        RETURN n.id as id, {", ".join([f"n.community_id_step{i} as c{i}" for i in range(101)])}
        ORDER BY r
        LIMIT 100"""
    nodes = list(memgraph.execute_and_fetch(nodes_query))
    df = pd.DataFrame.from_dict(nodes)
    newdf = amalgamate(df)
    for i in range(1, 101):
        print(sum(df[f"c{i-1}"] != df[f"c{i}"]), sum(newdf[f"c{i-1}"] != newdf[f"c{i}"]))

