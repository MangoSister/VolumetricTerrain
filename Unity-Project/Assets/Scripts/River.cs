using System;
using System.Collections.Generic;

public class River
{
    //this is a binary tree storing the corners in the river
    public IslandTileCorner data;
    public River left;
    public River right;
    public River father;
    public int discharge=0;
    public River(IslandTileCorner c)
    {
        this.data = c;
        this.left = null;
        this.right = null;
        this.father = null;
    }
    public static HashSet<IslandTileCorner> keeprivercorners = new HashSet<IslandTileCorner>();
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
        keeprivercorners.Add(r.data);//store all river corners
        return r.discharge;
    }


}

