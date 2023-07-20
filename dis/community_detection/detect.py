from gqlalchemy import Memgraph
from tqdm import tqdm

# Make a connection to the database
memgraph = Memgraph(host="localhost", port=7687)

TIMESTEPS = 101

similarity_threshold = 0.7
exponent = 3.2
min_value = 0.01
weight_property = '"weight"'
w_selfloop = 1.0
max_iterations = 200
max_updates = 7

set_query = f"""
    CALL community_detection_online.set(
        true,
        true,
        {similarity_threshold},
        {exponent},
        {min_value},
        {weight_property},
        {w_selfloop},
        {max_iterations},
        {max_updates})
    YIELD community_id, node
    SET node.community_id_step0 = community_id;
"""

print(set_query)

print("Detecting initial communities..")

initial_weight_query = f"MATCH ()-[e]->() SET e.weight = e.w0"
memgraph.execute(initial_weight_query)
memgraph.execute(set_query)

print("Finished")

uniques_query = f"MATCH (n:Neuron) RETURN COUNT(DISTINCT n.community_id_step0) as uniques;"
uniques = next(memgraph.execute_and_fetch(uniques_query))["uniques"]
print(f"{uniques} unique communities")

print("Updating communities...")
for i in (pbar := tqdm(range(1, TIMESTEPS))):
    if i < 5:
        q = f"MATCH (n:Neuron) SET n.community_id_step{i} = n.community_id_step0"
        memgraph.execute(q)
        continue
    change_weight_query = f"MATCH ()-[e]->() SET e.weight = e.w{i}"
    memgraph.execute(change_weight_query)

    update_query = f"""
        MATCH ()-[e]->()
        WHERE e.w{i-1} != e.weight
        WITH collect(e) as changed_edges
        CALL community_detection_online.update([], [], [], changed_edges, [], [])
        YIELD node, community_id
        SET node.community_id_step{i} = community_id;
    """
    memgraph.execute(update_query)

    changes_query = f"MATCH (n) WHERE n.community_id_step{i-1} != n.community_id_step{i} RETURN count(n)"
    minus_1_query = f"MATCH (n) WHERE n.community_id_step{i} = -1 RETURN count(n)"
    uniques_query = f"MATCH (n:Neuron) RETURN COUNT(DISTINCT n.community_id_step{i}) as uniques;"

    changes = next(memgraph.execute_and_fetch(changes_query))["count(n)"]
    minus_1 = next(memgraph.execute_and_fetch(minus_1_query))["count(n)"]
    uniques = next(memgraph.execute_and_fetch(uniques_query))["uniques"]

    if minus_1 > 0:
        print(f"WARNING: {minus_1} unassigned nodes!")
    pbar.set_description(f"changes: {changes}, unique: {uniques}")
