using UnityEngine;
using System.Collections;

namespace Input
{
    public class DestroyWithOpacity : MonoBehaviour
    { 
        [SerializeField] private float lifetime = 3f;
    
    void Start()
    {
        StartCoroutine(FadeOut());
    }

    IEnumerator FadeOut()
    {
        SpriteRenderer spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer == null) yield break;
        
        // Ждем 1 секунду, затем начинаем затухание на 2 секунды
        yield return new WaitForSeconds(lifetime - 2f);
        
        Color color = spriteRenderer.color;
        float timer = 0f;
        
        while (timer < 2f)
        {
            color.a = Mathf.Lerp(1f, 0f, timer / 2f);
            spriteRenderer.color = color;
            timer += Time.deltaTime;
            yield return null;
        }
        
        Destroy(gameObject);
    }
}
}