using UnityEngine;
using System.Collections;

public class NearestNeighbor : MonoBehaviour {
    
    //README!!!!!
    //As I said in last meeting, we may use Kd-tree and nearest neighbor search to solve the problem of
    //finding the voronoi cell for any given point. Compared to O(N^2) complexity of brute-force method, this method is only O(N*logN).
    //What you need to do is build a kd-tree with the set of random points at the very beginning, 
    //and run nearest neighbor search on the kd-tree when you want to know  the voronoi cell of some arbirary point.
    
    //So here I offer one solution with the help of a nice library called "alglib".  
    //This is a small example of using alglib.kdtree to search the nearest neighbor of a given point
    //Although I write it in a Unity script, the alglib has NO relation with Unity. I do so ONLY for convenience (now I am working in Unity).
    //To use the library alglib, you need to:
    //1. Go to http://www.alglib.net/download.php and download the free version for C#.
    //2. Add alglibnet2.dll (in downloaded directory) as a reference of your project.
    //3. You now should be able to use alglib. You DO NOT NEED TO import anything by "using ...".
    //4. Copy and paste the following Start() function and test it.
    //5. There should be a manual(html) in downloaded directory so just follow it for further usage.

	void Start ()
    {   
        //the array points are the dataset for query (in your case, points are the centers of voronoi cells)
        //here I have 4 2D points
        double[,] points = new double[,] { { 0, 0 }, { 0, 1 }, { 1, 0 }, { 1, 1 } };
        //we could attach an integer tag to each point above
        //In your case, you could let the tag be the index of your cell list
        int[] tags = new int[] { 0, 1, 2, 3 };
        //nx is the dimension of point. Because we use 2D point, nx = 2
        int nx = 2;
        //ny is an optional parameter, here it is useless.
        int ny = 0;
        //normtype indicates how you want to measure distance, normtype = 2 means we use euclidean distance
        int normtype = 2;
        //alglib.kdtree is the data structure of KD-tree in this library. Note now kdt is empty
        alglib.kdtree kdt;
        //this function build a kdtree with above provided data. After this, kdt will not be empty anymore
        alglib.kdtreebuildtagged(points, tags, nx, ny, normtype, out kdt);
        
        //I now want to know who is the nearest neighbor of (0,0.9)...
        double[] queryPt = new double[] { 0, 0.9 };
        //here the thrid parameter is 1, indicating that we only want to know the nearest neighbor
        //if you want to know both the nearest and the second nearest neighbors, set it to 2
        int k = alglib.kdtreequeryknn(kdt, queryPt, 1);
        //I create an array to hold the result, I don't have to initialize it because the library will do that for me
        double[,] resultPt = new double[0, 0];
        //now I can get the result. It is easy to see the nearest neighbor of (0,0.9) is (0,1) in this example
        //The size of resultPt should be 1x2 now. resultPt[0,0] is x-coordinate = 0, and resultPt[0,1] is y-coordinate = 1
        alglib.kdtreequeryresultsx(kdt, ref resultPt);

        //now I want to know the tag of (0,1) so I can use the tag to get access to the corresponding voronoi cell
        //once again, I create an array to hold the result (no need to initialize)
        int[] resultTag = new int[0];
        //now I can get the tag. The size of resultTag should be 1 now. And resultTag[0] should be 1
        alglib.kdtreequeryresultstags(kdt, ref resultTag);  
	}
}
