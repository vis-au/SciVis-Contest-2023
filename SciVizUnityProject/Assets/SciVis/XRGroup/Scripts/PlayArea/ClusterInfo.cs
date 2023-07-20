using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClusterInfo : MonoBehaviour
{
    private int id;
    private int clusterLevel;
    private float meanColorAttributeValue;

    public List<Neuron> neurons = new List<Neuron>();

    public int ID
    {
        get => id;
        set => id = value;
    }

    public int ClusterLevel
    {
        get => clusterLevel;
        set => clusterLevel = value;
    }

    public float MeanColorAttributeValue
    {
        get => meanColorAttributeValue;
        set => meanColorAttributeValue = value;
    }
}
