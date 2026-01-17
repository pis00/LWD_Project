using UnityEngine;

namespace CoverShooter
{
    public class ValveSmokeOnHit : MonoBehaviour
    {
        [Header("Smoke VFX")]
        [Tooltip("Assign the same prefab used by SmokeGrenade.Explosion (or ExplosionPreview).")]
        public GameObject SmokePrefab;

        [Tooltip("If true, smoke will be parented to the valve (follows it).")]
        public bool ParentToValve = true;

        [Tooltip("Local offset where the smoke should appear (e.g., valve opening).")]
        public Vector3 LocalOffset = Vector3.zero;

        [Tooltip("Seconds before destroying the spawned smoke object. Set 0 for no auto-destroy.")]
        public float DestroyAfter = 6f;

        [Header("Spam Control")]
        [Tooltip("Minimum time between hits that can spawn smoke.")]
        public float Cooldown = 0.3f;

        private float _cooldownLeft;

        private void Update()
        {
            if (_cooldownLeft > 0f)
                _cooldownLeft -= Time.deltaTime;
        }

        public void OnHit(Hit hit)
        {
            if (_cooldownLeft > 0f)
                return;

            _cooldownLeft = Cooldown;

            if (SmokePrefab == null)
                return;

            var smoke = Instantiate(SmokePrefab);

            if (ParentToValve)
                smoke.transform.SetParent(transform, false);
            else
                smoke.transform.SetParent(null);

            smoke.transform.position = transform.TransformPoint(LocalOffset);
            smoke.SetActive(true);

            if (DestroyAfter > 0f)
                Destroy(smoke, DestroyAfter);
        }

        private void OnValidate()
        {
            DestroyAfter = Mathf.Max(0f, DestroyAfter);
            Cooldown = Mathf.Max(0f, Cooldown);
        }
    }
}