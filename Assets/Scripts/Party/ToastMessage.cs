using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Somnia.UnityClient
{
    public class ToastMessage : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Text messageText;
        [SerializeField] private TMP_Text tmpMessageText;
        [SerializeField] private float defaultDuration = 2.8f;
        [SerializeField] private Vector2 minToastSize = new Vector2(860f, 150f);
        [SerializeField] private int minFontSize = 40;
        [SerializeField] private bool forceOnTop = true;

        private Coroutine hideRoutine;
        private Canvas rootCanvas;
        private RectTransform rootRect;

        private void Awake()
        {
            ResolveReferences();
            HideImmediate();
        }

        public void Show(string message)
        {
            Show(message, defaultDuration);
        }

        public void Show(string message, float duration)
        {
            ResolveReferences();

            if (messageText != null)
            {
                messageText.text = message;
                messageText.resizeTextForBestFit = true;
                messageText.resizeTextMinSize = minFontSize;
            }

            if (tmpMessageText != null)
            {
                tmpMessageText.text = message;
                tmpMessageText.enableAutoSizing = true;
                tmpMessageText.fontSizeMin = minFontSize;
            }

            EnsureVisualVisibility();

            if (root != null)
            {
                root.SetActive(true);
            }
            else
            {
                gameObject.SetActive(true);
            }

            if (hideRoutine != null)
            {
                StopCoroutine(hideRoutine);
            }

            hideRoutine = StartCoroutine(HideAfter(duration));
        }

        public void HideImmediate()
        {
            if (hideRoutine != null)
            {
                StopCoroutine(hideRoutine);
                hideRoutine = null;
            }

            if (root != null)
            {
                root.SetActive(false);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        private IEnumerator HideAfter(float duration)
        {
            yield return new WaitForSeconds(duration);

            if (root != null)
            {
                root.SetActive(false);
            }
            else
            {
                gameObject.SetActive(false);
            }

            hideRoutine = null;
        }

        private void ResolveReferences()
        {
            if (root == null)
            {
                root = gameObject;
            }

            if (messageText == null)
            {
                messageText = GetComponentInChildren<Text>(true);
            }

            if (tmpMessageText == null)
            {
                tmpMessageText = GetComponentInChildren<TMP_Text>(true);
            }

            if (rootRect == null && root != null)
            {
                rootRect = root.GetComponent<RectTransform>();
            }

            if (rootCanvas == null && root != null)
            {
                rootCanvas = root.GetComponentInParent<Canvas>(true);
            }
        }

        private void EnsureVisualVisibility()
        {
            if (rootRect != null)
            {
                rootRect.sizeDelta = new Vector2(
                    Mathf.Max(rootRect.sizeDelta.x, minToastSize.x),
                    Mathf.Max(rootRect.sizeDelta.y, minToastSize.y)
                );
            }

            if (forceOnTop && rootCanvas != null)
            {
                rootCanvas.overrideSorting = true;
                rootCanvas.sortingOrder = Mathf.Max(rootCanvas.sortingOrder, 500);
            }
        }
    }
}