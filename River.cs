using System;
using System.Collections.Generic;

public class River
{
    //this is a binary tree storing the corners in the river
    /*public List<IslandTileCorner> river = new List<IslandTileCorner>();//store a river corners
    public IslandTileCorner rp;//start corner of a river
    public int cornercount = 1;
    public void generateriver(IslandTileCorner p)
    {
        if (cornercount == 1)
            river.Add(p);
        IslandTileCorner loweast = p;
        foreach(var a in p.adjacent)
        {
            if(a.elevation<loweast.elevation)
            {
                loweast = a;
            }
        }
        river.Add(loweast);
        cornercount++;
        if (loweast.elevation!=0)
        {
            generateriver(loweast);
        }
        


    }*/
    public IslandTileCorner data;
    public River left;
    public River right;
    public int discharge=0;
    public River(IslandTileCorner c)
    {
        this.data = c;
        this.left = null;
        this.right = null;
    }
    public static int findDischarge(River r)
    {
        if(r==null)
        {
            return 0;
        }
        if(r.left==null&&r.right==null)
        {
           r.discharge=1;
        }
        else
        {
            r.discharge = findDischarge(r.left) + findDischarge(r.right);
        }
        if (r.left != null)
            System.Console.WriteLine("left");
        System.Console.WriteLine(r.discharge);
        return r.discharge;
    }


}

