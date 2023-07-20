using Microsoft.MixedReality.Toolkit.UX;
using UnityEngine;

public class TerrainEncodingMenuBehaviour : MonoBehaviour
{
    private GameObject colorPosDropdown;
    private GameObject aggregationTypeDropdown;
    private GameObject clusterLevelDropdown;
    private IBrainSubject brainSubject;
    // Start is called before the first frame update
    void Awake()
    {
        colorPosDropdown = transform.Find("Canvas/Vertical/ColorHorizontal/ColorPosAttribute").gameObject;
        aggregationTypeDropdown = transform.Find("Canvas/Vertical/AggregationType").gameObject;
        clusterLevelDropdown = transform.Find("Canvas/Vertical/ClusterLevel").gameObject;
        brainSubject = GetComponentInParent<IBrainSubject>();
        }



    public void SetColorPosDropdownValue(NeuronAttributeType value)
    {
        if (colorPosDropdown == null)
        {
            colorPosDropdown = transform.Find("Canvas/Vertical/ColorHorizontal/ColorPosAttribute").gameObject;
        }
        colorPosDropdown.GetComponentInChildren<TMPro.TextMeshProUGUI>().SetText(value.ToString());
        brainSubject?.SetTerrainEncoding(new NeuronAttribute(){value = value});
    }

    public void SetAggregationTypeValue(AggregationType value)
    {
        if (aggregationTypeDropdown == null)
        {
            aggregationTypeDropdown = transform.Find("Canvas/Vertical/AggregationType").gameObject;
        }
        aggregationTypeDropdown.GetComponentInChildren<TMPro.TextMeshProUGUI>().SetText(value.ToString());
        brainSubject?.SetTerrainAggregationType(value);
        
    }

    public void SetClusterLevelValue(int value)
    {
        if (clusterLevelDropdown == null)
        {
            clusterLevelDropdown = transform.Find("Canvas/Vertical/ClusterLevel").gameObject;
        }
        clusterLevelDropdown.GetComponentInChildren<TMPro.TextMeshProUGUI>().SetText(value.ToString());
        brainSubject?.SetTerrainClusterLevel(value);
    }
}
