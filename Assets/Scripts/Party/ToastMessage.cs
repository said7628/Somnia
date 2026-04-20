using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Somnia.UnityClient
{
    public class ToastMessage : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Text messageText;
        [SerializeField] private float defaultDuration = 2.2f;

        private Coroutine hideRoutine;

        private void Awake()
        {
            HideImmediate();
        }

        public void Show(string message)
        {
            Show(message, defaultDuration);
        }

        public void Show(string message, float duration)
        {
            if (messageText != null)
            {
                messageText.text = message;
            }

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
    }
}