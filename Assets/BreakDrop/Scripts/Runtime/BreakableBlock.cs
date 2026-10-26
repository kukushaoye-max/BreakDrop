using System.Collections;
using UnityEngine;

namespace BreakDrop
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class BreakableBlock : MonoBehaviour
    {
        private BreakDropGameManager _manager;
        private SpriteRenderer _renderer;
        private Collider2D[] _colliders;
        private Color _burstColor;
        private Coroutine _hintRoutine;
        public int Id { get; private set; }
        public bool IsBroken { get; private set; }
        public bool CanRemove { get; private set; }
        public Rigidbody2D Body { get; private set; }
        public Collider2D[] Colliders => _colliders;

        public void Configure(BreakDropGameManager manager, BlockDefinition data, SpriteRenderer renderer)
        {
            _manager = manager;
            Id = data.id;
            CanRemove = data.breakable && !data.fixedBlock;
            _renderer = renderer;
            _burstColor = DesignArt.Hex(data.color);
            _colliders = GetComponents<Collider2D>();
            Body = GetComponent<Rigidbody2D>();
        }

        private void OnMouseDown()
        {
            if (!BreakDropHud.BlocksWorldInput) _manager?.TryBreak(this);
        }

        public void ClearHint()
        {
            if (_hintRoutine != null) StopCoroutine(_hintRoutine);
            _hintRoutine = null;
            _renderer.color = Color.white;
        }

        public void SetBroken(bool broken, bool showEffect = false)
        {
            ClearHint();
            if (showEffect && broken && !IsBroken) SpawnBurst();
            IsBroken = broken;
            _renderer.enabled = !broken;
            foreach (Collider2D collider in _colliders) collider.enabled = !broken;
            Body.simulated = !broken;
        }

        public void ShowHint()
        {
            if (IsBroken || !CanRemove) return;
            ClearHint();
            _hintRoutine = StartCoroutine(HintPulse());
        }

        private IEnumerator HintPulse()
        {
            float elapsed = 0;
            while (elapsed < 1.5f && !IsBroken)
            {
                elapsed += Time.unscaledDeltaTime;
                // Only opacity changes: never resize a physics transform for a hint.
                _renderer.color = new Color(1, 1, 1, .55f + .45f * Mathf.Abs(Mathf.Sin(elapsed * 7)));
                yield return null;
            }
            _renderer.color = Color.white;
            _hintRoutine = null;
        }

        private void SpawnBurst()
        {
            if (!Application.isPlaying) return;
            for (int i = 0; i < 10; i++)
            {
                var particle = new GameObject("BreakParticle");
                particle.transform.SetParent(transform.parent, false);
                particle.transform.position = transform.position;
                particle.transform.localScale = Vector3.one * Random.Range(.06f, .13f);
                var renderer = particle.AddComponent<SpriteRenderer>();
                renderer.sprite = ProceduralArt.Square;
                renderer.color = _burstColor;
                renderer.sortingOrder = 2000;
                particle.AddComponent<BreakParticle>().Velocity = Random.insideUnitCircle * 3 + Vector2.up;
            }
        }
    }

    public sealed class BreakParticle : MonoBehaviour
    {
        public Vector2 Velocity;
        private float _life = .55f;
        private void Update()
        {
            _life -= Time.deltaTime;
            if (_life <= 0) { Destroy(gameObject); return; }
            Velocity += Vector2.down * (14 * Time.deltaTime);
            transform.position += (Vector3)Velocity * Time.deltaTime;
            var renderer = GetComponent<SpriteRenderer>();
            Color c = renderer.color;
            c.a = Mathf.Clamp01(_life / .25f);
            renderer.color = c;
        }
    }
}
