using System;
using System.Collections.Generic;

[Serializable]
public class clusterSplinesData
{
    public List<string> ChildIds { get; set; }
    public List<Spline> Splines { get; set; }
    public int flagExploded{get; set;} 

    public clusterSplinesData(List<string> childIdsConstructorArg, List<Spline> splinesConstructorArg, int flagExplodedConstructorArg)
    {
        ChildIds = childIdsConstructorArg;
        Splines = splinesConstructorArg;
        flagExploded = flagExplodedConstructorArg;
    }
}