import readAndWriteFile
import inputEdgeFileToCSV
from collections import Counter
import os
import ast
import subprocess
import time
import copy
from multiprocessing import Process
import math

start_time = time.time()
iterations = 0

def printClusterAttributes(heirarchy):
    count = 0
    for key in clusterIdsAndObjectsDict.keys():
        if count <30:
            count += 1
            clusterObject = clusterIdsAndObjectsDict[key]
            thisHeirarchy = clusterObject.heirarchy
            if thisHeirarchy == heirarchy:
                print(clusterObject)
        else:
            return

def printNeuronAttributes(ids):
    for id in ids:
        neuronObject = neuronIdsAndObjectsDict[id] 
        print(neuronObject)

class Cluster():
    def __init__(self, id, heirarchy):
        self.id = (id, heirarchy)  #tuple because ids can be duplicate across heirarchy levels
        self.heirarchy = heirarchy

        self.childrenClusterObjects = []
        self.childrenClusterIds = []

        self.parentClusterObject = None
        self.parentClusterId = None

        self.edgeObjects = {}
        self.edgeIds = {}

        self.isNeuron = False
        self.isMotherCluster = False

        self.totalOutgoingEdges = 0 
        # ie, where the parent cluster is not the same. So for instance in the second heirarchy
        # this would be an edge which starts in a sub cluster of 1 and ends in a subcluster of 2
        # this is the weight of the line going from this cluster to the portal node of the parent cluster

        self.location = None
    
    def setIsNeuronTrue(self):
        self.isNeuron = True
    
    def setLocation(self, locationTuple):
        self.location = locationTuple
    
    def addChild(self, childCluster):
        self.childrenClusterObjects.append(childCluster)
        self.childrenClusterIds.append(childCluster.id)

    def setParent(self, parentCluster):
        self.parentClusterObject = parentCluster
        self.parentClusterId = parentCluster.id

    def addEdge(self, edgeTargetObject):
        targetHeirarchy = edgeTargetObject.heirarchy
        if targetHeirarchy not in self.edgeObjects.keys():
            self.edgeObjects[targetHeirarchy] = [edgeTargetObject]
            self.edgeIds[targetHeirarchy] = [edgeTargetObject.id]
        else:
            self.edgeObjects[targetHeirarchy].append(edgeTargetObject)
            self.edgeIds[targetHeirarchy].append(edgeTargetObject.id)
        parentOfEdgeTarget = edgeTargetObject.parentClusterObject
        if self.parentClusterObject != parentOfEdgeTarget:
            self.totalOutgoingEdges += 1
    
    def getEdgesInHeirarchy(self, heirarchyDelta):
        try:
            heirarchy = self.heirarchy + heirarchyDelta #so if heirarchy delta is -1, we want edges to the previous level
            edges = [edge for edge in self.edgeObjects[heirarchy]]
            return(edges)
        except KeyError:
            return []


    def __str__(self):
        print("Instance of Cluster. Printing attributes below")
        print("Id: ", self.id)
        print("Is Neuron: ", self.isNeuron)
        print("Location: ", self.location)
        print("Heirarchy: ", self.heirarchy)
        print("Parent Cluster Id: ", self.parentClusterId)
        print("Child Cluster Ids: ", self.childrenClusterIds)
        if len(self.edgeIds.keys()) >0:
            print("Subset of Edges: ", self.edgeIds[self.heirarchy][0:5])
        print("Total Edges Going Out of Parent Cluster: ", self.totalOutgoingEdges)
        return("")
    

class Neuron:
    def __init__(self, id):
        self.id = id
        self.location = None
        self.parentObjectsDict = {}
        self.parentIdsDict = {}

    def setParent(self, parentObject):
        parentId = parentObject.id
        parentHeirarchy = parentObject.heirarchy
        self.parentClusterId = parentId
        self.parentObjectsDict[parentHeirarchy] = parentObject
        self.parentIdsDict[parentHeirarchy] = parentId
    
    def setLocation(self, location):
        self.location = location

    def __str__(self):
        print("Instance of Neuron. Printing attributes below")
        print("Id: ", self.id)
        print("Location: ", self.location)
        print("Parent Cluster Id: ", self.parentIdsDict)
        return("")
##########################################################################################
#Other recursive algorithm helper functions:

# [[startCluster, endCluster, Weight, point1, point2, point3...]]
def handleBrainBundlerOutputFileForDatabase(brainBundlingOutput, folderPath, fileName, timeStep):
    start_index = 0
    end_index = 0

    for i in range(0, len(brainBundlingOutput)):
        line = brainBundlingOutput[i]
        if "POINTS" in line:
            start_index = i + 1  # index of first point
        if "LINES" in line:
            end_index = i + 1   # index of first line
            break
    
    pointsList = [["id","pos_x","pos_y","pos_z", "weight", "startCluster", "endCluster"]]
    pointInfoDict = {}
    for i in range(start_index, end_index -1):
        point = brainBundlingOutput[i].split(" ")
        x = point[0]
        y = point[1]
        z = point[2]
        weight = int((float(point[3]) * 100))  #multiplied weight by 100 because unity needs bigger weight values
        startClusterId = point[4]
        endClusterId = point[5]

        id = i - start_index  #point ids start from 0 in the bundling code
        thisPoint = [id+1, x, y, z, weight, startClusterId, endClusterId]
        pointInfoDict[id] = [startClusterId, endClusterId, weight, tuple((float(x),float(y),float(z)))]
        pointsList.append(thisPoint)
    
    linesList = []
    for i in range(end_index, len(brainBundlingOutput)):
        line = brainBundlingOutput[i].split(" ")[1:]
        start = line[0]
        end = line[-1]
       
        clusterIdStart = pointInfoDict[int(start)][0]
        clusterIdEnd = pointInfoDict[int(end)][1]

        weightStart = pointInfoDict[int(start)][2]
        weightEnd = pointInfoDict[int(end)][2]
        assert weightStart == weightEnd
        weight = int(weightStart)
         
        # [[startCluster, endCluster, Weight, point1, point2, point3...]]
        thisLineList = [clusterIdStart, clusterIdEnd, weight]
        for j in range(0, len(line)):
            pointPosition = pointInfoDict[int(line[j])][3]
            thisLineList.append(pointPosition)
        linesList.append(thisLineList)
    
    folderPathComponents = folderPath.split("\\")
    parentFolderPath = ""
    for pathComponent in folderPathComponents[0:len(folderPathComponents) - 2]:
        parentFolderPath = parentFolderPath + pathComponent + "\\"
    
    readAndWriteFile.write_file(linesList, parentFolderPath + "Time Step_" + timeStep + "\\OutputForDB_" + fileName + ".csv", "list", "csv", "CFP")
    
    # uncomment this line if you want to also save the output to the individual folders. If not, the output will get saved in the
    # folder named after the current timestep. TL;DR leave this uncommented for most use cases. 
    # readAndWriteFile.write_file(linesList, folderPath + "OutputForDB_" + fileName + ".csv", "list", "csv", "CFP")

#this function reads the bundler output and puts it directly into the correct unity folder for my computer
def handleBrainBundlerOutputFile(brainBundlingOutput):
    start_index = 0
    end_index = 0

    for i in range(0, len(brainBundlingOutput)):
        line = brainBundlingOutput[i]
        if "POINTS" in line:
            start_index = i + 1  # index of first point
        if "LINES" in line:
            end_index = i + 1   # index of first line
            break
    
    pointsList = [["id","pos_x","pos_y","pos_z","area"]]
    pointToWeightDict = {}
    for i in range(start_index, end_index -1):
        point = brainBundlingOutput[i].split(" ")
        x = point[0]
        y = point[1]
        z = point[2]
        area = 1
        weight = int((float(point[3]) * 100))
        id = i - start_index  #point ids start from 0 in the bundling code
        thisPoint = [id+1, x, y, z, weight]
        pointToWeightDict[id] = weight
        pointsList.append(thisPoint)
    
    linesList = [["SourceId","TargetId","Weight"]]
    for i in range(end_index, len(brainBundlingOutput)):
        line = brainBundlingOutput[i].split(" ")[1:]
        start = line[0]
        end = line[-1]
        weightStart = pointToWeightDict[int(start)]
        weightEnd = pointToWeightDict[int(end)]
        assert weightStart == weightEnd
        weight = int(weightStart)
        
        for j in range(0, len(line)-1):
            thisStart = line[j]
            thisEnd = line[j+1]
            thisLine = [thisStart, thisEnd, weight]
            linesList.append(thisLine)
    readAndWriteFile.write_file(linesList, "lines_out.csv", "list", "csv", "UF")
    readAndWriteFile.write_file(pointsList, "points_out.csv", "list", "csv", "UF")
        

def computeEdgeBundlingPositionsCSV(childLocations, childIds):
    outputList = []
    sortedLocations = [x for _,x in sorted(zip(childIds,childLocations))]
    for i in sortedLocations:
        outputList.append(list(i))
    #write Output List to a useful location from which to call the bundling code

def computeEdgeBundlingSynapsesAndNodes(edgeIds):
    #indexes for edges should start from 1
    uniqueIds = []
    counts = dict(Counter(edgeIds))
    maxWeight = 0
    outputList = []
    for start_end in counts.keys():
        start = ast.literal_eval(start_end.split("_")[0])
        end = ast.literal_eval(start_end.split("_")[1])
        startIDString = str(start[0]) + "_" + str(start[1])
        endIDString = str(end[0]) + "_" + str(end[1])
        weight = counts[start_end]
        if weight > maxWeight:
            maxWeight = weight

        outputLine = [start, end, weight, startIDString, endIDString]
        uniqueIds.append(start)
        uniqueIds.append(end)
        outputList.append(outputLine)

    uniqueIds = list(set(uniqueIds))   # [1,10,11,8...]
    uniqueIdsToIndicesDict = {}
    neuronsList = []
    for i in range(0, len(uniqueIds)):
        uniqueId = uniqueIds[i]
        uniqueIdsToIndicesDict[uniqueId] = i+1  #[1:1, 10:2, 11:3, 8:4...]
        clusterObject = clusterIdsAndObjectsDict[uniqueId]
        clusterLocation  = list(clusterObject.location)
        neuronsList.append(clusterLocation)
    
    synapseList = []
    #reset indices to start from 1 and weights to be normalised between 1 and 10
    for edge in outputList:
        if weight >=1:
            start, end, weight, startIDString, endIDString = edge
            startCorrectIndex = uniqueIdsToIndicesDict[start]  
            endCorrectIndex = uniqueIdsToIndicesDict[end]

            # weightNormalised was earlier normalising based on the max value calculated above. 
            # However, we decided to use global normalisation so just dividing by 1000 here. Adding
            # 1 so that no weights are between 0 and 1. 
            weightNormalised = 1 + math.log10(weight)  

            newLine = [startCorrectIndex, endCorrectIndex, weightNormalised, startIDString, endIDString]
            synapseList.append(newLine)
    return (synapseList, neuronsList)

##########################################################################################
# recursive algorithm functions:

def runBrainBundlerCode(synapseList, neuronsList, fileName):
    folderPath = "C:\\Users\\Vidur\\Desktop\\Bundling Precompute\\" + fileName 
    if not os.path.exists(folderPath):
        os.makedirs(folderPath)
    
    timestep = fileName.split("_")[3]
    folderPath += "\\"
    readAndWriteFile.write_file(synapseList, folderPath + "bundlingInputSynapses.txt", "list", "txt", "CFP")
    readAndWriteFile.write_file(neuronsList, folderPath + "bundlingInputNeurons.txt", "list", "txt", "CFP")        
    
    p = subprocess.Popen("bundler -nodes bundlingInputNeurons.txt -cons bundlingInputSynapses.txt -fileName " + fileName +" -c_thr 0.4 -numcycles 10", cwd=folderPath, shell=True)
    p.wait()
    # os.chdir(folderPath)
    # os.system("bundler -nodes bundlingInputNeurons.txt -cons bundlingInputSynapses.txt -fileName " + fileName +" -c_thr 0.4 -numcycles 10")
    
    outputFile = readAndWriteFile.read_file(folderPath + fileName + ".txt", "txt", "list", "CFP")
    # handleBrainBundlerOutputFile(outputFile)
    handleBrainBundlerOutputFileForDatabase(outputFile, folderPath, fileName, timestep)

def bundleEdges(clusterObject):

    childrenClusters = clusterObject.childrenClusterObjects

    edgeIdsThatStayWithinSameParent = []
    edgeIdsThatDontShareSameParent = []
    
    # Why the if condition below:
    # At heirarchy level 0, we have cluster objects that contain no children. These actually correspond to neurons
    # At heirarchy level 1, we have cluster objects that are parents of groups of 10 neurons
    # The 10 neurons are placed on top of each other, so we never really want to see edges between them and this is never exploded out 
    # We therefore compute bundling only for one level above that
    # which is the smallest level we could explode outwards. 
    # I have a feeling the len(childrenClusters)>1 condition is always true at this level and can be removed due to the first condition
    if clusterObject.heirarchy > 1 and len(childrenClusters) > 1:  
        for childCluster in childrenClusters:
            edgesThatStayWithinSameParent = [] 
            edgesGoingOutsideSameParents = []
            
            # .getEdgesInHeirarchy(0) takes a single argument which specifies the delta 
            # of the heirarchy from which to get edges. For instance, if argument receives 
            # 1 as value, it will return edges from cluster.heirarchy + 1. If it receives 0
            # as the value, it will return edges from the same heirarchy as the object on which
            # it is being called
            for edge in childCluster.getEdgesInHeirarchy(0):
                if edge.parentClusterObject == childCluster.parentClusterObject:
                    edgesThatStayWithinSameParent.append(edge)
                else:
                    edgesGoingOutsideSameParents.append(edge)

            edgeIdsThatStayWithinSameParent += [str(childCluster.id) + "_" + str(edge.id) for edge in edgesThatStayWithinSameParent]
            edgeIdsThatDontShareSameParent  += [str(childCluster.id) + "_" + str(childCluster.parentClusterObject.id) for edge in edgesGoingOutsideSameParents]
            allCombinedEdgesForBundling = edgeIdsThatStayWithinSameParent + edgeIdsThatDontShareSameParent
            bundleEdges(childCluster)

        synapseList, neuronsList = computeEdgeBundlingSynapsesAndNodes(allCombinedEdgesForBundling)        
        fileName = timeStepFileName.split(".")[0] + "__" + "heirarchy_" + str(clusterObject.heirarchy) + "__ClusterId_" + str(clusterObject.id[0])
        p = Process(target = runBrainBundlerCode, args= (copy.deepcopy(synapseList), copy.deepcopy(neuronsList), fileName))
        processes.append(p)
            
        print(fileName)

        # runBrainBundlerCode(synapseList, neuronsList, fileName)

##########################################################################################

if __name__ == "__main__":
    # Create objects for all neurons (containing neuron id and parent clusters at all heirarchies)
    # and objects for all clusters (containing child clusters and parent cluster)
    clusterHeirarchies = readAndWriteFile.read_file("Edge Based Communities Latest.csv", "csv", "list", "DUAI")

    # this user entered variable stores the number of heirarchies provided
    # NOTE: The higher the heirarchy level, the bigger the cluster is. Heirarchy level starts from 1
    maxHeirarchyLevel = 4 

    clusterObjects = []  #list containing all objects of cluster type
    clusterIds = []   #list containing ids of all cluster objects
    clusterIdsAndObjectsDict = {}   #dictionary linking ids of cluster objects to the objects themselves

    # motherCluster is the highest cluster that contains the actual top level clusters from brain data
    # this doesn't actually exist in the data, it's just to make things easier in the code
    motherCluster = Cluster(-1,maxHeirarchyLevel+1) #set mother cluster to be id -1, and heirarchy to be the highest 
    motherCluster.isMotherCluster = True
    motherCluster.setLocation((-1000,-1000,-1000))

    clusterIdsAndObjectsDict[motherCluster.id] = motherCluster   #add the mother object with it's id to the dict


    neuronObjects = []
    neuronIds = []
    neuronIdsAndObjectsDict = {}

    rawNeuronsDict = {}
    neuronsRawFile = readAndWriteFile.read_file("neurons.csv", "csv", "list", "DUAI")

    for neuron in neuronsRawFile[1:]:
        id,pos_x,pos_y,pos_z,area = neuron
        rawNeuronsDict[int(id)] = (pos_x, pos_y, pos_z)
        thisNeuronObject = Neuron(int(id))
        thisNeuronObject.setLocation((float(pos_x), float(pos_y), float(pos_z)))
        neuronIdsAndObjectsDict[int(id)] = thisNeuronObject

    # clusterData contains [dummy,level_1,level_2,level_3,level_4]
    # level 1 is to be used to find the neuron ids in it's cluster
    for clusterData in clusterHeirarchies[1:]:   #first row ignored because it is the header row 
        #first column contains some dummy data, and is therefore ignored
        clusters = clusterData[1:]
        level1, level2, level3, level4 = clusters

        #i starts from 4-1 = 3
        for i in range (len(clusters)-1, -1, -1):  #iterate through all available heirarchy levels. Since the largest cluster is the last, start iterating from there
            clusterIndex = int(clusters[i])  
            if i == len(clusters)-1:  #if this is the highest heirarchy level, it won't have a parent. Set parent to mother cluster
                if (clusterIndex, i+1) not in clusterIdsAndObjectsDict.keys():
                    clusterObject = Cluster(clusterIndex, i+1) #cluster heirarchy levels start from 1, not 0
                    clusterIdsAndObjectsDict[clusterObject.id] = clusterObject
                    clusterObjects.append(clusterObjects)
                    clusterIds.append(clusterObject.id)
                    motherCluster.addChild(clusterObject)
                    clusterObject.setParent(motherCluster)
                else:
                    clusterObject = clusterIdsAndObjectsDict[(clusterIndex, i+1)]

            if i < len(clusters)-1:  # apart from highest cluster level, all other clusters have a parent
                # parent is the heirarchy level above, ie cluster id in the next column, so clusters[i+1]
                # current heirarchy level is i+1 (since heirarchies start from 1), and therefore parent's
                # heirarchy level is i+2
                parentClusterId = (int(clusters[i+1]), i+2) 
                
                # obtain parent cluster object from cluster dictionary. 
                # This is guaranteed to exist because we start from the largest cluster and 
                # move downwards to the smaller child clusters
                parentClusterObject = clusterIdsAndObjectsDict[parentClusterId]

                if (clusterIndex, i+1) not in clusterIdsAndObjectsDict.keys():
                    clusterObject = Cluster(clusterIndex, i+1) #cluster granularities start from 1
                    clusterIdsAndObjectsDict[clusterObject.id] = clusterObject
                    clusterObject.setParent(parentClusterObject)
                    parentClusterObject.addChild(clusterObject)
                    clusterObjects.append(clusterObject)
                    clusterIds.append(clusterObject.id)
                else:
                    clusterObject = clusterIdsAndObjectsDict[(clusterIndex, i+1)]

            # also treat neurons as lowest level clusters (because in the expanded view they need to
            # have edges drawn as well)
            if i == 0:
                clusterIndex = int(clusters[i])  
                for j in range (0, 10):
                    neuronId = int(level1)*10 + j + 1  #added 1 to be consistent with the edge file where neuron ids start from 1
                    thisNeuronObject = neuronIdsAndObjectsDict[neuronId]
                    neuronLocation = thisNeuronObject.location
                                                        # i = 0 here
                    neuronClusterObject = Cluster(neuronId, i) #regular cluster granularities start from 1. We keep the neuron cluster granularities = 0
                    neuronClusterObject.setLocation(neuronLocation)
                    clusterIdsAndObjectsDict[neuronClusterObject.id] = neuronClusterObject
                    thisNeuronObject.setParent(neuronClusterObject)  #set the parent of this neuron object to be the cluster object that represents this neuron
                    # clusters[i] = first cluster in the line, and i+1 is the heirarchy level = 1
                    parentClusterId = (int(clusters[i]), i+1)
                
                    # obtain parent cluster object from cluster dictionary. 
                    # This is guaranteed to exist because we start from the largest cluster and 
                    # move downwards to the smaller child clusters
                    parentClusterObject = clusterIdsAndObjectsDict[parentClusterId]
                    neuronClusterObject.setParent(parentClusterObject)
                    neuronClusterObject.setIsNeuronTrue()
                    parentClusterObject.addChild(neuronClusterObject)
                    clusterObjects.append(neuronClusterObject)
                    clusterIds.append(clusterObject.id)
            
            # neurons are already created above. Here, each neuron is assigned its corresponding
            # parents on all available heirarchies
            for j in range (0, 10):
                neuronId = int(level1)*10 + j + 1 #added 1 to be consistent with the edge file where neuron ids start from 1
                thisNeuronObject = neuronIdsAndObjectsDict[neuronId]
                thisNeuronObject.setParent(clusterObject)

    # printNeuronAttributes([1,2,3,4,5,6,7,8,9])
    # printClusterAttributes(1)
    clusterIds.sort()


    heirarchyReferenceDict = {0:4, 1:3, 2:2, 3:1}
    # CentroidConvex hull 2 - Level 0,-42.92578,42.59418,12.28041
    #### add representative points for each cluster:
    clusterPositions = readAndWriteFile.read_file("Heirarchical Representative Points.csv", "csv", "list", "DUAI")
    for clusterIndex in range(0, len(clusterPositions)):
        clusterPosition = clusterPositions[clusterIndex]
        clusterId = int(clusterPosition[0].split(" - ")[0].split(" ")[-1])
        heirarchyIndex = int(clusterPosition[0].split(" - ")[1].split(" ")[-1])   #cluster heirarchies start from 1 in my code (0 is assigned to neurons and -1 to the "mother cluster")
        heirarchyIndex = heirarchyReferenceDict[heirarchyIndex]
        clusterLocation = tuple(float(i) for i in clusterPosition[1:])
        clusterObject = clusterIdsAndObjectsDict[(clusterId, heirarchyIndex)]
        clusterObject.setLocation(clusterLocation)

    #### add edges to the cluster objects
    # to do this, iterate through the edge file and use the neuron object to find the parent clusters
    # of the source and destination nodes. Once these parent clusters are found, add edges appropriately

    timeStepFileName = "rank_0_step_1000000_out_network.txt"
    timestep = "Time Step_" + timeStepFileName.split("_")[3]
    timeStepFolderPath = "C:\\Users\\Vidur\\Desktop\\Bundling Precompute\\" + timestep 
    if not os.path.exists(timeStepFolderPath):
        os.makedirs(timeStepFolderPath)


    edges = inputEdgeFileToCSV.inputEdgeFileToCSV(timeStepFileName)
    # in the edge file, neuron ids start from 1, but this has already been handled so that 
    # neuron object ids also start from 1

    for edge in edges[1:]:
        Target_Rank,Target_ID,Source_Rank,Source_ID,Weight = [int(i) for i in edge]
        sourceNeuronObject = neuronIdsAndObjectsDict[Source_ID]
        targetNeuronObject = neuronIdsAndObjectsDict[Target_ID]
        
        sourceClusters = sourceNeuronObject.parentObjectsDict
        targetClusters = targetNeuronObject.parentObjectsDict
        
        numberOfHeirarchies = len(sourceClusters.keys())   #this will be 4+1 because one level of heirarchies is the neurons themselves

        for heirarchyIndex in range(0, numberOfHeirarchies): #heirarchies are labeled from 0 since heirarchy 0 is the "clusters" enclosing just one neuron
            sourceObjectAtThisLevel = sourceClusters[heirarchyIndex]
            targetObjectAtThisLevel = targetClusters[heirarchyIndex]
            if sourceObjectAtThisLevel != targetObjectAtThisLevel:
                sourceObjectAtThisLevel.addEdge(targetObjectAtThisLevel)
                targetObjectAtThisLevel.addEdge(sourceObjectAtThisLevel)


    allThreads = []
    bundlingFunctionCallParameters = []
    processes = []

    bundleEdges(motherCluster)

    numberOfSimultaneousProcesses = 8
    processes = processes[0:10] # creating a slice for debugging
    for processIndex in range(0, len(processes), numberOfSimultaneousProcesses):
        theseProcesses = processes[processIndex: min(len(processes), processIndex + numberOfSimultaneousProcesses)]
        for process in theseProcesses:
            process.start()
        for process in theseProcesses:
            process.join()

    end_time = time.time()
    time_taken = end_time - start_time
    print("Total Number of Processes: ", len(processes))
    print("Code compelted. Time taken: {:.5f} seconds".format(time_taken))