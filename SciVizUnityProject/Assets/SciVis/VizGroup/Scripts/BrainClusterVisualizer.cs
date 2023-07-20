/*
 * Visualization of hierarchical brain clusters.
 * Mouse click interaction with clusters are supported.
 * Clicking on a cluster expands it in smaller clusters. Clicking again, makes it retract.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using GK;
using Microsoft.MixedReality.Toolkit.SpatialManipulation;
using SciColorMaps.Portable;
using Unity.XR.CoreUtils;
using UnityEngine;

//importing this to easily print the properties of an object, without prior knowledge of the property names

public class BrainClusterVisualizer : MonoBehaviour, IBrainObserver
{
    private const int maxVertices = 64998;
    public Material grayMaterial;
    public GameObject brainGo;
    private bool animationStarted;
    private IBrainSubject brainSubject;

    private List<List<float>> cluster_values;
    private ColorMap colorMap;
    private bool hullSelected;
    private bool isCreated;
    public SemaphoreSlim isDrawingSemaphor = new(1, 1);
    private List<MoveResult> moveResults;
    private Dictionary<int, NeuronCluster> neuronClusterForConvexHullId = new();
    private Dictionary<int, InnerCluster> outerClusters;
    private Dictionary<int, OuterCluster> outerClusters1;
    private Dictionary<int, OuterCluster> outerClusters2;
    private Dictionary<int, OuterCluster> outerClusters3;
    private VisualizationHandler vizHandler;
    private StreamWriter writer;

    // Start is called before the first frame update
    private void Awake()
    {
        brainSubject = gameObject.GetComponentInParent<IBrainSubject>();
        vizHandler = gameObject.GetComponentInParent<VisualizationHandler>();
    }

    private void Start()
    {
        brainSubject.NotifyConvexHull();
        var emptyChildrenList = new List<string>();
        (brainSubject as BrainSubject).UpdateSplinesStream(0, 0, 5, "motherCluster",
            emptyChildrenList);
        //(brainSubject as BrainSubject).MakeBrainLegend();
    }

    public void ObserverUpdateSynapses(IBrainSubject subject)
    {
        //Nothing
    }

    public void ObserverUpdateSplines(IBrainSubject subject)
    {
        //Nothing
    }

    public void ObserverUpdateNeurons(IBrainSubject subject)
    {
    }

    public void ObserverUpdateConvexHull(IBrainSubject subject)
    {
        // :(
    }

    public void ObserverUpdateSelection(IBrainSubject brainSubject)
    {
        //Nothing
    }

    public void ObserverUpdateTerrain(IBrainSubject brainSubject)
    {
        //Nothing
    }

    public void ExplodeParentsOfCluster(HashSet<int> clusterIds, int granularity)
    {
        /*Debug.LogWarning("CLUSTERLEVEL 3");
        foreach (var id in outerClusters3.Keys)
        {
            Debug.LogWarning(id);
        }
        Debug.LogWarning("CLUSTERLEVEL 2");
        foreach (var id in outerClusters2.Keys)
        {
            Debug.LogWarning(id);
        }
        
        Debug.LogWarning("CLUSTERLEVEL 1");
        foreach (var id in outerClusters1.Keys)
        {
            Debug.LogWarning(id);
        }
        
        
        Debug.LogWarning("CLUSTERLEVEL 0");
        foreach (var id in outerClusters.Keys)
        {
            Debug.LogWarning(id);
        }*/
        if (!hullSelected)
        {
            hullSelected = true;
            animationStarted = true;
            moveResults = new List<MoveResult>();
            foreach (var clusterId in clusterIds)
            {
                Debug.LogWarning("GRANULARITY: " + granularity);

                GameObject cluster = null;
                if (granularity == 2)
                    cluster = outerClusters1[clusterId].GetClusterGameObject();
                else if (granularity == 3)
                    cluster = outerClusters2[clusterId].GetClusterGameObject();
                else if (granularity == 4)
                    cluster = outerClusters3[clusterId].GetClusterGameObject();

                var p = cluster.GetComponent<ClusterProperty>();
                if (p == null) p = cluster.AddComponent<ClusterProperty>();
                if (!p.selected)
                {
                    var current = cluster;
                    for (var i = granularity + 1; i <= 4; i++)
                    {
                        current = current.transform.parent.gameObject;
                        var p2 = current.GetComponent<ClusterProperty>();
                        if (p2 == null) p2 = current.AddComponent<ClusterProperty>();

                        if (p2.selected) break;

                        moveResults.AddRange(ExplodeViewOfGameObject(current));
                        var neuronCluster = current.GetComponent<ClusterInfo>();
                        ((BrainSubject)brainSubject).GetSpec()
                            .ExplodedClusters[neuronCluster.ClusterLevel].Add(neuronCluster.ID);

                        p2.selected = !p2.selected;
                    }
                }
            }
        }

        if (hullSelected && animationStarted)
        {
            StartCoroutine(Move(new List<MoveResult>(moveResults)));
            moveResults = new List<MoveResult>();
            animationStarted = false;
        }
    }

    public async void Draw(List<NeuronPosition> neurons, GameObject parent)
    {
        await isDrawingSemaphor.WaitAsync();
        if (!isCreated)
        {
            brainGo = new GameObject("ConvexBrain");

            brainGo.transform.parent = parent.transform;
            brainGo.transform.localPosition = Vector3.zero;
            brainGo.transform.localRotation = Quaternion.identity;
            brainGo.transform.localScale = Vector3.one;

            //outerClusters2 = ReadNeuronClusters(neurons);
            await ReadNeuronClusters(neurons);

            var neurons2 = new List<ConvexNeuron>();
            foreach (var pair in outerClusters3) neurons2.AddRange(pair.Value.GetNeurons());

            var neuronsCentroid = new Vector3(0, 0, 0);
            foreach (var neuron in neurons2) neuronsCentroid += neuron.GetPosition();
            neuronsCentroid /= neurons2.Count;

            CreateConvexHullsFromNeuronClusters(outerClusters3, neuronsCentroid, brainGo);

            writer = new StreamWriter(Application.persistentDataPath + "/table.csv");
            // ComputeClusterCentroids(brainGo, neuronsCentroid, new Vector3(0, 0, 0), 0);
            writer.Close();
            isCreated = true; // only create clusters etc. once
        }

        UpdateConvexHullColors(brainGo);
        isDrawingSemaphor.Release();
    }

    private void ComputeClusterCentroids(GameObject brainGo, Vector3 neuronsCentroid,
        Vector3 previousPosition, int level)
    {
        level++;
        foreach (Transform child in brainGo.transform)
        {
            var m = child.GetComponent<MeshFilter>().mesh;
            var verts = m.vertices;
            var centroidV = new Vector3(0, 0, 0);
            foreach (var vert in verts) centroidV += vert;
            centroidV /= verts.Length;

            Vector3 save;
            var translation = 50 * centroidV.normalized;
            Vector3 explodedPosition; // = child.gameObject.AddComponent<ExplodedPosition>();
            if (level == 1)
            {
                save = new Vector3(0, 0, 0);
                explodedPosition = centroidV;
            }
            else
            {
                save = previousPosition + translation;
                explodedPosition = save + centroidV; //save;
            }

            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.localScale = new Vector3(1f, 1f, 1f);
            sphere.transform.localPosition = explodedPosition + neuronsCentroid;
            // sphere.transform.localPosition *= 0.5f*0.01f;
            //if(level == 1){
            sphere.transform.parent = this.brainGo.transform;
            var localScale = sphere.transform.localScale * 0.5f * 0.01f;
            sphere.transform.localScale = new Vector3(localScale.x, localScale.y, localScale.z);
            sphere.transform.localPosition *= 0.5f * 0.01f;
            // }
            writer.WriteLine("Centroid{0},{1},{2},{3}", child.name,
                sphere.transform.localPosition.x, sphere.transform.localPosition.y,
                sphere.transform.localPosition.z);


            if (level < 4) //change to 3 in student code (depending on number of community levels)
                ComputeClusterCentroids(child.gameObject, neuronsCentroid, save, level);
        }
    }

    public void Zoom(RaycastHit hit)
    {
        Zoom(hit.transform.gameObject);
    }

    public void Zoom(GameObject hit)
    {
        if (!hullSelected)
        {
            var p = hit.GetComponent<ClusterProperty>();
            if (p == null) p = hit.AddComponent<ClusterProperty>();
            p.selected = !p.selected;

            var neuronCluster = hit.GetComponent<ClusterInfo>();
            var clusterID = neuronCluster.ID;
            var granularity = neuronCluster.ClusterLevel;
            /*if (outerClusters.ContainsKey(clusterID) && outerClusters[clusterID] == neuronCluster)
            {
                granularity = 3;
            }
            else if (outerClusters1.ContainsKey(clusterID) && outerClusters1[clusterID] == neuronCluster)
            {
                granularity = 2;
            }
            else if (outerClusters2.ContainsKey(clusterID) && outerClusters2[clusterID] == neuronCluster)
            {
                granularity = 1;
            }
            else if (outerClusters3.ContainsKey(clusterID) && outerClusters3[clusterID] == neuronCluster)
            {
                granularity = 0;
            }*/
            if (p.selected)
            {
                //ShowInner(hit.transform.gameObject);
                moveResults = ExplodeViewOfGameObject(hit);
                hullSelected = true;
                animationStarted = true;

                ((BrainSubject)brainSubject).GetSpec().ExplodedClusters[granularity].Add(clusterID);
            }
            else
            {
                //HideInner(hit.transform.gameObject);
                moveResults = RetractViewOfGameObject(hit);
                hullSelected = true;
                animationStarted = true;
                ((BrainSubject)brainSubject).GetSpec().ExplodedClusters[granularity]
                    .Remove(clusterID);
            }
        }

        if (hullSelected && animationStarted)
        {
            StartCoroutine(Move(new List<MoveResult>(moveResults)));
            moveResults = new List<MoveResult>();
            animationStarted = false;
        }
    }

    public async Task ExplodeAllClustersFromList(List<HashSet<int>> clustersToExplode)
    {
        Debug.LogWarning("Clusters to explode");
        Debug.LogWarning(clustersToExplode.Count);
        for (var index = 0; index < clustersToExplode.Count; index++)
        {
            Debug.LogWarning("Clusters to explode for index " + index + ": " +
                             clustersToExplode[index].Count);
            foreach (var clusterID in clustersToExplode[index])
            {
                Debug.LogWarning("Looking at cluster " + clusterID);
                hullSelected = false;
                if (outerClusters3.ContainsKey(clusterID) && index == 0)
                {
                    Zoom(outerClusters3[clusterID].GetClusterGameObject());
                    Debug.LogWarning("Should explode 3");
                }
                else if (outerClusters2.ContainsKey(clusterID) && index == 1)
                {
                    Debug.LogWarning("Should explode 2");
                    Zoom(outerClusters2[clusterID].GetClusterGameObject());
                }
                else if (outerClusters1.ContainsKey(clusterID) && index == 2)
                {
                    Debug.LogWarning("Should explode 1");
                    Zoom(outerClusters1[clusterID].GetClusterGameObject());
                }
                else if (outerClusters.ContainsKey(clusterID) && index == 3)
                {
                    Debug.LogWarning("Should explode 0");
                    Zoom(outerClusters[clusterID].GetClusterGameObject());
                }
            }
        }
    }

    public async Task CollapseAllClusters()
    {
        if (!hullSelected)
        {
            Debug.LogWarning("NumChildren: " + transform.childCount);
            foreach (Transform child in transform.Find("ConvexBrain"))
            {
                var p = child.gameObject.GetComponent<ClusterProperty>();
                if (p != null && p.selected)
                {
                    moveResults.AddRange(RetractViewOfGameObject(child.gameObject));
                    p.selected = false;
                    hullSelected = true;
                    animationStarted = true;
                }
            }
        }


        if (hullSelected && animationStarted)
        {
            //Coroutine routine = StartCoroutine(Move(new List<MoveResult>(moveResults)));
            await AsyncMove(new List<MoveResult>(moveResults));
            moveResults = new List<MoveResult>();
            animationStarted = false;
        }
    }

    private void ShowInner(GameObject hull)
    {
        foreach (Transform child in hull.transform)
        foreach (Transform childOfChild in child)
            childOfChild.gameObject.SetActive(true);
    }

    private void HideInner(GameObject hull)
    {
        foreach (Transform child in hull.transform)
        foreach (Transform childOfChild in child)
            childOfChild.gameObject.SetActive(false);
    }

    private async Task ReadNeuronClusters(List<NeuronPosition> neurons)
    {
        var specification = brainSubject.GetSpec();

        var queryCommunity1 = new StratumQuery(
            specification.SimulationId,
            0,
            specification.NeuronTimeStep,
            new NeuronAttribute { value = NeuronAttributeType.CommunityLevel1 },
            new NeuronFilter(Guid.NewGuid(),
                new NeuronAttribute { value = NeuronAttributeType.Calcium }, 0, 1));

        var queryCommunity2 = new StratumQuery(
            specification.SimulationId,
            0,
            specification.NeuronTimeStep,
            new NeuronAttribute { value = NeuronAttributeType.CommunityLevel2 },
            new NeuronFilter(Guid.NewGuid(),
                new NeuronAttribute { value = NeuronAttributeType.Calcium }, 0, 1));

        var queryCommunity3 = new StratumQuery(
            specification.SimulationId,
            0,
            specification.NeuronTimeStep,
            new NeuronAttribute { value = NeuronAttributeType.CommunityLevel3 },
            new NeuronFilter(Guid.NewGuid(),
                new NeuronAttribute { value = NeuronAttributeType.Calcium }, 0, 1));

        var queryCommunity4 = new StratumQuery(
            specification.SimulationId,
            0,
            specification.NeuronTimeStep,
            new NeuronAttribute { value = NeuronAttributeType.CommunityLevel4 },
            new NeuronFilter(Guid.NewGuid(),
                new NeuronAttribute { value = NeuronAttributeType.Calcium }, 0, 1));

        await brainSubject.Stratum(queryCommunity1);
        await brainSubject.Stratum(queryCommunity2);
        await brainSubject.Stratum(queryCommunity3);
        await brainSubject.Stratum(queryCommunity4);

        var entries = new List<CsvEntry>();
        foreach (var neuron in neurons)
        {
            var x = neuron.Position.x;
            var y = neuron.Position.y;
            var z = neuron.Position.z;
            var area = neuron.Area;
            var genericNeuron = (brainSubject as BrainSubject)._neurons
                .Where(i => i.Id == neuron.Id).First();
            var community0 = genericNeuron.CommunityLevel4;
            var community1 = genericNeuron.CommunityLevel3;
            var community2 = genericNeuron.CommunityLevel2;
            var community3 = genericNeuron.CommunityLevel1;

            var entry = new CsvEntry(new ConvexNeuron(new Vector3(x, y, z), area, neuron.Id),
                community0, community1, community2, community3);
            entries.Add(entry);
            // print(entry);
            // print(community0 + "_" + community1 + "_" + community2 + "_" + community3);
        }

        var dictOfInnerClusters = new Dictionary<int, InnerCluster>();
        var dictOfOuterClusters1 = new Dictionary<int, OuterCluster>();
        var dictOfOuterClusters2 = new Dictionary<int, OuterCluster>();
        var dictOfOuterClusters3 = new Dictionary<int, OuterCluster>();

        foreach (var entry in entries)
        {
            var comms = entry.GetCommunities();
            var community4 = comms[3];
            if (dictOfInnerClusters.ContainsKey(community4))
            {
                dictOfInnerClusters[community4].AddNeuron(entry.GetNeuron());
            }
            else
            {
                var innerCluster = new InnerCluster();
                innerCluster.SetClusterId(community4);
                innerCluster.SetClusterLevel(4);
                dictOfInnerClusters[community4] = innerCluster;
                dictOfInnerClusters[community4].AddNeuron(entry.GetNeuron());
            }
        }

        foreach (var entry in entries)
        {
            var comms = entry.GetCommunities();
            var community3 = comms[2];
            var community4 = comms[3];
            dictOfInnerClusters.TryGetValue(community4, out var innerCluster);

            if (dictOfOuterClusters1.ContainsKey(community3))
            {
                dictOfOuterClusters1[community3].AddNeuronCluster(innerCluster);
                // print(innerCluster.GetClusterId() +"-level"+2);
            }
            else
            {
                var outerCluster = new OuterCluster();
                outerCluster.SetClusterId(community3);
                outerCluster.SetClusterLevel(3);
                dictOfOuterClusters1[community3] = outerCluster;
                dictOfOuterClusters1[community3].AddNeuronCluster(innerCluster);
            }
        }

        foreach (var entry in entries)
        {
            var comms = entry.GetCommunities();
            var community2 = comms[1];
            var community3 = comms[2];
            dictOfOuterClusters1.TryGetValue(community3, out var outerCluster);

            if (dictOfOuterClusters2.ContainsKey(community2))
            {
                dictOfOuterClusters2[community2].AddNeuronCluster(outerCluster);
                // print(outerCluster.GetClusterId() +"-level"+1);
            }
            else
            {
                var outerCluster2 = new OuterCluster();
                outerCluster2.SetClusterId(community2);
                outerCluster2.SetClusterLevel(2);
                dictOfOuterClusters2[community2] = outerCluster2;
                dictOfOuterClusters2[community2].AddNeuronCluster(outerCluster);
            }
        }

        foreach (var entry in entries)
        {
            var comms = entry.GetCommunities();
            var community1 = comms[0];
            var community2 = comms[1];
            dictOfOuterClusters2.TryGetValue(community2, out var outerCluster);

            if (dictOfOuterClusters3.ContainsKey(community1))
            {
                dictOfOuterClusters3[community1].AddNeuronCluster(outerCluster);
                // print(outerCluster.GetClusterId() +"-level"+0);
            }
            else
            {
                var outerCluster3 = new OuterCluster();
                outerCluster3.SetClusterId(community1);
                outerCluster3.SetClusterLevel(1);
                dictOfOuterClusters3[community1] = outerCluster3;
                dictOfOuterClusters3[community1].AddNeuronCluster(outerCluster);
            }
        }

        outerClusters3 = dictOfOuterClusters3;
        outerClusters2 = dictOfOuterClusters2;
        outerClusters1 = dictOfOuterClusters1;
        outerClusters = dictOfInnerClusters;
    }

    private float GetMeanAttributeValue(List<Neuron> neurons, NeuronAttribute attribute)
    {
        float sum = 0;
        foreach (var neuron in neurons) sum += neuron.GetValueOf(attribute);
        return sum / neurons.Count;
    }

    private Color getColorForMeanClusterValue(float attributeValue)
    {
        var colorBytes = colorMap[attributeValue];
        return new Color(colorBytes[0] / 255f, colorBytes[1] / 255f, colorBytes[2] / 255f, 1.0f);
    }

    private List<Neuron> GetNeuronsInCluster(NeuronCluster cluster)
    {
        var subject = (BrainSubject)brainSubject;
        var clusterLevel = cluster.GetClusterLevel();
        var clusterId = cluster.GetClusterId();

        // FIXME: cluster level and community level are inverted!
        if (clusterLevel == 4)
            return subject._neurons.Where(n => n.CommunityLevel1 == clusterId).ToList();
        if (clusterLevel == 3)
            return subject._neurons.Where(n => n.CommunityLevel2 == clusterId).ToList();
        if (clusterLevel == 2)
            return subject._neurons.Where(n => n.CommunityLevel3 == clusterId).ToList();
        if (clusterLevel == 1)
            return subject._neurons.Where(n => n.CommunityLevel4 == clusterId).ToList();
        throw new Exception("Invalid cluster level");
    }

    private (float, float) getEncodingMinMax(NeuronAttribute neuronAttribute, List<Neuron> neurons,
        bool useLocalColorScale)
    {
        if (!useLocalColorScale)
        {
            return neuronAttribute.GetColorMinMax();
        }

        var localAttributes = neurons.Select(x => x.GetValueOf(neuronAttribute)).ToList();
        var maxLocalAttribute = localAttributes.Max();
        var minLocalAttribute = localAttributes.Min();

        return (minLocalAttribute, maxLocalAttribute);
    }

    private Color getClusterColor(ClusterInfo clusterInfo)
    {
        var subject = brainSubject as BrainSubject;
        var spec = subject.GetSpec();
        var colorAttribute = spec.NeuronColorEncoding;

        var neurons = clusterInfo.neurons;
        var attributeValue = GetMeanAttributeValue(neurons, colorAttribute);

        // store computed mean color attribute value to make tooltips easier
        clusterInfo.MeanColorAttributeValue = attributeValue;

        var color = getColorForMeanClusterValue(clusterInfo.MeanColorAttributeValue);
        // color.a = 1.0f / 4.0f * clusterInfo.ClusterLevel;
        if (clusterInfo.ClusterLevel == 0)
        {
            color.a = 0.7f;
        } 
        else if (clusterInfo.ClusterLevel == 1)
        {
            color.a = 0.9f;
        } 
        else if (clusterInfo.ClusterLevel == 2)
        {
            color.a = 1f;
        }
        else if (clusterInfo.ClusterLevel == 3)
        {
            color.a = 0f;
        }

        return color;
    }

    private void SetConvexHullColor(GameObject convexHull)
    {
        var clusterInfo = convexHull.GetComponent<ClusterInfo>();
        var color = getClusterColor(clusterInfo);

        convexHull.GetComponent<MeshRenderer>().material.color = color;
    }

    private void UpdateConvexHullColors(GameObject brainGo)
    {
        var subject = brainSubject as BrainSubject;

        var spec = subject.GetSpec();
        var neurons = subject._neurons;
        var colorAttribute = spec.NeuronColorEncoding;
        var useLocalColorScale = spec.LocalColorScale;

        var (min, max) = getEncodingMinMax(colorAttribute, neurons, useLocalColorScale);

        // special case: if calcium is selected as color attribute, users have the option of using
        // a diverging color scale encoding the delta to the target Calcium level in the simulation
        colorMap = ColorMapBuilder.UseDivergentColorScale(subject)
            ? ColorMapBuilder.CreateDivergingColorMap(min, ColorMapBuilder.TARGET_CALCIUM_LEVEL,
                max)
            : ColorMapBuilder.CreateSequentialColorMap(min, max);

        var convexHulls = brainGo.GetComponentsInChildren<Transform>(true)
            .Where(x => x.gameObject.GetInstanceID() != brainGo.GetInstanceID())
            .Select(x => x.gameObject)
            .ToList();

        // this assumes that only convex hulls are contained in brainGo!
        foreach (var convexHull in convexHulls)
        {
            SetConvexHullColor(convexHull);
        }
    }

    private void CreateConvexHullsFromNeuronClusters(
        Dictionary<int, OuterCluster> dictOfOuterClusters2, Vector3 centroidOfNeruons,
        GameObject brainGo)
    {
        var colors = new List<Color>();
        for (var i = 0; i < 500; i++)
        {
            colors.Add(ConvertColorValue(166, 206, 227));
            colors.Add(ConvertColorValue(31, 120, 180));
            colors.Add(ConvertColorValue(178, 223, 138));
            colors.Add(ConvertColorValue(51, 160, 44));
            colors.Add(ConvertColorValue(251, 154, 153));
            colors.Add(ConvertColorValue(227, 26, 28));
            colors.Add(ConvertColorValue(253, 191, 111));
            colors.Add(ConvertColorValue(255, 127, 0));
            colors.Add(ConvertColorValue(202, 178, 214));
            colors.Add(ConvertColorValue(106, 61, 154));
            colors.Add(ConvertColorValue(255, 255, 153));
        }

        var c = 0;
        foreach (var pair in dictOfOuterClusters2)
        {
            var outerCluster = pair.Value;
            var convexHull = CreateConvexHullFromNeurons(pair.Value.GetClusterId(), 0,
                centroidOfNeruons, outerCluster.GetNeurons(), grayMaterial, colors[c], brainGo);
            outerCluster.SetClusterGameObject(convexHull);
            var neuronClusters = outerCluster.GetNeuronClusters();
            foreach (var cluster in neuronClusters)
            {
                var convexHull2 = CreateConvexHullFromNeurons(cluster.GetClusterId(), 1,
                    centroidOfNeruons, cluster.GetNeurons(), grayMaterial, colors[c], convexHull);
                cluster.SetClusterGameObject(convexHull2);
                var neuronClusters2 = ((OuterCluster)cluster).GetNeuronClusters();
                foreach (var cluster2 in neuronClusters2)
                {
                    var convexHull3 = CreateConvexHullFromNeurons(cluster2.GetClusterId(), 2,
                        centroidOfNeruons, cluster2.GetNeurons(), grayMaterial, colors[c],
                        convexHull2);
                    cluster2.SetClusterGameObject(convexHull3);
                    var neuronClusters3 = ((OuterCluster)cluster2).GetNeuronClusters();
                    foreach (var cluster3 in neuronClusters3)
                    {
                        var convexHull4 = CreateConvexHullFromNeurons(cluster3.GetClusterId(), 3,
                            centroidOfNeruons, cluster3.GetNeurons(), grayMaterial, colors[c],
                            convexHull3);
                        cluster3.SetClusterGameObject(convexHull4);
                    }
                }
            }

            c++;
        }
    }

    private GameObject CreateConvexHullFromNeurons(int community, int level,
        Vector3 centroidOfNeurons, List<ConvexNeuron> neurons, Material material, Color color,
        GameObject parent)
    {
        var calc = new ConvexHullCalculator();
        var verts = new List<Vector3>();
        var tris = new List<int>();
        var normals = new List<Vector3>();
        var points = new List<Vector3>();

        foreach (var neuron in neurons) points.Add(neuron.GetPosition() - centroidOfNeurons);

        calc.GenerateHull(points, true, ref verts, ref tris, ref normals);
        var mesh = new Mesh();
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.SetNormals(normals);


        var centroidV = new Vector3(0, 0, 0);
        foreach (var vert in verts) centroidV += vert;
        centroidV /= verts.Count;

        var convexObject = new GameObject("Convex hull " + community + " - Level " + level);
        var info = convexObject.AddComponent<ClusterInfo>();
        info.ClusterLevel = level;
        info.ID = community;

        var subject = brainSubject as BrainSubject;
        if (level == 3)
            info.neurons = subject._neurons.Where(n => n.CommunityLevel1 == community).ToList();
        else if (level == 2)
            info.neurons = subject._neurons.Where(n => n.CommunityLevel2 == community).ToList();
        else if (level == 1)
            info.neurons = subject._neurons.Where(n => n.CommunityLevel3 == community).ToList();
        else if (level == 0)
            info.neurons = subject._neurons.Where(n => n.CommunityLevel4 == community).ToList();
        else
            throw new Exception("Invalid cluster level");

        convexObject.AddComponent<MeshFilter>().mesh = mesh;
        convexObject.AddComponent<MeshCollider>().sharedMesh = mesh;
        convexObject.AddComponent<MeshRenderer>().material = material;
        // var alpha = 1.0f / 5.5f * (level + 1);
        var alpha = 0.7f;
        convexObject.GetComponent<MeshRenderer>().material.color =
            new Color(color.r, color.g, color.b,
                alpha); //= new Color(grayColor.r, grayColor.g, grayColor.b, 60); 
        float scaleFactor = 1;
        if (level == 0) scaleFactor = 1.0f;
        if (level == 1) scaleFactor = 1.0f;
        if (level == 2)
        {
            scaleFactor = 1.0f;
            convexObject.SetActive(false);
        }

        if (level == 3)
        {
            scaleFactor = 1.0f;
            convexObject.SetActive(false);
        }

        //scaling 
        var step = centroidV * scaleFactor - centroidV;

        convexObject.transform.parent = parent.transform;
        convexObject.transform.localPosition = Vector3.zero;
        convexObject.transform.localRotation = Quaternion.identity;
        convexObject.transform.localScale = Vector3.one;

        convexObject.SetLayerRecursively(16);
        convexObject.GetComponentInParent<ObjectManipulator>().colliders
            .Add(convexObject.GetComponent<Collider>());
        convexObject.GetComponentInParent<ObjectManipulator>().enabled = false;
        convexObject.GetComponentInParent<ObjectManipulator>().enabled = true;
        return convexObject;
    }


    private Color ConvertColorValue(float r, float g, float b)
    {
        return new Color(r / 255, g / 255, b / 255);
    }

    private List<MoveResult> RetractViewOfGameObject(GameObject hull)
    {
        var result = new List<MoveResult>();
        //Retract children of children, if exploded in two levels
        foreach (Transform child in hull.transform)
        {
            if (child.gameObject.GetComponent<ClusterProperty>() != null &&
                child.gameObject.GetComponent<ClusterProperty>().selected)
            {
                var childResult = RetractViewOfGameObject(child.gameObject);
                result.AddRange(childResult);

                foreach (Transform grandChild in child.transform)
                {
                    if (grandChild.gameObject.GetComponent<ClusterProperty>() != null &&
                        grandChild.gameObject.GetComponent<ClusterProperty>().selected)
                    {
                        var grandChildResult = RetractViewOfGameObject(grandChild.gameObject);
                        result.AddRange(grandChildResult);
                    }

                    foreach (Transform childOfGrandChild in grandChild)
                        childOfGrandChild.gameObject.SetActive(false);
                }
            }

            //Only make clusters at level 2 invisible
            foreach (Transform childOfChild in child) childOfChild.gameObject.SetActive(false);
        }

        foreach (Transform child in hull.transform)
        {
            var m = child.gameObject.GetComponent<MeshFilter>().mesh;
            var verts = m.vertices;
            var centroidV = new Vector3(0, 0, 0);
            foreach (var vert in verts) centroidV += vert;
            centroidV /= verts.Length;

            var translation = 50 * centroidV.normalized;

            var mr = new MoveResult(child.gameObject, child.localPosition,
                child.localPosition - translation);
            result.Add(mr);

            var neuronPositions = vizHandler.neurons;
            var selectedConvexNeuronIds = GetSelectedNeuronIds(child.name);

            foreach (var id in selectedConvexNeuronIds) neuronPositions[id].Position -= translation;

            vizHandler.neurons = neuronPositions;
            brainSubject.NotifyNeurons();

            if (child.gameObject.GetComponent<ClusterProperty>() != null)
            {
                child.gameObject.GetComponent<ClusterProperty>().selected = false;
                var info = child.gameObject.GetComponent<ClusterInfo>();
                ((BrainSubject)brainSubject).GetSpec().ExplodedClusters[info.ClusterLevel]
                    .Remove(info.ID);
            }
            else
            {
                child.gameObject.AddComponent<ClusterProperty>().selected = false;
                var info = child.gameObject.GetComponent<ClusterInfo>();
                ((BrainSubject)brainSubject).GetSpec().ExplodedClusters[info.ClusterLevel]
                    .Remove(info.ID);
            }
        }

        UpdateSplineCallRetract(hull.name);
        return result;
    }

    private List<int> GetHullIdAndHeirarchy(string hullName)
    {
        var IdString = Regex.Match(hullName, @"\d+").Value;
        var resultIdInt = int.Parse(IdString);

        var Granularity = Regex.Match(hullName, @"(\d+)(?!.*\d)").Value;
        var GranularityInt = int.Parse(Granularity);

        var outputList = new List<int>();
        outputList.Add(resultIdInt);
        outputList.Add(GranularityInt);

        return outputList;
    }

    private List<int> GetSelectedNeuronIds(string childName)
    {
        var resultChildString = Regex.Match(childName, @"\d+").Value;
        var resultChildInt = int.Parse(resultChildString);

        var childGranularity = Regex.Match(childName, @"(\d+)(?!.*\d)").Value;
        var childGranularityInt = int.Parse(childGranularity);

        var selectedConvexNeurons = new List<ConvexNeuron>();

        switch (childGranularityInt)
        {
            case 0:
                selectedConvexNeurons = outerClusters3[resultChildInt].GetNeurons();
                break;
            case 1:
                selectedConvexNeurons = outerClusters2[resultChildInt].GetNeurons();
                break;
            case 2:
                selectedConvexNeurons = outerClusters1[resultChildInt].GetNeurons();
                break;
            case 3:
                selectedConvexNeurons = outerClusters[resultChildInt].GetNeurons();
                break;
            default:
                selectedConvexNeurons = null;
                break;
        }

        var selectedConvexNeuronIds = selectedConvexNeurons.Select(t => t.GetId()).ToList();
        return selectedConvexNeuronIds;
    }


    private List<MoveResult> ExplodeViewOfGameObject(GameObject hull)
    {
        var result = new List<MoveResult>();
        var childIds = new List<string>();
        var parentId = hull.name;

        foreach (Transform child in hull.transform)
        {
            childIds.Add(child.name);
            var m = child.gameObject.GetComponent<MeshFilter>().mesh;
            var verts = m.vertices;
            var centroidV = new Vector3(0, 0, 0);
            foreach (var vert in verts) centroidV += vert;
            centroidV /= verts.Length;

            var translation = 50 * centroidV.normalized;

            var mr = new MoveResult(child.gameObject, child.localPosition,
                child.localPosition + translation);
            result.Add(mr);

            var neuronPositions = vizHandler.neurons;
            var selectedConvexNeuronIds = GetSelectedNeuronIds(child.name);

            foreach (var id in selectedConvexNeuronIds) neuronPositions[id].Position += translation;


            vizHandler.neurons = neuronPositions;
            brainSubject.NotifyNeurons();

            foreach (Transform childOfChild in child) childOfChild.gameObject.SetActive(true);
        }

        var hullIdAndHeirarchy = GetHullIdAndHeirarchy(hull.name);
        var hullId = hullIdAndHeirarchy[0];
        var hullHeirarchy = 4 - hullIdAndHeirarchy[1];
        UpdateSplineCall(hullId, hullHeirarchy, parentId, childIds);
        return result;
    }

    private async void UpdateSplineCall(int hullId, int hullHeirarchy, string parentId,
        List<string> childIds)
    {
        Debug.Log("logging time step__" + brainSubject.GetSpec().SynapseTimeStep);
        Debug.Log(brainSubject.GetSpec().SynapseTimeStep);

        await (brainSubject as BrainSubject).semaphoreSlim.WaitAsync();
        try
        {
            await (brainSubject as BrainSubject).UpdateSplinesStream(1000000, hullId, hullHeirarchy,
                parentId, childIds);
            // TO DO: update the "1000000" above to the actual time step of the simulation
            // defined on the second Debug.Log() statement above 
        }
        finally
        {
            (brainSubject as BrainSubject).semaphoreSlim.Release();
        }
    }

    private async void UpdateSplineCallRetract(string parentId)
    {
        await (brainSubject as BrainSubject).semaphoreSlim.WaitAsync();
        try
        {
            await (brainSubject as BrainSubject).RetractClusterUpdateSplines(parentId);
        }
        finally
        {
            (brainSubject as BrainSubject).semaphoreSlim.Release();
        }
    }

    private IEnumerator Move(List<MoveResult> moveResults)
    {
        for (var i = 0f; i <= 1; i += 0.04f)
        {
            var newT = (float)expInOut(i);
            foreach (var mr in moveResults)
            {
                var start = mr.Start;
                var end = mr.End;
                var go = mr.Go;

                go.transform.localPosition = Vector3.Lerp(start, end, newT);
            }

            yield return null;
        }

        hullSelected = false;
    }

    private async Task AsyncMove(List<MoveResult> moveResults)
    {
        for (var i = 0f; i <= 1; i += 0.04f)
        {
            var newT = (float)expInOut(i);
            foreach (var mr in moveResults)
            {
                var start = mr.Start;
                var end = mr.End;
                var go = mr.Go;

                go.transform.localPosition = Vector3.Lerp(start, end, newT);
            }

            await Task.Yield();
        }

        hullSelected = false;
    }

    private double expInOut(double t)
    {
        return ((t *= 2) <= 1 ? tpmt(1 - t) : 2 - tpmt(t - 1)) / 2;
    }

    private double tpmt(double x)
    {
        return (Math.Pow(2, -10 * x) - 0.0009765625) * 1.0009775171065494;
    }
}


public class ConvexNeuron
{
    private readonly int area;
    private readonly int id;

    private readonly Vector3 position;

    public ConvexNeuron(Vector3 position, int area, int id)
    {
        this.position = position;
        this.area = area;
        this.id = id;
    }

    public Vector3 GetPosition()
    {
        return position;
    }

    public int GetArea()
    {
        return area;
    }

    public int GetId()
    {
        return id;
    }
}

public class CsvEntry
{
    private readonly int comm0;
    private readonly int comm1;
    private readonly int comm2;
    private readonly int comm3;
    private readonly ConvexNeuron neuron;

    public CsvEntry(ConvexNeuron neuron, int comm0, int comm1, int comm2, int comm3)
    {
        this.neuron = neuron;
        this.comm0 = comm0;
        this.comm1 = comm1;
        this.comm2 = comm2;
        this.comm3 = comm3;
    }

    public ConvexNeuron GetNeuron()
    {
        return neuron;
    }

    public int[] GetCommunities()
    {
        return new[] { comm0, comm1, comm2, comm3 };
    }
}

public abstract class NeuronCluster
{
    private GameObject clusterGameObject;
    private int id;
    private int level;
    public abstract List<ConvexNeuron> GetNeurons();

    public Vector3 GetClusterCentroid()
    {
        var neurons = GetNeurons();
        var centroid = new Vector3(0, 0, 0);
        foreach (var n in neurons) centroid += n.GetPosition();
        centroid /= neurons.Count;
        return centroid;
    }

    public int GetClusterId()
    {
        return id;
    }

    public void SetClusterId(int id)
    {
        this.id = id;
    }

    public int GetClusterLevel()
    {
        return level;
    }

    public void SetClusterLevel(int level)
    {
        this.level = level;
    }

    public GameObject GetClusterGameObject()
    {
        return clusterGameObject;
    }

    public void SetClusterGameObject(GameObject clusterGameObject)
    {
        this.clusterGameObject = clusterGameObject;
    }
}

public class OuterCluster : NeuronCluster
{
    private readonly List<NeuronCluster> neuronClusters;
    private GameObject clusterGameObject;

    public OuterCluster()
    {
        neuronClusters = new List<NeuronCluster>();
    }

    public OuterCluster(List<NeuronCluster> neuronClusters)
    {
        this.neuronClusters = neuronClusters;
    }

    public override List<ConvexNeuron> GetNeurons()
    {
        var neurons = new List<ConvexNeuron>();
        foreach (var cluster in neuronClusters) neurons.AddRange(cluster.GetNeurons());

        return neurons;
    }

    public List<NeuronCluster> GetNeuronClusters()
    {
        return neuronClusters;
    }

    public NeuronCluster GetClusterWithId(int id)
    {
        var clusters = GetNeuronClusters();
        foreach (var cluster in clusters)
            if (cluster.GetClusterId() == id)
                return cluster;
        return null;
    }

    public void AddNeuronCluster(NeuronCluster neuronCluster)
    {
        foreach (var cluster in neuronClusters)
            if (cluster.GetClusterCentroid() == neuronCluster.GetClusterCentroid())
                //don't add neuron cluster if it is already in the list
                return;
        neuronClusters.Add(neuronCluster);
    }
}

public class InnerCluster : NeuronCluster
{
    private readonly List<ConvexNeuron> neurons;
    private GameObject clusterGameObject;

    public InnerCluster()
    {
        neurons = new List<ConvexNeuron>();
    }

    public InnerCluster(List<ConvexNeuron> neurons)
    {
        this.neurons = neurons;
    }

    public override List<ConvexNeuron> GetNeurons()
    {
        return neurons;
    }

    public void AddNeuron(ConvexNeuron neuron)
    {
        neurons.Add(neuron);
    }

    public Vector3 GetClusterCentroid()
    {
        var neurons = GetNeurons();
        var centroid = new Vector3(0, 0, 0);
        foreach (var n in neurons) centroid += n.GetPosition();
        centroid /= neurons.Count;
        return centroid;
    }
}

public class ClusterProperty : MonoBehaviour
{
    public bool selected;
}

public class MoveResult
{
    public MoveResult(GameObject go, Vector3 start, Vector3 end)
    {
        Go = go;
        Start = start;
        End = end;
    }

    public Vector3 Start { get; }

    public Vector3 End { get; }

    public GameObject Go { get; }
}