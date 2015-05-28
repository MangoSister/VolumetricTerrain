//PGRTerrain: Procedural Generation and Rendering of Terrain
//DH2323 Course Project in KTH
//Island.cs
//Yang Zhou: yanzho@kth.se
//Yanbo Huang: yanboh@kth.se
//Huiting Wang: huitingw@kth.se
//2015.5

using System;
using System.Collections;
using System.Collections.Generic;
using BenTools.Mathematics;
using UnityEngine;

using SRandom = System.Random;

namespace PGRTerrain.Generation
{
    public class Island
    {
        //-------------Data---------------------------        
        public int width;//screen width 
        public int height;//screen hight
        public float _maxElevation;
        public int relaxationTime;//relaxation times
        public int num_of_centers;//number of centers
        
        public int num_of_rivers;       
        public int _mainStreamLength;//corner num of main river
        public int _subStreamLength;// corner num of sub river
        public float _riverSplitFreq;
        
        private int countm = 0;// for remenbering the num of corner of a main river branch
        private int counts = 0;//for remenbering the num of corner of a sub river branch
        
        private NearestNeighbor NN;//class for searching nearest center
        public HashSet<IslandTile> ocean = new HashSet<IslandTile>();//store ocean tiles
        public HashSet<IslandTile> land = new HashSet<IslandTile>();//store land tiles
        private HashSet<IslandTileCorner> shore = new HashSet<IslandTileCorner>();//store corners in shore
        //public HashSet<IslandTileCorner> totalcorners = new HashSet<IslandTileCorner>();//total corners
        private Dictionary<Vector, IslandTile> Tiles = new Dictionary<Vector, IslandTile>();//store all tiles
        private List<Vector> centers = new List<Vector>();//stores all tiles' positions
        private List<Vector> landcenters = new List<Vector>();//stores land tiles' postions
        
        public List<River> rivers;//each river is a binary tree through this you can travesing all river corners by tree's order        

        private SRandom _rndGen; //random generator
        //class constractor
        public Island(int width, int height, 
                    int relaxTime, int centerNum, 
                    int riverNum, float maxElevation, 
                    float mainStreamLengthRatio, //typical: 0.02
                    float subStreamLengthRatio, //typical: 0.5
                    float riverSplitFreq, //typical: 0.2
                    int seed = 0)
        {
            this.width = width;
            this.height = height;
            this.relaxationTime = relaxTime;
            this.num_of_centers = centerNum;
            this.num_of_rivers = riverNum;
            this._maxElevation = maxElevation;

            if (mainStreamLengthRatio < 0f || mainStreamLengthRatio > 0.2f)
                throw new ArgumentOutOfRangeException("ratio must be between 0 and 0.2");
            _mainStreamLength = (int) Math.Floor(Math.Max(width, height) * mainStreamLengthRatio);

            if (subStreamLengthRatio < 0f || subStreamLengthRatio > 1f)
                throw new ArgumentOutOfRangeException("ratio must be between 0 and 1");
            _subStreamLength = (int) Math.Floor(_mainStreamLength * subStreamLengthRatio);
           
            if (_riverSplitFreq < 0f || _riverSplitFreq > 1f)
                throw new ArgumentOutOfRangeException("frequency must be between 0 and 1");
            _riverSplitFreq = riverSplitFreq;

            _rndGen = new SRandom(seed);
            centers = random_centers(width, height, num_of_centers);
            VoronoiGraph vg = Fortune.ComputeVoronoiGraph(centers);//run voronoi diagram algorithm
            for (int i = 0; i < centers.Count; i++)//Initialize and store IslandTiles
            {
                Tiles[centers[i]] = new IslandTile(centers[i], vg, width, height);
            }
            //call improveRandomPoints function "relaxation" times

            for (int re = 0; re < relaxationTime; re++)
            {
                centers = improveRandomPoints(Tiles, centers);
                VoronoiGraph vGraph = Fortune.ComputeVoronoiGraph(centers);
                Tiles = new Dictionary<Vector, IslandTile>();
                for (int j = 0; j < centers.Count; j++)
                {
                    Tiles[centers[j]] = new IslandTile(centers[j], vGraph, width, height);
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
                        float minDistToShore = float.MaxValue;
                        foreach (var s in shore)
                        {
                            float distToShore = (float)Math.Sqrt((c.position - s.position).data[0] * (c.position - s.position).data[0] +
                                                                (c.position - s.position).data[1] * (c.position - s.position).data[1]);
                            if (minDistToShore > distToShore)
                                minDistToShore = distToShore;
                        }
                        c.elevation = minDistToShore * minDistToShore / _maxElevation;
                        c.elevation = Math.Min(c.elevation, _maxElevation);
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
            foreach (var item in land)
            {
                landcenters.Add(item.center);

            }
            rivers = GenerateRivers();//generate rivers
            foreach (var ri in rivers)
            {
                River.findDischarge(ri);//get discharge for every corner
            }
            //put discharge information in it's tile
            foreach (var kc in River.keeprivercorners)
            {
                foreach (var t in kc.touches)
                {
                    t.hasriver = true;
                    foreach (var c in t.corners)
                    {
                        if (c.position == kc.position)
                        {
                            c.discharge = kc.discharge;

                        }
                        break;
                    }
                }
            }

            StoreBiome();//set biome type for each tile

            //from now on, all data of a tile are generated. 

        }

        //-------------Methods-----------------------------
        //----------------------1.get initial random points --------------------
        public List<Vector> random_centers(int width, int hight, int num_of_centers)
        {
           
            HashSet<int> points = new HashSet<int>();
            List<Vector> poi = new List<Vector>();
            while (points.Count < num_of_centers)
            {
                points.Add(_rndGen.Next(0, width * hight - 1));
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

        public List<River> GenerateRivers()
        {
            List<River> allrivers = new List<River>(); //all rivers

            //generate startpoints in shore corners
            List<IslandTileCorner> lshore = new List<IslandTileCorner>();

            foreach (var s in shore)
                lshore.Add(s);
            HashSet<IslandTileCorner> startpoints = new HashSet<IslandTileCorner>();
        
            while (startpoints.Count < num_of_rivers)
            {
                int index = _rndGen.Next(0, lshore.Count - 1);
                bool valid = false;
                foreach(var neighbor in lshore[index].adjacent)
                {
                    if (neighbor.elevation > 0f)
                        valid = true;
                }
                if (valid)
                    startpoints.Add(lshore[index]);
            }
            foreach (var rs in startpoints)
            {

                River root = new River(rs);
                //generation_Mainriver(highest);
                GenerateMainRiver(root);
                allrivers.Add(root);
                countm = 0;//main river's numofcorners
                counts = 0;//each sub river's numofcorners

            }
            return allrivers;

        }

        private void GenerateMainRiver(River rc)
        {
            IslandTileCorner maxima = rc.data;
            bool existMaxima = false;
            foreach (var c in rc.data.adjacent)
            {
                if (c.elevation > maxima.elevation)
                {
                    maxima = c;
                    existMaxima = true;
                }
            }

            if (!existMaxima)
                return;

            if (rc.right == null)
            {
                rc.right = new River(maxima);
                rc.right.father = rc;
            }
            countm++;
            if (countm < _mainStreamLength)
                GenerateMainRiver(rc.right);

            double doSplit = _rndGen.NextDouble();
            if (doSplit > _riverSplitFreq)
                return;

            IslandTileCorner secondMaxima = rc.data;
            bool existSecondMaxima = false;
            foreach (var c in rc.data.adjacent)
            {
                if ((c.elevation > secondMaxima.elevation) && (c != maxima))
                {
                    secondMaxima = c;
                    existSecondMaxima = true;
                }
            }
            if (!existSecondMaxima)
                return;

            if (rc.left == null)
            {
                rc.left = new River(secondMaxima);
                rc.left.father = rc;
            } 
            GenerateSubRiver(rc.left);
            counts = 0;
            
        }
        public void GenerateSubRiver(River sr)
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
            if (counts < _subStreamLength)
                GenerateSubRiver(sr.right);
        }
        // get biome type of each tiles. this varible is defined in IslandTile.cs
        //to use this function, you'd better first calculate the maxelevation of pixels
        public void StoreBiome()
        {
            foreach (var item in Tiles.Values)
            {
                item.biome[BiomeType.Snow] = Mathf.InverseLerp(0.7f * _maxElevation, 1.0f * _maxElevation, item.elevation);
                item.biome[BiomeType.BareRock] = MathHelper.TriangularInvLerp(0.3f * _maxElevation, 1.0f * _maxElevation, item.elevation);
                item.biome[BiomeType.GrassLand] = MathHelper.TriangularInvLerp(0.05f * _maxElevation, 0.4f * _maxElevation, item.elevation);
                item.biome[BiomeType.Beach] = Mathf.InverseLerp(0.1f * _maxElevation, 0.0f, item.elevation);
                if (item.hasriver && item.elevation < 0.4f * _maxElevation)
                {
                    item.biome[BiomeType.RainForest] = 1f;
                }
                float sum = 0;
                foreach (var component in item.biome.Values)
                    sum += component;
                var keyList = new List<BiomeType>(item.biome.Keys);
                foreach (var key in keyList)
                    item.biome[key] /= sum;
           }      
        }
        public float GetElevation(Vector p)
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

        public Dictionary<BiomeType, float> GetBiome(Vector p)
        {
            int tag = NN.FindNearestTile(p);
            Vector neighbercenter = centers[tag];
            return Tiles[neighbercenter].biome;
        }


    }

}