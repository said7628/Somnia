using System;
using System.Collections;
using UnityEngine;

namespace Somnia.UnityClient
{
    public class SlotsLoader : MonoBehaviour
    {
        [SerializeField] private GameApiClient apiClient;

        public SlotSummary[] CurrentSlots { get; private set; }

        public void SetApiClient(GameApiClient client)
        {
            apiClient = client;
        }

        public IEnumerator LoadSlots(Action<SlotSummary[]> onLoaded, Action<string> onError)
        {
            yield return apiClient.GetJson(
                "/game/slots",
                onSuccess: (raw) =>
                {
                    var data = JsonUtility.FromJson<SlotSummaryResponse>(raw);
                    if (data == null || !data.success || data.slots == null)
                    {
                        onError?.Invoke("Respuesta inválida al cargar slots");
                        return;
                    }

                    CurrentSlots = data.slots;
                    onLoaded?.Invoke(CurrentSlots);
                },
                onError: (err) => onError?.Invoke(err),
                withAuth: true
            );
        }
    }
}
