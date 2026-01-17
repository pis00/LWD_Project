using UnityEngine;

namespace CoverShooter
{
    /// <summary>
    /// Explosive barrel that reacts to CoverShooter hits (OnHit(Hit)).
    /// When destroyed, it applies explosion damage using the same pipeline as Grenade.cs:
    /// Physics.OverlapSphere + SendMessage("OnHit", hit).
    /// </summary>
    public class ExplosiveBarrel : MonoBehaviour
    {
        [Header("Barrel Health")]
        [Tooltip("How much damage the barrel can take before exploding.")]
        public float Health = 50f;

        [Header("Explosion")]
        [Tooltip("Explosion prefab to instantiate when the barrel explodes (optional).")]
        public GameObject Explosion;

        [Tooltip("Radius of the explosion.")]
        public float ExplosionRadius = 4.5f;

        [Tooltip("Damage at the center of the explosion.")]
        public float CenterDamage = 150f;

        [Tooltip("Optional force applied to rigidbodies in the radius.")]
        public float ExplosionForce = 800f;

        [Tooltip("Optional upward modifier for explosion force.")]
        public float UpwardsModifier = 0.0f;

        [Tooltip("Camera shake duration when the barrel explodes.")]
        public float ShakeDuration = 0.5f;

        [Tooltip("Camera shake intensity when close to the camera.")]
        public float ShakeIntensity = 100f;

        [Tooltip("Layers affected by the explosion. Default is Everything.")]
        public LayerMask AffectedLayers = ~0;

        [Tooltip("Should the barrel apply physics force to rigidbodies.")]
        public bool ApplyPhysicsForce = true;

        [Tooltip("Should the barrel also damage other explosive barrels (chain reaction).")]
        public bool ChainReaction = true;

        private bool _hasExploded;
        private GameObject _lastAttacker;

        /// <summary>
        /// Called by CoverShooter damage pipeline (bullets/melee/explosions) via SendMessage("OnHit", hit).
        /// </summary>
        public void OnHit(Hit hit)
        {
            if (_hasExploded) return;

            _lastAttacker = hit.Attacker;

            Health -= hit.Damage;
            if (Health <= float.Epsilon)
                Explode();
        }

        private void Explode()
        {
            if (_hasExploded) return;
            _hasExploded = true;
            ThirdPersonCamera.Shake(transform.position, ShakeIntensity, ShakeDuration);

            if (Explosion != null)
            {
                // Optional: if Explosion prefab has Alert, you could set Generator like Grenade does.
                var alert = Explosion.GetComponent<Alert>();
                if (alert != null)
                    alert.Generator = Actors.Get(_lastAttacker);

                var particle = Instantiate(Explosion, transform.position, Quaternion.identity, null);
                particle.SetActive(true);
            }

            // Collect colliders in the blast radius
            int count = Physics.OverlapSphereNonAlloc(transform.position, ExplosionRadius, Util.Colliders, AffectedLayers, QueryTriggerInteraction.Ignore);

            for (int i = 0; i < count; i++)
            {
                var col = Util.Colliders[i];
                if (col == null || col.isTrigger) continue;

                // Apply explosion force (optional)
                if (ApplyPhysicsForce && col.attachedRigidbody != null)
                {
                    col.attachedRigidbody.AddExplosionForce(
                        ExplosionForce,
                        transform.position,
                        ExplosionRadius,
                        UpwardsModifier,
                        ForceMode.Impulse
                    );
                }

                // Determine closest point for better distance calculation (same idea as Grenade.cs)
                var closest = col.transform.position;

                if (col is MeshCollider meshCol && meshCol.convex)
                    closest = col.ClosestPoint(transform.position);

                var vector = transform.position - closest;
                var distance = vector.magnitude;

                if (distance >= ExplosionRadius) continue;

                Vector3 normal;
                if (distance > float.Epsilon)
                    normal = vector / distance;
                else
                    normal = (closest - col.transform.position).normalized;

                float fraction = 1f - (distance / ExplosionRadius);
                ApplyExplosionDamage(col.gameObject, closest, normal, fraction);

                // Optional: chain reaction
                if (ChainReaction)
                {
                    var otherBarrel = col.GetComponentInParent<ExplosiveBarrel>();
                    if (otherBarrel != null && otherBarrel != this)
                    {
                        // Give it enough damage to trigger, or partial
                        otherBarrel.OnHit(new Hit(closest, normal, CenterDamage * fraction, _lastAttacker, otherBarrel.gameObject, HitType.Explosion, 0));
                    }
                }
            }

            Destroy(gameObject);
        }

        private void ApplyExplosionDamage(GameObject target, Vector3 position, Vector3 normal, float fraction)
        {
            float damage = CenterDamage * fraction;
            if (damage <= float.Epsilon) return;

            var hit = new Hit(position, normal, damage, _lastAttacker, target, HitType.Explosion, 0);

            // EXACTLY like Grenade.cs: send the Hit through the same pipeline
            target.SendMessage("OnHit", hit, SendMessageOptions.DontRequireReceiver);
        }

        private void OnValidate()
        {
            Health = Mathf.Max(1f, Health);
            ExplosionRadius = Mathf.Max(0.1f, ExplosionRadius);
            CenterDamage = Mathf.Max(0f, CenterDamage);
            ExplosionForce = Mathf.Max(0f, ExplosionForce);
            ShakeDuration = Mathf.Max(0f, ShakeDuration);
            ShakeIntensity = Mathf.Max(0f, ShakeIntensity);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.DrawWireSphere(transform.position, ExplosionRadius);
        }
    }
}