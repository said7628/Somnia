using UnityEngine;
using UnityEngine.EventSystems;

public class HoverCanva : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Vector3 originalScale;
    private Coroutine scaleRoutine;

    [Header("Hover Settings")]
    public float scaleMultiplier = 1.1f;
    public float speed = 10f;

    void Start()
    {
        originalScale = transform.localScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        StartScaling(originalScale * scaleMultiplier);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        StartScaling(originalScale);
    }

    void StartScaling(Vector3 target)
    {
        if (scaleRoutine != null)
            StopCoroutine(scaleRoutine);

        scaleRoutine = StartCoroutine(ScaleTo(target));
    }

    System.Collections.IEnumerator ScaleTo(Vector3 target)
    {
        while (Vector3.Distance(transform.localScale, target) > 0.01f)
        {
            transform.localScale = Vector3.Lerp(
                transform.localScale,
                target,
                Time.unscaledDeltaTime * speed
            );

            yield return null;
        }

        transform.localScale = target;
    }
}