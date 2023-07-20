using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class NeuronPosition{
    public int Id { get; set; }
    public Vector3 Position { get; set; }
    public int Area { get; set; }

    public NeuronPosition(int Id , Vector3 Position, int Area)
    {
        this.Id = Id;
        this.Position = Position;
        this.Area = Area;
    }
}