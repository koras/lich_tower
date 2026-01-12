using UnityEngine;

public class RotateSprite : MonoBehaviour
{
    
    [SerializeField] private float speed = 180f; // градусов в секунду

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(0f, 0f, speed * Time.deltaTime);
    }
}
