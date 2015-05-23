using System;
using System.Collections;
using System.Collections.Generic;
using BenTools.Mathematics;

namespace Island_test
{
    class Program
    {

        static void Main(string[] args)
        {
            //there is a example to show you how to generate island,rivers and pixel's elevation
            //width=500,hight=500,relaxtion=2,centers=700,rivers=5
            Island island = new Island(500, 500, 2, 700, 5);
            //each river is a binary tree through this you can travesing all river corners by tree's order
            List<River> rivers;
            rivers = island.generationofRivers();
            foreach (var r in rivers)
            {
                River.findDischarge(r);//get discharge for every corner
                System.Console.WriteLine();
                System.Console.WriteLine();
            }
            //put discharge information in it's tile
            foreach (var r in River.keeprivercorners)
            {
                foreach (var t in r.touches)
                {
                    t.hasriver = true;
                    foreach (var c in t.corners)
                    {
                        if (c.position == r.position)
                        {
                            c.discharge = r.discharge;

                        }
                        break;
                    }
                }
            }
            //Vector testpoint = new Vector(499,499);
            
                //get elevation for every pixel 

            for (int i = 0; i < island.hight; i++)
            {
                for (int j = 0; j < island.width; j++)
                {
                    float h;
                    Vector testpoint = new Vector(i, j);
                    h = island.getfinalelevation(testpoint);
                }

            }
           
               




                // System.Console.WriteLine();
                //System.Console.WriteLine(Math.Atan2(testpoint.data[1] - island.centers[tag].data[1], testpoint.data[0] - island.centers[tag].data[0]));
                //System.Console.WriteLine(Math.Atan2(bottom[0].position.data[1] - island.centers[tag].data[1], bottom[0].position.data[0] - island.centers[tag].data[0]));
                //System.Console.WriteLine(Math.Atan2(bottom[1].position.data[1] - island.centers[tag].data[1], bottom[1].position.data[0] - island.centers[tag].data[0]));
                //System.Console.WriteLine(pelevation);
                //System.Console.WriteLine(island.Tiles[neighbercenter].center.data[0]+" "+island.Tiles[neighbercenter].center.data[1]+" "+island.Tiles[neighbercenter].elevation);
                //System.Console.WriteLine(bottom[0].position.data[0]+" "+bottom[0].position.data[1]+" "+bottom[0].elevation);
                //System.Console.WriteLine(bottom[1].position.data[0]+" "+ bottom[1].position.data[1]+" "+ bottom[1].elevation);
                /* Vector testpoint = new Vector(30, 20);
                 int tag = NearistNeighbor.FindNearistTile(island.centers, testpoint);
                 Vector neighbercenter = island.centers[tag];
                 System.Console.WriteLine(tag + " " + neighbercenter.data[0] + " " + neighbercenter.data[1]);
                 */
                /* using (System.IO.StreamWriter file1 = new System.IO.StreamWriter("E:\\MyFile1.txt"))
                 using (System.IO.StreamWriter file2 = new System.IO.StreamWriter("E:\\MyFile2.txt"))
                 using (System.IO.StreamWriter file3 = new System.IO.StreamWriter("E:\\MyFile3.txt"))
                 using (System.IO.StreamWriter file4 = new System.IO.StreamWriter("E:\\MyFile4.txt"))
                 using (System.IO.StreamWriter file7 = new System.IO.StreamWriter("E:\\MyFile7.txt"))
                 using (System.IO.StreamWriter file8 = new System.IO.StreamWriter("E:\\MyFile8.txt"))
                 using (System.IO.StreamWriter file5 = new System.IO.StreamWriter("E:\\MyFile5.txt"))
                 using (System.IO.StreamWriter file6 = new System.IO.StreamWriter("E:\\MyFile6.txt"))
                 using (System.IO.StreamWriter river1x = new System.IO.StreamWriter("E:\\river1x.txt"))
                 using (System.IO.StreamWriter river1y = new System.IO.StreamWriter("E:\\river1y.txt"))
                 using (System.IO.StreamWriter river2x = new System.IO.StreamWriter("E:\\river2x.txt"))
                 using (System.IO.StreamWriter river2y = new System.IO.StreamWriter("E:\\river2y.txt"))
                 using (System.IO.StreamWriter river3x = new System.IO.StreamWriter("E:\\river3x.txt"))
                 using (System.IO.StreamWriter river3y = new System.IO.StreamWriter("E:\\river3y.txt"))
                 using (System.IO.StreamWriter river4x = new System.IO.StreamWriter("E:\\river4x.txt"))
                 using (System.IO.StreamWriter river4y = new System.IO.StreamWriter("E:\\river4y.txt"))
                 using (System.IO.StreamWriter river5x = new System.IO.StreamWriter("E:\\river5x.txt"))
                 using (System.IO.StreamWriter river5y = new System.IO.StreamWriter("E:\\river5y.txt"))
                 //using (System.IO.StreamWriter rivere = new System.IO.StreamWriter("E:\\rivere.txt"))
            
                 {
                     for (int i = 0; i < island.centers.Count; i++)
                     {
                         file3.Write(island.centers[i].data[0] + " ");
                         file4.Write(island.centers[i].data[1] + " ");
                     }
                     foreach (var item in island.Tiles.Values)
                     {

                         if (item.iswater)
                         {
                             //System.Console.WriteLine(item.center);
                             file1.Write(item.center.data[0] + " ");
                             file2.Write(item.center.data[1] + " ");

                         }

                     }
                     for (int i = 0; i < rivers[0].river.Count; i++)
                     {
                         river1x.Write(rivers[0].river[i].position.data[0] + " ");
                         river1y.Write(rivers[0].river[i].position.data[1] + " ");
                         //rivere.Write(r_test.river[i].elevation+" ");

                     }
                     for (int i = 0; i < rivers[1].river.Count; i++)
                     {
                         river2x.Write(rivers[1].river[i].position.data[0] + " ");
                         river2y.Write(rivers[1].river[i].position.data[1] + " ");
                         //rivere.Write(rivers.river[i].elevation+" ");

                     }
                     for (int i = 0; i < rivers[2].river.Count; i++)
                     {
                         river3x.Write(rivers[2].river[i].position.data[0] + " ");
                         river3y.Write(rivers[2].river[i].position.data[1] + " ");
                         //rivere.Write(r_test.river[i].elevation+" ");

                     }
                     for (int i = 0; i < rivers[3].river.Count; i++)
                     {
                         river4x.Write(rivers[3].river[i].position.data[0] + " ");
                         river4y.Write(rivers[3].river[i].position.data[1] + " ");
                         //rivere.Write(r_test.river[i].elevation+" ");

                     }
                     for (int i = 0; i < rivers[4].river.Count; i++)
                     {
                         river5x.Write(rivers[4].river[i].position.data[0] + " ");
                         river5y.Write(rivers[4].river[i].position.data[1] + " ");
                         //rivere.Write(r_test.river[i].elevation+" ");

                     }

                     foreach (var item in island.shore)
                     {
                         file5.Write(item.position.data[0] + " ");
                         file6.Write(item.position.data[1] + " ");
                     }
                     foreach (var item in island.totalcorners)
                     {
                         file7.Write(item.position.data[0] + " ");
                         file8.Write(item.position.data[1] + " ");
                     }
                 }
                 */

            


        }
    }
}
