using UnityEngine;

/// <summary>
/// tracer prefab을 생성해 주는 간단한 팩토리.
/// 사격 로직은 이쪽에서 하지 않는다.
/// 이미 계산된 start / end를 받아 tracer만 생성한다.
/// </summary>
public class ShotTracerFactory : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] private ShotTracerView tracerPrefab;

    /// <summary>
    /// tracer 한 발 생성.
    /// </summary>
    public void SpawnTracer(Vector3 startWorld, Vector3 endWorld)
    {
        if (tracerPrefab == null)
        {
            Debug.LogWarning("ShotTracerFactory: tracerPrefab이 비어 있다.");
            return;
        }

        ShotTracerView tracerInstance = Instantiate(tracerPrefab, Vector3.zero, Quaternion.identity);
        tracerInstance.Play(startWorld, endWorld);
    }
}