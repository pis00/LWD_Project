using System.Collections;
using UnityEngine;

public class TrainLoopMover : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform train;
    [SerializeField] private Transform startPoint;
    [SerializeField] private Transform endPoint;

    [Header("Timing")]
    [Tooltip("Time between spawns (seconds). 60 = every minute.")]
    [SerializeField] private float intervalSeconds = 60f;

    [Tooltip("Optional delay before the first run (seconds).")]
    [SerializeField] private float initialDelaySeconds = 0f;

    [Header("Motion")]
    [SerializeField] private float speedMetersPerSecond = 12f;
    [SerializeField] private bool faceDirection = true;

    [Header("Rotation")]
    [SerializeField] private bool useFixedRotation = true;
    [SerializeField] private Vector3 fixedEulerRotation = new Vector3(270f, 270f, 0f);

    private Coroutine _loop;

    private void OnEnable()
    {
        if (_loop == null)
            _loop = StartCoroutine(Loop());
    }

    private void OnDisable()
    {
        if (_loop != null)
        {
            StopCoroutine(_loop);
            _loop = null;
        }
    }

    private IEnumerator Loop()
    {
        if (train == null || startPoint == null || endPoint == null)
            yield break;

        train.gameObject.SetActive(false);

        if (initialDelaySeconds > 0f)
            yield return new WaitForSeconds(initialDelaySeconds);

        while (true)
        {
            // Start timestamp for "every minute" cadence
            float cycleStartTime = Time.time;

            // Spawn at start
            train.position = startPoint.position;

            if (useFixedRotation)
            {
                train.rotation = Quaternion.Euler(fixedEulerRotation);
            }
            else
            {
                train.rotation = startPoint.rotation;

                if (faceDirection)
                {
                    Vector3 dir = endPoint.position - startPoint.position;
                    dir.y = 0f;
                    if (dir.sqrMagnitude > 0.0001f)
                        train.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
                }
            }

            train.gameObject.SetActive(true);

            // Move to end with constant speed
            Vector3 a = startPoint.position;
            Vector3 b = endPoint.position;

            float dist = Vector3.Distance(a, b);
            float duration = dist / Mathf.Max(0.01f, speedMetersPerSecond);
            duration = Mathf.Max(0.01f, duration);

            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / duration;
                train.position = Vector3.Lerp(a, b, Mathf.Clamp01(t));
                yield return null;
            }

            // Hide at end
            train.position = endPoint.position;
            train.gameObject.SetActive(false);

            // Wait remaining time to keep exact interval (start-to-start)
            float elapsed = Time.time - cycleStartTime;
            float wait = Mathf.Max(0f, intervalSeconds - elapsed);
            yield return new WaitForSeconds(wait);
        }
    }

    private void OnValidate()
    {
        intervalSeconds = Mathf.Max(0f, intervalSeconds);
        initialDelaySeconds = Mathf.Max(0f, initialDelaySeconds);
        speedMetersPerSecond = Mathf.Max(0.01f, speedMetersPerSecond);
    }
}