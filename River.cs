﻿using System;
using System.Collections.Generic;

public class River
{
    public List<IslandTileCorner> river = new List<IslandTileCorner>();//store a river corners
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
        


    }

}

