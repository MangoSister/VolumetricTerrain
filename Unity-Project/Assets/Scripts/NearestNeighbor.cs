//PGRTerrain: Procedural Generation and Rendering of Terrain
//DH2323 Course Project in KTH
//NearestNeighbor.cs
//Yang Zhou: yanzho@kth.se
//Yanbo Huang: yanboh@kth.se
//Huiting Wang: huitingw@kth.se
//2015.5

//Note: the method of generating KD-tree is based on the free version library of ALGlib
using System;
using System.Collections;
using System.Collections.Generic;
using BenTools.Mathematics;

namespace PGRTerrain.Generation
{
    public class NearestNeighbor
    {
        public alglib.kdtree kdt;
        public NearestNeighbor(List<Vector> centers)
        {
            double[,] points = new double[centers.Count, 2];
            for (int i = 0; i < centers.Count; i++)
            {
                points[i, 0] = centers[i].data[0];
                points[i, 1] = centers[i].data[1];
            }
            int[] tags = new int[centers.Count];
            for (int i = 0; i < centers.Count; i++)
            {
                tags[i] = i;
            }
            int nx = 2;
            int ny = 0;
            int normtype = 2;
            alglib.kdtreebuildtagged(points, tags, nx, ny, normtype, out kdt);
        }
        public int FindNearestTile(Vector testpoint)
        {

            double[] queryPt = new double[] { testpoint.data[0], testpoint.data[1] };
            alglib.kdtreequeryknn(kdt, queryPt, 1);
            double[,] resultPt = new double[0, 0];
            alglib.kdtreequeryresultsx(kdt, ref resultPt);
            int[] resultTag = new int[0];
            alglib.kdtreequeryresultstags(kdt, ref resultTag);
            return resultTag[0];


        }


    }
}
