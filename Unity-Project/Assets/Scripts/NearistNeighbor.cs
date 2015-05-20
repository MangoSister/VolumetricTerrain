using System;
using System.Collections;
using System.Collections.Generic;
using BenTools.Mathematics;


public class NearistNeighbor
{
    public static int FindNearistTile(List<Vector> centers,Vector testpoint)
    {
        double[,] points = new double[centers.Count, 2];
        for(int i=0;i<centers.Count;i++)
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
        alglib.kdtree kdt;
        alglib.kdtreebuildtagged(points, tags, nx, ny, normtype, out kdt);
        double[] queryPt = new double[] { testpoint.data[0], testpoint.data[1] };
        int k = alglib.kdtreequeryknn(kdt, queryPt, 1);
        double[,] resultPt = new double[0, 0];
        alglib.kdtreequeryresultsx(kdt, ref resultPt);
        int[] resultTag = new int[0];
        alglib.kdtreequeryresultstags(kdt, ref resultTag);
        return resultTag[0];


    }
        

}

