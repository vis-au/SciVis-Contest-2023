using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using IATK;
using SciVis.XRGroup;
using SciVis.XRGroup.Scripts.Tools;
using UnityEngine;

public class TerrainViewSelectionHandler : MonoBehaviour
{
    private HashSet<int> nodeIds;
    private int clusterId;
    private GameObject playArea;
    private Color[] initColors;
    public void init(HashSet<int> nodeIds, int clusterId, Color[] initColors)
    {
        this.nodeIds = nodeIds;
        this.clusterId = clusterId;
        playArea = GetComponentInParent<PlayAreaUtil>().gameObject;
        this.initColors = initColors;
    }

    public HashSet<int> GetNodeIds()
    {
        return nodeIds;
    }

    public int GetClusterId()
    {
        return clusterId;
    }

    public GameObject getPlayArea()
    {
        return playArea;
    }

    /*private void OnTriggerEnter(Collider other)
    {
        AdjustBrush brush = other.gameObject.GetComponentInParent<AdjustBrush>();
        if (brush == null)
        {
            return;
        }
        brush.BrushInBrain(nodeIds, playArea);
    }*/

    public void Brush(bool brushingNotErasing)
    {
        if (brushingNotErasing)
        {
            GetComponent<View>().SetColors(initColors.Select(x => Color.red).ToArray());
        }
        else
        {
            GetComponent<View>().SetColors(initColors);
        }
    }
}
