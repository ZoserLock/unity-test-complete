using System;
using System.Collections.Generic;
using UnityEngine;

// Set of classes that will hold the data that comes from the web services.
[Serializable]
public class GetRequestResponse
{
    public bool Success = false;
}

[Serializable]
public class GetModelResponse : GetRequestResponse
{
    public string ObjUrl;
    public string MtlUrl;
    public string TextureUrl;
}

[Serializable]
public class GetShovelsResponse : GetRequestResponse
{
    public List<ShovelData> Shovels;
}

[Serializable]
public class GetShovelInfoResponse : GetRequestResponse
{
    public List<Report> Reports;
}

[Serializable]
public class ShovelData
{
    public int ID;
    public string Name;
    public Vector3 Position;

    // Derived Data filled later.
    public Report Report = null;
}

[Serializable]
public class ShovelState
{
    public DateTime Start;
    public DateTime End;
    public string Name;
    public string Color;
}

[Serializable]
public class Report
{
    public int ShovelID;
    public double Performance;
    public double PlannedPerformance;
    public List<ShovelState> LastStates;
}


