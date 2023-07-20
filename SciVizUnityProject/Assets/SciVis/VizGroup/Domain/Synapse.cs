using System;

[Serializable]
public class Synapse{
    public int SourceId{ get; set; }
    public int TargetId{ get; set; }
    public int Weight{ get; set; }
}