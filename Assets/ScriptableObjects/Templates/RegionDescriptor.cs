using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class RegionDescriptor {

    public uint id;
    public String name;
    public float x;
    public float y;

    public Vector2 center;
    public List<Edge> frontiers;
    [NonSerialized]
    public List<RegionDescriptor> adjacentRegion;

}
