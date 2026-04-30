using UnityEngine;

[DisallowMultipleComponent]
public class InGame_RoomExample : MonoBehaviour
{
    public static InGame_RoomExample Instance { get; private set; }

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
