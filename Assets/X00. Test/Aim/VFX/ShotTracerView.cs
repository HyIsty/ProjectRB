using System.Collections;
using UnityEngine;

/// <summary>
/// 한 발의 tracer를 잠깐 보여주고 사라지게 하는 뷰 컴포넌트.
/// 이 스크립트는 판정을 계산하지 않는다.
/// 이미 계산된 start / end 월드 좌표를 받아서 시각적으로만 표시한다.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class ShotTracerView : MonoBehaviour
{
    [Header("Lifetime")]
    [SerializeField] private float lifeTime = 0.06f;

    [Header("Width")]
    [SerializeField] private float startWidth = 0.08f;
    [SerializeField] private float endWidth = 0.03f;

    [Header("Render Offset")]
    [SerializeField] private float zOffset = -0.5f;

    private LineRenderer lineRenderer;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();

        // 시작 시 기본 설정
        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = true;
        lineRenderer.enabled = false;
    }

    /// <summary>
    /// tracer를 재생한다.
    /// 이미 계산된 시작점과 끝점을 그대로 받아서 표시만 한다.
    /// </summary>
    public void Play(Vector3 startWorld, Vector3 endWorld)
    {
        // 2D 카메라에서 바닥 뒤로 숨어버리지 않게 z를 앞으로 당긴다.
        startWorld.z = zOffset;
        endWorld.z = zOffset;

        // 시작 폭 / 끝 폭 적용
        lineRenderer.startWidth = startWidth;
        lineRenderer.endWidth = endWidth;

        // 선 위치 적용
        lineRenderer.SetPosition(0, startWorld);
        lineRenderer.SetPosition(1, endWorld);

        // 보이기
        lineRenderer.enabled = true;

        // 생명 주기 시작
        StartCoroutine(PlayRoutine());
    }

    /// <summary>
    /// 아주 짧은 시간 동안 보였다가 폭을 줄이며 사라진다.
    /// 끝나면 자기 자신을 제거한다.
    /// </summary>
    private IEnumerator PlayRoutine()
    {
        float time = 0f;

        while (time < lifeTime)
        {
            time += Time.deltaTime;

            float t = time / lifeTime;
            float currentStartWidth = Mathf.Lerp(startWidth, 0f, t);
            float currentEndWidth = Mathf.Lerp(endWidth, 0f, t);

            lineRenderer.startWidth = currentStartWidth;
            lineRenderer.endWidth = currentEndWidth;

            yield return null;
        }

        // 끝나면 제거
        Destroy(gameObject);
    }
}