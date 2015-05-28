//PGRTerrain: Procedural Generation and Rendering of Terrain
//DH2323 Course Project in KTH
//IslandTileEdge.cs
//Yang Zhou: yanzho@kth.se
//Yanbo Huang: yanboh@kth.se
//Huiting Wang: huitingw@kth.se
//2015.5

using System.Collections;
using System.Collections.Generic;
using BenTools.Mathematics;

namespace PGRTerrain.Generation
{
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
}
