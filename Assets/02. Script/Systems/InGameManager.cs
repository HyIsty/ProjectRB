using UnityEngine;

[DisallowMultipleComponent]
public class InGameManager : MonoBehaviour
{
    public static InGameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
    }

}
