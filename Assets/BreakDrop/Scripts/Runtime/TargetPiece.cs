using UnityEngine;
namespace BreakDrop
{
    [RequireComponent(typeof(PolygonCollider2D), typeof(Rigidbody2D))]
    public sealed class TargetPiece : MonoBehaviour
    {
        public Rigidbody2D Body => GetComponent<Rigidbody2D>();
        public Collider2D Collider => GetComponent<PolygonCollider2D>();
    }
}
