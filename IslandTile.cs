using System;
using System.Collections;
using System.Collections.Generic;
using BenTools.Mathematics;
//using BenTools.Data;
//using Microsoft.DirectX;

public class IslandTile
{
    public Vector center;//cell center positon
    public HashSet<IslandTileEdge> edges=new HashSet<IslandTileEdge>();//edges belong to this tile
    public HashSet<IslandTileCorner> corners = new HashSet<IslandTileCorner>();//all corners in this tile
    public HashSet<Vector> neighbors = new HashSet<Vector>();//store neighber tiles' center positon
    public bool iswater = false;
    public bool isshore = false;
    public float elevation=float.MaxValue;
    public int width;//these two factor are for boundary infinity
    public int hight;
    //constructor
    //public static HashSet<IslandTileCorner> total_corners = new HashSet<IslandTileCorner>();
    public IslandTile(Vector c,VoronoiGraph g,int w,int h)
    {
        width = w;
        hight = h;
        center = c;
        AddEdges(g);
    }
    public void AddEdges(VoronoiGraph g)
    {
        foreach (VoronoiEdge e in g.Edges)
        {
            if ((e.LeftData == center) || (e.RightData == center))
            {
                bool isInf = (e.VVertexA == Fortune.VVInfinite) ||
                     (e.VVertexB == Fortune.VVInfinite);
                if (isInf) { continue; }
                AddCorners(e);
                //IslandTileEdge ed = new IslandTileEdge(e);
                //ed.neighbors.Add(this);
                edges.Add(new IslandTileEdge(e));
                //find neighbor tile of this tile
                if (e.LeftData == center) { neighbors.Add(e.RightData); }
                else { neighbors.Add(e.LeftData); }
            }
        }
    }
    public void AddCorners(VoronoiEdge e)
    {
        IslandTileCorner ca;
        if (IslandTileCorner.Index.ContainsKey(e.VVertexA))
        {
            ca = IslandTileCorner.Index[e.VVertexA];
        }
        else
        {//conflicts of out of boundaries
          //and through this progress, all tile near border will be seted as water
            //(IslandTile.water is false by default)
            if (e.VVertexA.data[0] <=0)
            { 
                e.VVertexA.data[0] = 0;
              //this.iswater = true;
            }
            if (e.VVertexA.data[0] >= (width-1))
            {
                e.VVertexA.data[0] = (width-1);
                //this.iswater = true;
            }
            if (e.VVertexA.data[1] <= 0)
            {
                e.VVertexA.data[1] = 0;
                //this.iswater = true;
            }
            if (e.VVertexA.data[1] >= (hight-1))
            {
                e.VVertexA.data[1] = (hight-1);
                //this.iswater = true;
            }
            ca = new IslandTileCorner(e.VVertexA); 
        }
        IslandTileCorner cb;
        if (IslandTileCorner.Index.ContainsKey(e.VVertexB))
        {
            cb = IslandTileCorner.Index[e.VVertexB];
        }
        else
        {
            if (e.VVertexB.data[0] <=0)
            { 
                e.VVertexB.data[0] = 0;
                //this.iswater = true;
            }
            if (e.VVertexB.data[0] >= (width - 1))
            {
                e.VVertexB.data[0] = (width - 1);
                //this.iswater = true;
            }
            if (e.VVertexB.data[1] <=0)
            { 
                e.VVertexB.data[1] = 0;
                //this.iswater = true;
            }
            if (e.VVertexB.data[1] >= (hight - 1))
            {
                e.VVertexB.data[1] = (hight - 1);
                //this.iswater = true;
            }
            cb = new IslandTileCorner(e.VVertexB); 
        }
        ca.adjacent.Add(cb);
        cb.adjacent.Add(ca);
        ca.protrudes.Add(e);
        cb.protrudes.Add(e);
        ca.touches.Add(this);
        cb.touches.Add(this);
        corners.Add(ca);
       // total_corners.Add(ca);
        corners.Add(cb);
        //total_corners.Add(cb);


    }
}
