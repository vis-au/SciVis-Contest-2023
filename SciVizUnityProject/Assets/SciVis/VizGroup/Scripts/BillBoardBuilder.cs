using IATK;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Threading;

public class BillBoardBuilder: MonoBehaviour{
    private IRepository _repository = Repository.Instance;
    private List<NeuronTimestep> _points;

    private View oldView;

    public List<Gradient> gradients;

    public float normaliseValue(float value, float from1, float to1, float from2, float to2)
    {
        return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }
    private View billBoardBuilder(NeuronAttribute valueAttribute){
        
        
        float max = _points.Select(x => x.Value).Max();
        float min = _points.Select(x => x.Value).Min();

        float maxZOrder = _points.Select(x => x.ZOrder).Max();
        float maxTimeStep = _points.Select(x => x.TimeStep).Max();
        Gradient g1 = new Gradient();
        GradientColorKey[] gck = new GradientColorKey[2];
        gck[0] = new GradientColorKey(Color.white, min);
        gck[1] = new GradientColorKey(Color.magenta, max);
        g1.colorKeys = gck;
        Gradient g2 = new Gradient();
        gck[0] = new GradientColorKey(Color.white, min);
        gck[1] = new GradientColorKey(Color.red, max);
        g2.colorKeys = gck;
        Gradient g3 = new Gradient();
        gck[0] = new GradientColorKey(Color.white, min);
        gck[1] = new GradientColorKey(Color.blue, max);
        g3.colorKeys = gck;
        List<Gradient> gs = new List<Gradient>{g1,g2,g3};
        gradients = gs;
        

        ViewBuilder vb = new ViewBuilder (MeshTopology.Lines, "BillBoard").
                    initialiseDataView(_points.Count).
                    setDataDimension(_points.Select(x => (float)x.TimeStep / maxTimeStep).ToArray(), ViewBuilder.VIEW_DIMENSION.Z).
                    setDataDimension(_points.Select(x => normaliseValue((float)x.Value,min,max,0,1)).ToArray(), ViewBuilder.VIEW_DIMENSION.Y).
                    setColors(_points.Select(x => gs[x.ZOrder].Evaluate(x.Value)).ToArray()).
                    createIndicesConnectedLineTopology(_points.Select(x => (float)x.ZOrder).ToArray());
                    //setSize(csvds["Base"].Data).
                    
        // Use the "IATKUtil" class to get the corresponding Material mt
        Material mt = new Material(Shader.Find("IATK/LineAndDotsShader"));
        mt.SetFloat("_Size", 0.15f);
        mt.SetFloat("_MinSize", 0.01f);
        mt.SetFloat("_MaxSize", 0.05f);
        // Create a view builder with the point topology
        View view = vb.updateView().apply(gameObject, mt);

        if (oldView != null)
        {
            oldView.gameObject.Destroy();
        }

        oldView = view;
        return view;

    }

    void Awake(){
          // For now the endpoint only supports a single cluster at a time and only for the Calcium simulation
           //await drawBillBoard(new HashSet<int>{}, 0, SimulationType.calcium, new NeuronAttribute{value = NeuronAttributeType.Calcium});
           float max = 1;
           float min = 0;
           
           Gradient g1 = new Gradient();
           GradientColorKey[] gck = new GradientColorKey[2];
           gck[0] = new GradientColorKey(Color.white, min);
           gck[1] = new GradientColorKey(Color.magenta, max);
           g1.colorKeys = gck;
           Gradient g2 = new Gradient();
           gck[0] = new GradientColorKey(Color.white, min);
           gck[1] = new GradientColorKey(Color.red, max);
           g2.colorKeys = gck;
           Gradient g3 = new Gradient();
           gck[0] = new GradientColorKey(Color.white, min);
           gck[1] = new GradientColorKey(Color.blue, max);
           g3.colorKeys = gck;
           List<Gradient> gs = new List<Gradient>{g1,g2,g3};
           gradients = gs;
           this.GetComponentInParent<SelectionPanelBehavior>().MakeLegend();
    }

    public async Task drawBillBoard(HashSet<int> ids, int granularity , SimulationType simulation_id, NeuronAttribute attribute)
    { 
        List<NeuronTimestep> avg = await _repository.GetBillBoard(new BillBoardQuery(100,granularity,simulation_id,attribute, "avg", ids.ToList<int>()));
        avg.ForEach(x => x.ZOrder = 0);
        _points = avg;
        List<NeuronTimestep> max =  await _repository.GetBillBoard(new BillBoardQuery(100,granularity,simulation_id,attribute, "max",ids.ToList<int>()));
        Debug.Log(max.Count);
        max.ForEach(x => x.ZOrder = 1);
        _points.AddRange(max);
        List<NeuronTimestep> min = await _repository.GetBillBoard(new BillBoardQuery(100,granularity,simulation_id,attribute, "min", ids.ToList<int>()));
        Debug.Log(min.Count);
        min.ForEach(x => x.ZOrder = 2);
        _points.AddRange(min);
        billBoardBuilder(new NeuronAttribute{value= attribute.value});
    }
}

