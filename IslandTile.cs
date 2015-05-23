using System;
using System.Collections;
using System.Collections.Generic;
using BenTools.Mathematics;


public class IslandTile
{
    public Vector center;//cell center positon
    public HashSet<IslandTileEdge> edges=new HashSet<IslandTileEdge>();//edges belong to this tile
    public HashSet<IslandTileCorner> corners = new HashSet<IslandTileCorner>();//all corners in this tile
    public HashSet<Vector> neighbors = new HashSet<Vector>();//store neighber tiles' center positon
    public Dictionary<double, IslandTileCorner> angle_corner = new Dictionary<double, IslandTileCorner>();
    public bool iswater = false;
    public bool isshore = false;
    public bool hasriver = false;
    public int biome=3;//biome type: 1 beach 2 grassland 3 rain forest 4 bare moutain(rock and etc) 5 snow
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



    //get location information of a pixel. return two corners which are in triangle's bottom
    public List<IslandTileCorner> pixel_loation(Vector pixel)
    {
        //vector between center and given pixel
        Vector e0 = new Vector(pixel.data[0] - center.data[0], pixel.data[1] - center.data[1]);
        //e0.data[0]=pixel.data[0]-center.data[0];
        //e0.data[1]=pixel.data[1]-center.data[1];
        //double theta0 = Math.Acos(e0.data[0] / (Math.Sqrt(e0.data[0] * e0.data[0] + e0.data[1] * e0.data[1])));
        double theta0 = Math.Atan2(e0.data[1], e0.data[0]);
        if (theta0 < 0)
            theta0 += 2 * Math.PI;
        //System.Console.WriteLine("theta0=  " + theta0);
        foreach(var c in corners)
        {
            //Vector between each corner and center 
            Vector e1 = new Vector(c.position.data[0] - center.data[0], c.position.data[1] - center.data[1]);
           // e1.data[0] = c.position.data[0] - center.data[0];
            //e1.data[1] = c.position.data[1] - center.data[1];
            //double theta1 = Math.Acos(e1.data[0] / (Math.Sqrt(e1.data[0] * e1.data[0] + e1.data[1] * e1.data[1])));
            //get it's angle
            double theta1 = Math.Atan2(e1.data[1], e1.data[0]);
            if (theta1 < 0)
                theta1 += 2 * Math.PI;
            angle_corner[theta1] = c;
        }
        /*foreach(var item in angle_corner.Keys)
        {
            System.Console.WriteLine(item);
        }*/
        List<double> angle = new List<double>(angle_corner.Keys);
        //sort angles
        angle.Sort();
        /*for(int i=0;i<angle.Count;i++)
        {
            System.Console.WriteLine(angle[i]);
        }*/
        //find which triangle this pixel is in
        int forward=0,backward=0;
        for(int i=0;i<angle.Count;i++)
        {
            if(theta0>=angle[angle.Count-1])
            {
                forward=angle.Count-1;
                backward=0;
                break;
            }
            else if(theta0<=angle[i])
            {
                if(i==0)
                {
                    forward = 0;
                    backward = angle.Count - 1;
                    break;
                }
                forward = i;
                backward = i - 1;
                break;
            }
            
        }
        //System.Console.WriteLine("theta1= "+angle[forward] + "    " + angle[backward]);
        List<IslandTileCorner> bottomcorners = new List<IslandTileCorner>();
        bottomcorners.Add(angle_corner[angle[forward]]);
        bottomcorners.Add(angle_corner[angle[backward]]);
        return bottomcorners;
    }



    //calculate elevation for each pixel
    public float PixelElevation(Vector p, List<IslandTileCorner> bottomcorners)
    {
        //note: e0=u*e1+v*e2
        IslandTileCorner c1 = bottomcorners[0];
        IslandTileCorner c2 = bottomcorners[1];
        Vector e0 = new Vector(p.data[0] - center.data[0], p.data[1] - center.data[1]);
        //e0.data[0] = p.data[0] - center.data[0];
        //e0.data[1] = p.data[1] - center.data[1];
        Vector e1 = new Vector(c1.position.data[0] - center.data[0], c1.position.data[1] - center.data[1]);
        //e1.data[0] = c1.position.data[0] - center.data[0];
        //e1.data[1] = c1.position.data[1] - center.data[1];
        Vector e2 = new Vector(c2.position.data[0] - center.data[0], c2.position.data[1] - center.data[1]);
        //e2.data[0] = c2.position.data[0] - center.data[0];
        //e2.data[1] = c2.position.data[1] - center.data[1];
        double temp = e1.data[0] * e2.data[1] - e1.data[1] * e2.data[0];
        double u = (e0.data[0] * e2.data[1] - e0.data[1] * e2.data[0]) / temp;
        double v = (e0.data[1] * e1.data[0] - e0.data[0] * e1.data[1]) / temp;
        //System.Console.WriteLine(u + " " + v);
        float pixelelevation;
        pixelelevation = (float)(elevation + (c1.elevation - elevation) * u + (c2.elevation - elevation) * v);
        return pixelelevation;
    }



}
