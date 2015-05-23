using System;
using System.Collections;
using System.Collections.Generic;
using BenTools.Mathematics;

public class Island
{
    //-------------Data---------------------------
    public int relaxation;//relaxation times
    public int width;//screen width 
    public int hight;//screen hight
    public int num_of_rivers;
    public int num_of_centers;//number of centers
    public int num1;//corner num of main river
    public int num2;// corner num of sub river
    public int countm = 0;// for remenbering the num of corner of a main river branch
    public int counts = 0;//for remenbering the num of corner of a sub river branch
    public float maxelevation = float.MaxValue;
    public NearestNeighbor NN;//class for searching nearest center
    public HashSet<IslandTile> ocean = new HashSet<IslandTile>();//store ocean tiles
    public HashSet<IslandTile> land = new HashSet<IslandTile>();//store land tiles
    public HashSet<IslandTileCorner> shore = new HashSet<IslandTileCorner>();//store corners in shore
    //public HashSet<IslandTileCorner> totalcorners = new HashSet<IslandTileCorner>();//total corners
    public Dictionary<Vector, IslandTile> Tiles = new Dictionary<Vector, IslandTile>();//store all tiles
    public List<Vector> centers = new List<Vector>();//stores all tiles' positions
    public List<Vector> landcenters = new List<Vector>();//stores land tiles' postions
    public List<River> allrivers = new List<River>();//all rivers
    Random sub_rnd = new Random();//decide whether there is a subriver
    //class constractor
    public Island(int w,int h,int r,int numc,int numr)
    {
        width = w;
        hight = h;
        relaxation = r;
        num_of_centers = numc;
        num_of_rivers = numr;
        num1 = Math.Max(w / 50, h / 50);
        num2 = num1 / 2;
        centers = random_centers(width, hight, num_of_centers);
        VoronoiGraph vg = Fortune.ComputeVoronoiGraph(centers);//run voronoi diagram algorithm
        for (int i = 0; i < centers.Count; i++)//Initialize and store IslandTiles
        {
            Tiles[centers[i]] = new IslandTile(centers[i], vg,width,hight);
        }
        //call improveRandomPoints function "relaxation" times

        for (int re = 0; re < relaxation; re++)
        {
            centers = improveRandomPoints(Tiles, centers);
            VoronoiGraph vGraph = Fortune.ComputeVoronoiGraph(centers);
            Tiles = new Dictionary<Vector, IslandTile>();
            for (int j = 0; j < centers.Count; j++)
            {
                Tiles[centers[j]] = new IslandTile(centers[j], vGraph,width,hight);
            }
        }
        NN = new NearestNeighbor(centers);//builded kdtree
         foreach (var item in Tiles.Values)
         {
            if (item.center.data[0] < (width / 10) || item.center.data[0] > (width - width / 10) ||
                 item.center.data[1] < (width / 10) || item.center.data[1] > (width - width / 10))
                {
                    item.iswater = true;
                    item.elevation = 0;
                    foreach (var c in item.corners)
                    {
                        c.elevation = 0;
                        // totalcorners[c.position] = c;
                        //water.Add(c);
                    }
                    ocean.Add(item);
                }
                else
                    land.Add(item);

            }
        //spreading ocean area
        int waterspreadcount = 0;
        foreach (var item in Tiles.Values)
        {
            if (!item.iswater)
            {
                foreach (var i in item.neighbors)
                {
                    if (Tiles[i].iswater)
                    {
                        item.iswater = true;
                        item.elevation = 0;
                        foreach (var c in item.corners)
                        {
                            c.elevation = 0;
                            //totalcorners[c.position] = c;
                            //water.Add(c);
                        }
                        ocean.Add(item);
                        land.Remove(item);
                        waterspreadcount++;

                    }
                    if (waterspreadcount > (num_of_centers / 3))
                        break;
                }
            }
            if (waterspreadcount > (num_of_centers / 3))
                break;
        }
        //remove one tile island
        foreach (var item in Tiles.Values)
        {
            float sum_of_elevation = 0;
            foreach (var c in item.corners)
            {
                sum_of_elevation += c.elevation;
            }
            if (sum_of_elevation == 0)
            {
                item.iswater = true;
                item.elevation = 0;
                ocean.Add(item);
                land.Remove(item);
            }
        }
        //-----calculate coastline------------------------
        foreach (var item in land)
        {
            foreach (var c in item.corners)
                if (c.elevation == 0)
                {
                    shore.Add(c);
                    item.isshore = true;
                }
        }
        //calculate elevation for corners
        foreach (var t in Tiles.Values)
        {
            if (!t.iswater)
            {
                float sum_elevation = 0;
                foreach (var c in t.corners)
                {
                    foreach (var s in shore)
                    {
                        float elevation = (float)Math.Sqrt(Math.Pow((c.position - s.position).data[0], 2) + Math.Pow(
                            (c.position - s.position).data[1], 2));
                        if (c.elevation > elevation)
                            c.elevation = elevation;
                    }
                    sum_elevation += c.elevation;
                   // totalcorners[c.position] = c;
                }
                t.elevation = sum_elevation / t.corners.Count;

            }
        }
        //store total corners
        /*foreach(var item in Tiles.Values)
        {
            foreach (var c in item.corners)
                totalcorners.Add(c);
        }*/
        //landcenters
        foreach(var item in land)
        {
            landcenters.Add(item.center);

        }
        storebiome();//
       
        //from now on, all data of a tile are generated. 

    }
    
    //-------------Methods-----------------------------
    //----------------------1.get initial random points --------------------
    public List<Vector> random_centers(int width, int hight, int num_of_centers)
    {
        Random rnd = new Random();
        HashSet<int> points = new HashSet<int>();
        List<Vector> poi = new List<Vector>();
        while (points.Count < num_of_centers)
        {
            points.Add(rnd.Next(0, width * hight - 1));
        }
        
        foreach (int item in points)
        {
            int rows = item / width;
            int cols = item % width;
            //System.Console.WriteLine(rows);
            poi.Add(new Vector(rows, cols));//get row and col indexs

        }
        return poi;

    }


    //-----------2.improveRandomPoints(use the centroid of corners to replace inital centers)---
    public List<Vector> improveRandomPoints(Dictionary<Vector, IslandTile> Tiles, List<Vector> c)
    {
        List<Vector> improvepoint_list = new List<Vector>();
        for (int i = 0; i < c.Count; i++)
        {
            Vector improvepoint = new Vector(0, 0);//the point in the center of a tile
            foreach (IslandTileCorner cor in Tiles[c[i]].corners)
            {
                improvepoint += cor.position;//add all corners together and then calculate the centriod 
            }
            improvepoint.data[0] = (int)(improvepoint.data[0] / Tiles[c[i]].corners.Count);
            improvepoint.data[1] = (int)(improvepoint.data[1] / Tiles[c[i]].corners.Count);

            improvepoint_list.Add(improvepoint);
        }
        return improvepoint_list;
    }
    //----------------------------3.generate rivers-----------------------------
    //this function will return a list of rivers,and the num of river is decided by you in your main function
   /* public List<River> generationofRivers(int num)
    {
        List<River> totalriver = new List<River>();
        Random rnd = new Random();
        HashSet<IslandTileCorner> startpoints = new HashSet<IslandTileCorner>();
        while(startpoints.Count<num)
        {
            int index=rnd.Next(0, landcenters.Count);
            
            foreach (var c in Tiles[landcenters[index]].corners)
            {
              if (c.elevation != 0)
              {
                startpoints.Add(c);
               //System.Console.WriteLine("haha");
                break;
              }
            }    
        }
        foreach (var item in startpoints)
        {
            River r = new River();
            r.generateriver(item);
            totalriver.Add(r);
        }
        return totalriver;
            
            
    }*/

    public List<River> generationofRivers()
    {
        //generate startpoints in shore corners
        List<IslandTileCorner> lshore=new List<IslandTileCorner>();
        
        foreach(var s in shore)
            lshore.Add(s);
        HashSet<IslandTileCorner> startpoints = new HashSet<IslandTileCorner>();
        Random r = new Random();
        while(startpoints.Count<num_of_rivers)
        {
            int index = r.Next(0, lshore.Count - 1);
            startpoints.Add(lshore[index]);
        }
        foreach(var rs in startpoints)
        {
            
            River root=new River(rs);
            //generation_Mainriver(highest);
            generation_Mainriver(root);
            allrivers.Add(root);
            countm = 0;//main river's numofcorners
            counts = 0;//each sub river's numofcorners

        }
        return allrivers;

    }

    public void generation_Mainriver(River rc)
    {
        IslandTileCorner highest = rc.data;
        IslandTileCorner secondhigh = rc.data;
        foreach(var c in rc.data.adjacent)
        {
          if (c.elevation > highest.elevation)
             highest = c;
        }
        foreach(var c in rc.data.adjacent)
        {
          if ((c.elevation > secondhigh.elevation) && (c != highest))
             secondhigh = c;
        }

        if (rc.right == null)
        { 
            rc.right = new River(highest);
            rc.right.father = rc;
        }
        countm++;
        if(countm<num1)
            generation_Mainriver(rc.right);
        double whetherleft = sub_rnd.NextDouble();

        if (whetherleft < 0.2)
        {
            if (rc.left == null)
            {
                rc.left = new River(secondhigh);
                rc.left.father = rc;
            }
            generation_Subriver(rc.left);
            counts = 0;
        }
    }
    public void generation_Subriver(River sr)
    {
        //to make it easy I don't concider the subriver of a subriver
        IslandTileCorner highest = sr.data;
        foreach (var c in sr.data.adjacent)
        {
            if (c.elevation > highest.elevation)
                highest = c;
        }
        if (sr.right == null)
        {
            sr.right = new River(highest);
            sr.right.father = sr;
        }
        counts++;
        if(counts<num2)
            generation_Subriver(sr.right);
    }
    // get biome type of each tiles. this varible is defined in IslandTile.cs
    //to use this function, you'd better first calculate the maxelevation of pixels
    public void storebiome()
    {
        foreach(var item in Tiles.Values)
        {
            if(item.iswater)
            {
                item.biome = 0;//ocean
            }
            if(item.isshore)
            {
                item.biome = 1;//beach
            }
            if(item.elevation>=(0.9*maxelevation))
            {
                //tiles above 0.9maxelevation supposed to be snow
                item.biome = 5;
            }
            if((item.elevation<0.9*maxelevation)&&(item.elevation>=0.6*maxelevation))
            {
                item.biome = 4;//rock
            }
            if(item.elevation<0.6)
            {
                item.biome = 3;//grassland
            }
            if(item.hasriver)
            {
                item.biome = 2;//forest
            }

        }
    }
    public float getfinalelevation(Vector p)
    {
        float pelevation;//elevation of this pixel
        //find nearist center
        int tag = NN.FindNearestTile(p);
        Vector neighbercenter = centers[tag];
        //water's elevation is zero
        if (Tiles[neighbercenter].iswater)
        {
            pelevation = 0;
        }
        else
        {
            //pixel _location() finds two bottom corners of the triangle where this pixel in
            List<IslandTileCorner> bottom = Tiles[neighbercenter].pixel_loation(p);
            //PixelElevation() gets the final elevation
            pelevation = Tiles[neighbercenter].PixelElevation(p, bottom);
        }
        return pelevation;
    }

    
    

}