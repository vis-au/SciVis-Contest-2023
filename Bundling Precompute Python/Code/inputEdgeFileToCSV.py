import csv
import misc_ReadAndWriteFile
import sys

# fileName = str(sys.argv[1])
# fileName = "rank_0_step_1000000_out_network.txt"

def inputEdgeFileToCSV(fileName):
    lines = misc_ReadAndWriteFile.read_file(fileName, "txt", "list", "DUAI")

    start_index = 0
    read_index = 0

    edges_csv_list = []
    for read_index in range(0, len(lines)):
        line = lines[read_index]
        split_line = []
        tab_split = line.split("\t")
        for split in tab_split:
            space_split = split.split(" ")
            split_line  += space_split
        if "#" in split_line:
            start_index = read_index
        if len(split_line)==5:
            edges_csv_list.append(split_line)

    edges_csv_list = [["Target_Rank","Target_ID","Source_Rank","Source_ID","Weight"]] + edges_csv_list[start_index+1:]

    # misc_ReadAndWriteFile.write_file(edges_csv_list, "edges_csv_list.csv", "list", "csv", "DGBCH")

    return (edges_csv_list)
# fileName = "rank_0_step_1000000_out_network.txt"
# inputEdgeFileToCSV(fileName)