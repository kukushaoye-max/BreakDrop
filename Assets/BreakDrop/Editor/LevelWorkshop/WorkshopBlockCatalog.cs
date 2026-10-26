using System;
using UnityEngine;
namespace BreakDrop.Editor
{
    // Matches the authored piece set used by PrototypeLevelBatchGenerator.
    internal static class WorkshopBlockCatalog
    {
        internal sealed class Entry
        {
            public readonly string art,shape,label;
            public readonly float width,height;
            public Entry(string art,string shape,int width,int height)
            {this.art=art;this.shape=shape;this.width=width;this.height=height;label=art+" · "+(shape=="Rectangle"?"矩形":shape)+" "+width+"×"+height;}
            public BlockDefinition Create(int id,float rotation=0)
            {
                return new BlockDefinition{id=id,y=3,art=art,shape=shape,width=width,height=height,rotation=rotation,
                    mass=shape=="Rectangle"?width*height:width+height-1};
            }
        }
        internal static readonly Entry[] Entries={
            new Entry("BlockA","Rectangle",2,1),new Entry("BlockB","Rectangle",2,1),
            new Entry("BlockC","Rectangle",1,1),new Entry("BlockD","L3",3,3),
            new Entry("BlockE","Rectangle",1,1),new Entry("BlockG","Rectangle",3,1),
            new Entry("BlockI","Rectangle",2,1),new Entry("BlockJ","L2",2,3),
            new Entry("BlockK","Rectangle",1,1),new Entry("BlockL","Rectangle",1,1),
            new Entry("BlockR","Rectangle",5,1)};
    }
}
