using System;

[Serializable]
public class Spline{
    public string SourceId{get; set;}
    public string TargetId{get; set;}
    public int Weight{get; set;}
    public string splinesList{get; set;}
}