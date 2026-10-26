using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BreakDrop.Editor
{
    internal static class SupportSampleRecipes
    {
        internal static readonly string[] Names = { "L形承托", "双块共托", "桥梁换支点", "错层承接", "组合支撑" };
        internal static int[][] ReferenceCandidates(int sample)
        {
            switch(sample)
            {
                case 1:return new[]{new[]{6,8,7,4,1}};
                case 2:return new[]{new[]{9,8,3,6,1},new[]{9,8,3,1,6},new[]{10,8,3,6,1},new[]{9,8,6,3,1}};
                case 3:return new[]{new[]{7,8,9,2,5,3},new[]{7,9,8,5,2,3},new[]{6,9,8,4,2,1}};
                case 4:return new[]{new[]{10,11,6,8,5,4,2},new[]{10,11,8,6,5,4,2}};
                default:return new[]{new[]{12,14,6,3,7,1},new[]{12,14,6,2,1,3},new[]{13,14,9,4,2,1}};
            }
        }
        internal static LevelDefinition Create(int sample, int attempt)
        {
            var blocks = new List<BlockDefinition>();
            Action<float,float,float,float,string> add = (x,y,w,h,shape) => {
                int id=blocks.Count+1;
                blocks.Add(new BlockDefinition { id=id,x=x+(w-1)*.5f-2.5f+PrototypeLevelBatchGenerator.CenterX,
                    y=PrototypeLevelBatchGenerator.BaseY+y+(h-1)*.5f,width=w,height=h,shape=shape,
                    art=shape=="L3"?"BlockD":shape=="L2"?"BlockJ":new[]{"BlockA","BlockB","BlockG","BlockI","BlockL"}[(id+sample)%5],
                    mass=shape.StartsWith("L")?w+h-1:w*h,breakable=true,fixedBlock=false });
            };
            float tx=2.5f;
            switch(sample)
            {
                case 1:
                    add(0,0,3,1,"Rectangle");add(3,0,3,1,"Rectangle");
                    add(0,1,1,1,"Rectangle");add(1,1,3,1,"Rectangle");add(4,1,2,1,"Rectangle");
                    add(1,2,3,3,"L3");add(2,2,2,1,"Rectangle");add(2,3,2,1,"Rectangle");
                    add(0,2,1,3,"Rectangle");add(4,2,1,3,"Rectangle");add(5,2,1,3,"Rectangle");
                    tx=2+(attempt%5)*.25f; break;
                case 2:
                    add(0,0,3,1,"Rectangle");add(3,0,3,1,"Rectangle");
                    add(0,1,3,2,"L3");add(2,1,2,1,"Rectangle");add(4,1,2,1,"Rectangle");
                    add(1,1,1,1,"Rectangle");add(3,2,3,1,"Rectangle");
                    add(1,3,4,1,"Rectangle");add(1,4,2,1,"Rectangle");add(3,4,2,1,"Rectangle");
                    add(0,3,1,2,"Rectangle");add(5,3,1,2,"Rectangle");
                    tx=2.5f; break;
                case 3:
                    add(0,0,2,1,"Rectangle");add(2,0,2,1,"Rectangle");add(4,0,2,1,"Rectangle");
                    add(1,1,2,1,"Rectangle");add(3,1,2,1,"Rectangle");
                    add(1,3,1,1,"Rectangle");add(4,3,1,1,"Rectangle");
                    add(1,2,4,1,"Rectangle");add(1,4,4,1,"Rectangle");
                    add(0,1,1,6,"Rectangle");add(5,1,1,6,"Rectangle");
                    tx=2+(attempt%5)*.25f; break;
                case 4:
                    add(0,0,3,1,"Rectangle");add(3,0,3,1,"Rectangle");
                    add(0,1,2,1,"Rectangle");add(2,1,2,1,"Rectangle");add(4,1,2,1,"Rectangle");
                    add(0,2,3,3,"L3");add(1,2,2,1,"Rectangle");add(1,3,2,1,"Rectangle");
                    add(3,2,3,2,"L3");add(4,2,2,1,"Rectangle");
                    add(1,5,4,1,"Rectangle");add(3,4,2,1,"Rectangle");add(5,4,1,1,"Rectangle");
                    tx=2+(attempt%5)*.25f;break;
                case 5:
                    add(0,0,3,1,"Rectangle");add(3,0,3,1,"Rectangle");
                    add(1,1,2,1,"Rectangle");add(3,1,2,1,"Rectangle");add(2,2,2,1,"Rectangle");
                    add(1,2,2,3,"L2");add(2,3,1,1,"Rectangle");
                    add(4,2,1,1,"Rectangle");add(3,3,2,2,"L2");add(4,3,1,1,"Rectangle");
                    add(0,1,1,7,"Rectangle");
                    add(1,5,1,1,"Rectangle");add(4,5,1,1,"Rectangle");add(1,6,4,1,"Rectangle");
                    add(5,1,1,7,"Rectangle");
                    tx=2+(attempt%5)*.25f;break;
            }
            // Deterministic seed enumerates target offsets and six foundation seam variants.
            int seam=(attempt/5)%6;
            if(sample==3)
            {
                int[][] partitions={new[]{2,2,2},new[]{1,3,2},new[]{2,3,1},new[]{1,4,1},new[]{3,1,2},new[]{2,1,3}};
                int left=0;
                for(int i=0;i<3;i++) {var b=blocks[i];b.width=partitions[seam][i];b.mass=b.width;b.x=left+(b.width-1)*.5f-2.5f+PrototypeLevelBatchGenerator.CenterX;left+=(int)b.width;}
            }
            else
            {
                int width=new[]{3,2,4,3,2,4}[seam];
                blocks[0].width=width;blocks[0].mass=width;blocks[0].x=(width-1)*.5f-2.5f+PrototypeLevelBatchGenerator.CenterX;
                blocks[1].width=6-width;blocks[1].mass=6-width;blocks[1].x=width+(5-width)*.5f-2.5f+PrototypeLevelBatchGenerator.CenterX;
            }
            if(sample==3 && Mathf.Approximately(tx,2.5f))tx=2.4f;
            int supportId=sample==1?6:sample==2?9:sample==3?9:sample==4?11:14;
            var support=blocks.First(b=>b.id==supportId);
            float targetX=tx-2.5f+PrototypeLevelBatchGenerator.CenterX;
            if(targetX<support.x-support.width*.5f||targetX>support.x+support.width*.5f)
                throw new InvalidOperationException("Target is outside the authored bearing surface.");
            float surface=support.y+support.height*.5f-.0075f;
            float targetY=surface+42f/DesignArt.PixelsPerUnit*Mathf.Sin(Mathf.PI/3)+.0175f;
            return new LevelDefinition { levelId=sample,towerLevel=true,bestMoves=0,timeLimitSeconds=90,
                target=new VectorData{x=targetX,y=targetY},
                goal=new VectorData{x=PrototypeLevelBatchGenerator.CenterX,y=-3.66923077f},goalWidth=5.76923077f,
                failX=4.3f,failY=-5.1f,starHeights=new[]{Mathf.Lerp(targetY,-2.26923f,.33f),Mathf.Lerp(targetY,-2.26923f,.66f),-2.26923f},
                blocks=blocks.ToArray(),hintOrder=Array.Empty<int>() };
        }
    }
}
