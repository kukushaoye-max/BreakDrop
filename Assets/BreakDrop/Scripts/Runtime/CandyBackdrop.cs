using UnityEngine;
namespace BreakDrop
{
    public sealed class CandyBackdrop : MonoBehaviour
    {
        private Material artworkMaterial;
        private void Awake()
        {
            artworkMaterial=Resources.Load<Material>("UI/SquareCornerArtwork");
            GetComponent<SpriteRenderer>().sharedMaterial=artworkMaterial;
        }
        private void LateUpdate()
        {
            var c=Camera.main;if(!c)return;
            transform.position=new Vector3(c.transform.position.x,c.transform.position.y,0);
            var s=GetComponent<SpriteRenderer>().sprite;
            transform.localScale=new Vector3(c.orthographicSize*2*c.aspect/s.bounds.size.x,c.orthographicSize*2/s.bounds.size.y,1);
        }
    }
}

