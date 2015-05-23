using System.Collections;
using System.Collections.Generic;
using BenTools.Mathematics;
using Microsoft.DirectX;
public class IslandTileEdge
{
    public VoronoiEdge edge;
    public IslandTileCorner cornera;
    public IslandTileCorner cornerb;

    //constructor of IslandTileEdge
    public IslandTileEdge(VoronoiEdge e)
    {
        edge = e;
        cornera = IslandTileCorner.Index[e.VVertexA];
        cornerb = IslandTileCorner.Index[e.VVertexB];
    }
}

