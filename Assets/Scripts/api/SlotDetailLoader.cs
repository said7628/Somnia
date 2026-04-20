using System;
using System.Collections;
using UnityEngine;

namespace Somnia.UnityClient
{
    public class SlotDetailLoader : MonoBehaviour
    {
        [SerializeField] private GameApiClient apiClient;

        public SlotDetailResponse CurrentSlotDetail { get; private set; }

        public void SetApiClient(GameApiClient client)
        {
            apiClient = client;
        }

        public IEnumerator LoadSlotDetail(int slotNumber, Action<SlotDetailResponse> onLoaded, Action<string> onError)
        {
            yield return apiClient.GetJson(
                $"/game/slots/{slotNumber}",
                onSuccess: (raw) =>
                {
                    var data = JsonUtility.FromJson<SlotDetailResponse>(raw);
                    if (data == null || !data.success || data.slot == null)
                    {
                        onError?.Invoke("Respuesta inválida al cargar detalle de slot");
                        return;
                    }

                    CurrentSlotDetail = data;
                    GameSessionManager.Instance?.SetCurrentSlot(slotNumber);
                    onLoaded?.Invoke(data);
                },
                onError: (err) => onError?.Invoke(err),
                withAuth: true
            );
        }
    }
}
