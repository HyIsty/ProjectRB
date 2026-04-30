using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// glossary 키를 설명 데이터로 매핑하는 간단한 데이터베이스.
/// 
/// 현재 단계에서는:
/// - 인스펙터에서 리스트를 직접 입력하고
/// - 필요할 때 key로 조회해서
/// - GlossaryTooltipUI가 제목/설명을 표시하게 만드는 용도다.
/// </summary>
public class EffectGlossaryDatabase : MonoBehaviour
{
    [Header("Glossary Entries")]
    [SerializeField] private List<EffectGlossaryEntry> entries = new List<EffectGlossaryEntry>();

    /// <summary>
    /// key로 glossary 항목을 찾는다.
    /// 대소문자는 구분하지 않는다.
    /// </summary>
    public bool TryGetEntry(string key, out EffectGlossaryEntry foundEntry)
    {
        foundEntry = null;

        if (string.IsNullOrWhiteSpace(key))
            return false;

        for (int i = 0; i < entries.Count; i++)
        {
            EffectGlossaryEntry entry = entries[i];

            if (entry == null)
                continue;

            if (string.Equals(entry.key, key, StringComparison.OrdinalIgnoreCase))
            {
                foundEntry = entry;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 디버그/확인용. 현재 등록된 항목 수를 반환.
    /// </summary>
    public int GetEntryCount()
    {
        return entries != null ? entries.Count : 0;
    }
}