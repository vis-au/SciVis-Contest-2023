using System;
using System.Collections.Generic;
using System.Linq;
using SciColorMaps.Portable;
using UnityEngine;


//importing this to easily print the properties of an object, without prior knowledge of the property names

public class VisualizationHandler : MonoBehaviour, IBrainObserver
{
    private const int maxVertices = 64998;

    // The colormap defined above works on the numerical range 0-1 and return RGB values in the range 0-255.
    private const float colorMinValue = 0.0f;
    private const float colorMaxValue = 1.0f;

    private const float sizeMinValue = 0.0f; // Sizes are defined in the range specified here.
    private const float sizeMaxValue = 0.4f; // 1.0f

    public Shader shaderForNeuronShaders;
    public Shader shaderForConnections;
    public Material materialForNeuronShaders;
    public Material materialForConnections;
    public List<NeuronPosition> neurons;
    private BrainClusterVisualizer brainClusterVisualizer;
    private GameObject brainGo;
    private BrainSubject brainSubject;

    private GameObject connectionsGo;
    private Material material;

    public ColorMap
        NeuronsColorMap; // Colormap defaults to the viridis colorscale, see https://github.com/ar1st0crat/SciColorMaps for instructions.

    public ColorMap SynapsesColorMap; // 

    public (float, float) SynapsesColorDomain => (colorMinValue, colorMaxValue);

    public (float, float) NeuronsColorDomain
    {
        get
        {
            var neuronColorAttribute = brainSubject.GetSpec().NeuronColorEncoding;
            return neuronColorAttribute.GetColorMinMax();
        }
    }

    public GameObject NeuronsGo { get; set; }


    private void Awake()
    {
        brainGo = gameObject; //new GameObject("Brain");

        brainGo.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);

        var sub = GetComponentInParent<IBrainSubject>();
        CreateColorScheme((BrainSubject)sub);

        materialForNeuronShaders = new Material(shaderForNeuronShaders);
        materialForConnections = new Material(shaderForConnections);

        materialForNeuronShaders.mainTexture = Resources.Load("sphere-texture") as Texture2D;
        materialForNeuronShaders.enableInstancing = true;
        materialForNeuronShaders.SetColor("_BrushColor", Color.red);


        var meshForNeuronShaders = GenerateMesh();
        NeuronsGo = CreateGameObjectWithMesh(meshForNeuronShaders, materialForNeuronShaders,
            "NeuronShadersMesh",
            brainGo);
        var meshForConnections = GenerateMesh();
        connectionsGo =
            CreateGameObjectWithMesh(meshForConnections, materialForConnections, "ConnectionsMesh",
                brainGo);

        brainClusterVisualizer = GetComponent<BrainClusterVisualizer>();

        neurons = ReadBrainPositions();
        var no_neurons = new List<Neuron>();
        var noneAttribute = new NeuronAttribute();
        noneAttribute.value = NeuronAttributeType.None;
        var filters = new List<NeuronFilter>();
        var localColorScale = false;

        UpdateNeuronShaders(NeuronsGo, neurons, no_neurons, noneAttribute, noneAttribute, filters,
            localColorScale);
        sub.Attach(this);
        brainSubject = gameObject.GetComponentInParent<BrainSubject>();
    }

    public void ObserverUpdateNeurons(IBrainSubject subject)
    {
        var new_neurons = (subject as BrainSubject)._neurons;

        var size_encoding = (subject as BrainSubject).spec.NeuronSizeEncoding;
        var color_encoding = (subject as BrainSubject).spec.NeuronColorEncoding;
        CreateColorScheme(subject as BrainSubject);
        var filters = (subject as BrainSubject).spec.Filters;
        var localColorScale = (subject as BrainSubject).spec.LocalColorScale;

        UpdateNeuronShaders(NeuronsGo, neurons, new_neurons, color_encoding, size_encoding, filters,
            localColorScale);
        
    }

    public void ObserverUpdateSelection(IBrainSubject brainSubject)
    {
        //Do nothing
    }

    public void ObserverUpdateTerrain(IBrainSubject brainSubject)
    {
        // Do nothing
    }


    public void ObserverUpdateSynapses(IBrainSubject subject)
    {
        var connections = (subject as BrainSubject)._synapses;

        var new_neurons = (subject as BrainSubject)._neurons;
        var filters = (subject as BrainSubject).spec.Filters;
    }

    public void ObserverUpdateSplines(IBrainSubject subject)
    {
        var connections = (subject as BrainSubject)._splines;
        var new_neurons = (subject as BrainSubject)._neurons;
        var filters = (subject as BrainSubject).spec.Filters;
        UpdateConnectionsWithBundledEdges(connectionsGo, neurons, connections);
    }


    public void ObserverUpdateConvexHull(IBrainSubject subject)
    {
        brainClusterVisualizer.Draw(neurons, brainGo);
    }
    //If we choose to display the brain in a different scale, then the size range should likely also be modified.

    public float normaliseValue(float value, float from1, float to1, float from2, float to2)
    {
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }


    private Mesh GenerateMesh()
    {
        var vertices = new Vector3[maxVertices];

        var normals = new Vector3[maxVertices];
        for (var i = 0; i < maxVertices; i++) normals[i] = Vector3.up;

        var colors = new Color[maxVertices];
        for (var i = 0; i < maxVertices; i++) colors[i] = new Color(1, 0, 0);

        var indices = new int[maxVertices];
        for (var i = 0; i < maxVertices; ++i) indices[i] = i;

        var mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.colors = colors;
        mesh.SetIndices(indices, MeshTopology.Points, 0);
        return mesh;
    }

    private GameObject CreateGameObjectWithMesh(Mesh mesh, Material materialToApply,
        string name = "GeneratedMesh",
        GameObject parent = null)
    {
        var meshGameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Destroy(meshGameObject.GetComponent<Collider>());
        meshGameObject.GetComponent<MeshFilter>().mesh = mesh;
        meshGameObject.GetComponent<MeshRenderer>().sharedMaterial = materialToApply;
        meshGameObject.name = name;
        meshGameObject.transform.parent = parent.transform;
        meshGameObject.transform.localPosition = Vector3.zero;
        meshGameObject.transform.localRotation = Quaternion.identity;
        meshGameObject.transform.localScale = Vector3.one;
        return meshGameObject;
    }

    private void UpdateNeuronShaders(GameObject neuronsGo, List<NeuronPosition> neurons,
        List<Neuron> neuronAttributes,
        NeuronAttribute neuronColorAttribute,
        NeuronAttribute neuronSizeAttribute, List<NeuronFilter> neuronFilters, bool localColorScale)
    {
        var mesh = neuronsGo.GetComponent<MeshFilter>().mesh;
        mesh.Clear();

        var defaultColorValue = Color.grey;
        var outsideFilterColor = Color.grey;
        outsideFilterColor.a = 0.5f;
        var defaultSizeValue = new Vector2(0.5f, 0.5f);

        var colors = new Color[neurons.Count];
        var sizes = new Vector2[neurons.Count];


        var maxLocalAttribute = 0f;
        var minLocalAttribute = 0f;
        if (neuronAttributes.Count > 0)
        {
            var localAttributes = neuronAttributes.Select(x => x.GetValueOf(neuronColorAttribute))
                .ToList();
            maxLocalAttribute = localAttributes.Max();
            minLocalAttribute = localAttributes.Min();
        }

        var filterAttributes =
            neuronFilters.Where(x => x.Checked).Select(x => x.Attribute).ToList();

        var normals = new Vector3[neurons.Count];
        for (var i = 0; i < neurons.Count && i < maxVertices; i++) normals[i] = Vector3.up;

        var vertices = new Vector3[neurons.Count];
        for (var i = 0; i < neurons.Count && i < maxVertices; i++)
            vertices[i] = neurons[i].Position;

        var indices = new int[neurons.Count];
        for (var i = 0; i < neurons.Count && i < maxVertices; i++) indices[i] = i;

        for (var i = 0; i < neuronAttributes.Count && i < maxVertices; i++)
        {
            var neuron_i = neuronAttributes[i];
            if (neuron_i is null)
            {
                // Needed because streaming might cause some slight problems
                colors[i] = defaultColorValue;
                sizes[i] = defaultSizeValue;
                continue;
            }

            var insideFilters = true;
            for (var j = 0; j < filterAttributes.Count; j++)
            {
                var filter = neuronFilters[j];
                var value = neuron_i.GetValueOf(filter.Attribute);
                insideFilters = insideFilters && value >= filter.Min && value <= filter.Max;
            }

            if (insideFilters)
            {
                if (neuronColorAttribute.value == NeuronAttributeType.None)
                {
                    colors[i] = defaultColorValue;
                }
                else
                {
                    var value = neuron_i.GetValueOf(neuronColorAttribute);
                    var colorBytes = NeuronsColorMap[value];
                    colors[i] = new Color(colorBytes[0] / 255f, colorBytes[1] / 255f,
                        colorBytes[2] / 255f, 0.0f);
                }
            }
            else
            {
                colors[i] = outsideFilterColor;
            }


            if (neuronSizeAttribute.value == NeuronAttributeType.None)
            {
                sizes[i] = defaultSizeValue;
            }
            else
            {
                var minMax = neuronSizeAttribute.GetMinMax();
                var value = neuron_i.GetValueOf(neuronSizeAttribute);

                var sizeValue = normaliseValue(value, minMax.Item1, minMax.Item2, sizeMinValue,
                    sizeMaxValue);
                sizes[i] = new Vector2(sizeValue, sizeValue);
            }
        }

        var uvs = new Vector2[vertices.Length];

        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.colors = colors;
        mesh.uv = uvs;
        mesh.uv2 = sizes;
        mesh.SetIndices(indices, MeshTopology.Points, 0);
    }

    private void UpdateConnectionsWithBundledEdges(GameObject connectionsGo,
        List<NeuronPosition> neurons,
        List<Spline> connections)
    {
        float scaleFactor = 1;

        var bundled_vertices = new List<NeuronPosition>();
        var connections_bundled = new List<Synapse>();

        var idCounter = 0;

        //Use this snippet to update colors

        var minWeight = 100;
        var maxWeight = 500;
        var weightsOfColors = new List<int>();
        var edgeInfo = new List<Tuple<Vector3, Vector3>>();

        for (var i = 0; i < connections.Count; i++)
        {
            if (connections[i].splinesList == "") continue;
            var parts = connections[i].splinesList.Split(';');
            var weightOfThisSpline = connections[i].Weight;

            if (weightOfThisSpline > maxWeight) maxWeight = weightOfThisSpline;
            if (weightOfThisSpline < minWeight) minWeight = weightOfThisSpline;

             var theseCoordinates1 = parts[0].Split(",");
             var xString1 = theseCoordinates1[0].Replace("(", "").Replace(")", "")
                    .Replace("\"", "");
             var yString1 = theseCoordinates1[1].Replace("(", "").Replace(")", "")
                    .Replace("\"", "");
             var zString1 = theseCoordinates1[2].Replace("(", "").Replace(")", "")
                    .Replace("\"", "");
                var x1 = float.Parse(xString1) / scaleFactor;
                var y1 = float.Parse(yString1) / scaleFactor;
                var z1 = float.Parse(zString1) / scaleFactor;
            

             theseCoordinates1 = parts[parts.Length-1].Split(",");
             xString1 = theseCoordinates1[0].Replace("(", "").Replace(")", "")
                    .Replace("\"", "");
             yString1 = theseCoordinates1[1].Replace("(", "").Replace(")", "")
                    .Replace("\"", "");
             zString1 = theseCoordinates1[2].Replace("(", "").Replace(")", "")
                    .Replace("\"", "");
                var x2  = float.Parse(xString1) / scaleFactor;
                var y2 = float.Parse(yString1) / scaleFactor;
                var z2 = float.Parse(zString1) / scaleFactor;

                Vector3 v1 = new Vector3(x1,y1,z1); Vector3 v2 = new Vector3(x2,y2,z2);
             

            bool addSpline = true;
            foreach (Tuple<Vector3, Vector3> tu in edgeInfo){
                Vector3 startVector = tu.Item1;
                Vector3 endVector = tu.Item2;
                if ((startVector == v2 && endVector == v1) || (startVector == v1 && endVector == v2)){
                    addSpline = false;
                }
            }
            if (addSpline == true){
                var t = Tuple.Create<Vector3, Vector3>(v1, v2);
                edgeInfo.Add(t);
                for (var partIndex = 0; partIndex < parts.Length; partIndex += 1)
                {
                    var theseCoordinates = parts[partIndex].Split(",");

                    var xString = theseCoordinates[0].Replace("(", "").Replace(")", "")
                        .Replace("\"", "");
                    var yString = theseCoordinates[1].Replace("(", "").Replace(")", "")
                        .Replace("\"", "");
                    var zString = theseCoordinates[2].Replace("(", "").Replace(")", "")
                        .Replace("\"", "");

                    var Id = idCounter;

                    var x = float.Parse(xString) / scaleFactor;
                    var y = float.Parse(yString) / scaleFactor;
                    var z = float.Parse(zString) / scaleFactor;

                    bundled_vertices.Add(new NeuronPosition(Id, new Vector3(x, y, z),
                        weightOfThisSpline));
                    weightsOfColors.Add(weightOfThisSpline);
                    if (partIndex < parts.Length - 1)
                    {

                        var SourceId = Id;
                        float TargetId = Id + 1;
                        float Weight = weightOfThisSpline;
                        bool add = true;
                        foreach(Synapse s in connections_bundled){
                            if((s.SourceId == SourceId && s.TargetId == TargetId)
                            || 
                            (s.SourceId == TargetId && s.TargetId == SourceId)){
                                add = false;
                                Debug.Log("False");
                            }
                        }
                        if(add){
                        connections_bundled.Add(new Synapse
                            { SourceId = SourceId, TargetId = (int)TargetId, Weight = (int)Weight });
                        }

                    }

                    idCounter += 1;
                }
            }
        }

        neurons = bundled_vertices;
        var mesh = connectionsGo.GetComponent<MeshFilter>().mesh;
        mesh.Clear();

        var normals = new Vector3[neurons.Count];
        for (var i = 0; i < neurons.Count && i < maxVertices; i++) normals[i] = Vector3.up;

        // Debug.Log("Max Weight: " + maxWeight);
        // Debug.Log("Min Weight: " + minWeight);
        SynapsesColorMap = new ColorMap("hot");
        var colors = new Color[weightsOfColors.Count];
        for (var i = 0; i < weightsOfColors.Count && i < maxVertices; i++)
        {
            var colorValue = colorMaxValue -
                             normaliseValue(weightsOfColors[i], minWeight, maxWeight, colorMinValue,
                                 colorMaxValue);
            // Debug.Log(colorValue);
            var colorBytes = SynapsesColorMap[colorValue];
            colors[i] = new Color(colorBytes[0] / 255f, colorBytes[1] / 255f, colorBytes[2] / 255f,
                1.0f);
        }

        brainSubject.MakeBrainLegend();

        var connectionCount = connections_bundled.Count;
        var indices = new int[connectionCount * 2];

        var j = 0;
        // int max_id = 0;
        // int min_id = connections_bundled[1].SourceId;
        int connectionsHalf = (int) connectionCount;
        for (var i = 0; i < connectionsHalf; i++, j += 2)
        {
            indices[j] = connections_bundled[i].SourceId;
            indices[j + 1] = connections_bundled[i].TargetId;

            // if(indices[j]>max_id){
            //     max_id = indices[j];
            // }            
            // if(indices[j+1]>max_id){
            //     max_id = indices[j+1];
            // }

            // if(indices[j]<min_id){
            //     min_id = indices[j];
            // }            
            // if(indices[j+1]<min_id){
            //     min_id = indices[j+1];
            // }
        }

        var vertices = new Vector3[neurons.Count];
        for (var i = 0; i < neurons.Count; i++) vertices[i] = neurons[i].Position;

        var uv2 = new Vector2[neurons.Count];
        for (var i = 0; i < neurons.Count; i++)
            uv2[i] = new Vector2(neurons[i].Area / 50000f, neurons[i].Area / 50000f);

        var uv = new Vector2[neurons.Count];
        for (var i = 0; i < neurons.Count; i++)
            uv[i] = new Vector2(i, i);

        //var uvs = new Vector2[vertices.Length];
        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.colors = colors;
        mesh.uv = uv;
        mesh.uv2 = uv2; //send the widths of the shader as input in the uv2 variable.
       // mesh.uv3 = uv3;
        mesh.SetIndices(indices, MeshTopology.Lines, 0);
    }

    private List<NeuronPosition> ReadBrainPositions()
    {
        var csv = (TextAsset)Resources.Load("neurons", typeof(TextAsset));
        var n = Environment.NewLine;

        var lines = new List<string>(csv.text.Split(n.ToCharArray()));
        //float scaleFactor = 187;
        var neurons = new List<NeuronPosition>();

        for (var i = 1; i < lines.Count; i++)
        {
            if (lines[i] == "") continue;

            var parts = lines[i].Split(',');

            var Id = int.Parse(parts[0]) -
                     1; // -1 since in the neurons csv file the neurons have IDs from 1, whereas in the database they have IDs from 0
            var x = float.Parse(parts[1]);
            var y = float.Parse(parts[2]);
            var z = float.Parse(parts[3]);
            var area = int.Parse(parts[4]);

            neurons.Add(new NeuronPosition(Id, new Vector3(x, y, z), area));
        }

        var neuronsCentroid = new Vector3(0, 0, 0);
        foreach (var neuron in neurons) neuronsCentroid += neuron.Position;
        neuronsCentroid /= neurons.Count;

        foreach (var neuron in neurons) neuron.Position -= neuronsCentroid;

        return neurons;
    }

    private void CreateColorScheme(BrainSubject subject)
    {
        var (min, max) = getColorEncodingMinMax(subject.GetSpec().NeuronColorEncoding, subject._neurons,subject.GetSpec().LocalColorScale);

        NeuronsColorMap = ColorMapBuilder.UseDivergentColorScale(subject)
            ? ColorMapBuilder.CreateDivergingColorMap(min, ColorMapBuilder.TARGET_CALCIUM_LEVEL,
                max)
            : ColorMapBuilder.CreateSequentialColorMap(min, max);
    }

    private (float, float) getColorEncodingMinMax(NeuronAttribute neuronAttribute, List<Neuron> neurons,
        bool useLocalColorScale)
    {
        if (!useLocalColorScale) return neuronAttribute.GetColorMinMax();

        var localAttributes = neurons.Select(x => x.GetValueOf(neuronAttribute)).ToList();
        var maxLocalAttribute = localAttributes.Max();
        var minLocalAttribute = localAttributes.Min();

        return (minLocalAttribute, maxLocalAttribute);
    }
}