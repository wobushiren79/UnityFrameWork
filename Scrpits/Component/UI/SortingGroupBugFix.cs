using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;

public class SortingGroupBugFix : BaseMonoBehaviour
{
    private void Awake()
    {
        SortingGroup sortingGroup= GetComponent<SortingGroup>();
        int oldOrder = sortingGroup.sortingOrder;
        sortingGroup.sortingOrder = oldOrder+1;
        sortingGroup.sortingOrder = oldOrder;
    }
}