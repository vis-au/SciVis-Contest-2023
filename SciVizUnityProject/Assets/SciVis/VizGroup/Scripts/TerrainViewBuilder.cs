using IATK;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;


public class TerrainViewBuilder: MonoBehaviour, IBrainObserver
{
    public Gradient gradient;
    public Gradient gradientSelection;

    public List<List<NeuronTimestep>> _points;

    public float normaliseValue(float value, float from1, float to1, float from2, float to2)
    {
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }
    private void terrainViewBuilder(List<List<NeuronTimestep>> points, NeuronAttribute valueAttribute)
    {

        _points = points;
        float max = -1f/0f;
        float min = 1/0f;
        // Use min max computed here for local scale, use global values of attribute for global scale
        for (int i=0; i < points.Count; i++){
            for (int j=0; j < points[i].Count; j++){
                if (points[i][j].Value > max){
                    max = points[i][j].Value;
                }
                if (points[i][j].Value < min){
                    min = points[i][j].Value;
                }
            }
        }
        int z_orders = points.Count;

        int granularity = points[0][0].Granularity;
        Dictionary<int,float> selectedClusters = GetComponentInParent<BrainSubject>().clustersContainingSelection(granularity);
        BrainSubject sub = GetComponentInParent<BrainSubject>();
    
        for (int i = 0; i < points.Count; i++)
        {
            float maxZOrder = points[i].Select(x => x.ZOrder).Max();
            float maxTimeStep = points[i].Select(x => x.TimeStep).Max();
            ;
            ViewBuilder vb = new ViewBuilder (MeshTopology.Lines, "Terrainview").
                        initialiseDataView(points[i].Count).
                        //setDataDimension(points.Select(x => (float)x.ZOrder / (maxZOrder + 1)).ToArray(), ViewBuilder.VIEW_DIMENSION.X). // We need +1 to take 0 into account
                        setDataDimension(points[i].Select(x => (float)x.TimeStep / maxTimeStep).ToArray(), ViewBuilder.VIEW_DIMENSION.X).
                        setDataDimension(points[i].Select(x => (float)normaliseValue(x.Value, min, max, 0, 1)).ToArray(), ViewBuilder.VIEW_DIMENSION.Y).
                        setColors(points[i].Select(x =>
                        {
                            if (selectedClusters.ContainsKey(x.Id))
                            {
                                return gradientSelection.Evaluate(selectedClusters[x.Id]);
                            }
                            return gradient.Evaluate(x.Value);
                        }).ToArray()).
                        createIndicesConnectedLineTopology(points[i].Select(x => (float)x.ZOrder).ToArray());
                        
            // Use the "IATKUtil" class to get the corresponding Material mt
            Material mt = new Material(Shader.Find("IATK/LineAndDotsShader"));
            mt.SetFloat("_MinSize", 0.01f);
            mt.SetFloat("_MaxSize", 0.05f);
            // Create a view builder with the point topology
            View view = vb.updateView().apply(gameObject, mt);
            
            //For selection
            BoxCollider collider = view.gameObject.AddComponent<BoxCollider>();
            collider.center = new Vector3(0.5f, 0.5f, 0f);
            collider.size = new Vector3(1, 1, 0.01f);
            TerrainViewSelectionHandler handler = view.gameObject.AddComponent<TerrainViewSelectionHandler>();
            handler.init(sub.neuronsInClusters(points[i][0].Id, granularity), points[i][0].Id, points[i].Select(x => gradient.Evaluate(x.Value)).ToArray());
            
            float position = ((float)points[i][0].ZOrder)/((float)z_orders);
            view.transform.localPosition = new Vector3(0,0,position);
        }
    }


    void Awake(){
        BrainSubject sub = GetComponentInParent<BrainSubject>();
        sub.Attach(this);
        
        float max = -1f/0f;
        float min = 1/0f;
        Gradient g = new Gradient();
        GradientColorKey[] gck = new GradientColorKey[2];
        gck[0] = new GradientColorKey(Color.white, 0f);
        gck[1] = new GradientColorKey(new Color32(77,146,33,255), 1f);
        g.colorKeys = gck;
        gradient = g;
        
        Gradient g2 = new Gradient();
        GradientColorKey[] gck2 = new GradientColorKey[2];
        gck2[0] = new GradientColorKey(new Color32(255,126,126,255), 0f);
        //gck2[0] = new GradientColorKey(Color.white, 0f);
        gck2[1] = new GradientColorKey(Color.red, 1f);

        InitPosition();

        g2.colorKeys = gck2;
        gradientSelection = g2;
    }


    public void InitPosition()
    {
        transform.parent.localPosition = Vector3.zero;
        transform.parent.localRotation = Quaternion.identity;
        BrainSubject sub = GetComponentInParent<BrainSubject>();
        Vector3 brainPos = sub.transform.GetComponentInChildren<VisualizationHandler>(true).transform.position;
        Vector3 cameraPos = Camera.main.transform.position;

        transform.parent.parent.position = new Vector3((brainPos.x + cameraPos.x) / 2, cameraPos.y - 0.5f, (brainPos.z + cameraPos.z) / 2);
        transform.parent.parent.forward = new Vector3(brainPos.x - cameraPos.x, 0, brainPos.z - cameraPos.z);
    }

    public void ObserverUpdateTerrain(IBrainSubject subject)
    {
        foreach (Transform child in transform)
        {
            child.gameObject.Destroy();
        }
        List<List<NeuronTimestep>> points = (subject as BrainSubject)._points;
        NeuronAttribute field = (subject as BrainSubject)._field;
        terrainViewBuilder(points, field);
        
    }

    public void ObserverUpdateSynapses(IBrainSubject subject)
    {
        //Do nothing
    }

    public void ObserverUpdateSplines(IBrainSubject subject)
    {
        //Do nothing
    }

    public void ObserverUpdateNeurons(IBrainSubject subject)
    {
        //Do nothing
    }

    public void ObserverUpdateConvexHull(IBrainSubject subject)
    {
        //: )
    }

    public void ObserverUpdateSelection(IBrainSubject brainSubject)
    {
        if (_points == null)
        {
            return;
        }
        int granularity = _points[0][0].Granularity;
        Dictionary<int, float> selectedClusters =
            GetComponentInParent<BrainSubject>().clustersContainingSelection(granularity);

        for (int i = 0; i< transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            View view = child.GetComponent<View>();
            view.SetColors(_points[i].Select(x =>
            {
                if (selectedClusters.ContainsKey(x.Id))
                {
                    return gradientSelection.Evaluate(selectedClusters[x.Id]);
                }
                return gradient.Evaluate(x.Value);
            }).ToArray());
        }
    }
}

