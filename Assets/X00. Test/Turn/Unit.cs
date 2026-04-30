using UnityEngine;

public class Unit : MonoBehaviour
{
    public string unitName;
    public int moveRange = 3;
    public bool isPlayer;

    // Gizmo 시각화를 위한 색상 설정
    public Color unitColor => isPlayer ? Color.cyan : Color.red;

    private void OnDrawGizmos()
    {
        // 유닛의 위치 표시
        Gizmos.color = unitColor;
        Gizmos.DrawWireSphere(transform.position, 0.5f);

        // 유닛 이름 표시 (Scene 뷰 전용)
#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up, unitName);
#endif
    }
}
