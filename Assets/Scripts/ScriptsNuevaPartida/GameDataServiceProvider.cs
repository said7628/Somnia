using UnityEngine;
using Somnia.Economy.Core;
using Somnia.Economy.Services;

public class GameDataServiceProvider : MonoBehaviour
{
    public GameDataService GameDataService { get; private set; }

    private void Awake()
    {
        var module = EconomyModule.Instance ?? FindFirstObjectByType<EconomyModule>();
        if (module == null)
        {
            var go = new GameObject("[EconomyModule]");
            module = go.AddComponent<EconomyModule>();
        }

        GameDataService = module.GameDataService as GameDataService;
    }
}