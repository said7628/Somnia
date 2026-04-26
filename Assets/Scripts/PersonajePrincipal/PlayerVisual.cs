using UnityEngine;

public class PlayerVisual : MonoBehaviour
{
    [System.Serializable]
    private class PlayerCosmeticClipEntry
    {
        public int idItem;
        public AnimationClip visual;
        public AnimationClip visualWhite;
    }

    [Header("Animators")]
    [SerializeField] private Animator faceAnimator;
    [SerializeField] private Animator outfitAnimator;

    [Header("Eyes (Separados)")]
    [SerializeField] private Animator eyesNormalAnimator;
    [SerializeField] private Animator eyesWhiteAnimator;

    [Header("Animation Clips")]
    [SerializeField] private AnimationClip[] faceClips;
    [SerializeField] private AnimationClip[] outfitClips;
    [SerializeField] private AnimationClip[] eyesClips;
    [SerializeField] private AnimationClip[] eyesWhiteClips;
    [Header("Mapeo explícito por id_item (opcional, recomendado)")]
    [SerializeField] private PlayerCosmeticClipEntry[] eyesVisualByItem;
    [SerializeField] private PlayerCosmeticClipEntry[] outfitVisualByItem;

    private PlayerCustomizationManager data;

    void Start()
    {
        data = FindObjectOfType<PlayerCustomizationManager>();
        ApplyCustomization();
    }

    public void ApplyCustomization()
    {
        if (data == null)
        {
            Debug.LogError("No se encontró PlayerCustomizationManager");
            return;
        }

        // 🔒 PROTECCIÓN DE ÍNDICES
        if (data.selectedFaceColor >= faceClips.Length ||
            data.selectedOutfit >= outfitClips.Length ||
            data.selectedEyes >= eyesClips.Length)
        {
            Debug.LogError("Índice fuera de rango en PlayerVisual");
            return;
        }

        // FACE
        faceAnimator.Play(faceClips[data.selectedFaceColor].name, 0, 0f);

        // OUTFIT
        AnimationClip resolvedOutfit = ResolveOutfitClip();
        if (resolvedOutfit != null)
        {
            outfitAnimator.Play(resolvedOutfit.name, 0, 0f);
        }
        else
        {
            outfitAnimator.Play(outfitClips[data.selectedOutfit].name, 0, 0f);
        }

        // 🔥 EYES (SOLUCIÓN DEFINITIVA)

        // Apagar ambos primero
        if (eyesNormalAnimator != null)
            eyesNormalAnimator.gameObject.SetActive(false);

        if (eyesWhiteAnimator != null)
            eyesWhiteAnimator.gameObject.SetActive(false);

        // Activar el correcto
        if (data.selectedFaceColor == 9) // 👈 color negro (índice 9)
        {
            if (data.selectedEyes < eyesWhiteClips.Length)
            {
                eyesWhiteAnimator.gameObject.SetActive(true);
                AnimationClip resolvedEyesWhite = ResolveEyesClip(true);
                if (resolvedEyesWhite != null)
                {
                    eyesWhiteAnimator.Play(resolvedEyesWhite.name, 0, 0f);
                }
                else
                {
                    eyesWhiteAnimator.Play(eyesWhiteClips[data.selectedEyes].name, 0, 0f);
                }
            }
            else
            {
                Debug.LogError("Índice fuera de rango en eyesWhiteClips");
            }
        }
        else
        {
            eyesNormalAnimator.gameObject.SetActive(true);
            AnimationClip resolvedEyes = ResolveEyesClip(false);
            if (resolvedEyes != null)
            {
                eyesNormalAnimator.Play(resolvedEyes.name, 0, 0f);
            }
            else
            {
                eyesNormalAnimator.Play(eyesClips[data.selectedEyes].name, 0, 0f);
            }
        }

        Debug.Log("Customización aplicada correctamente");
    }

    private AnimationClip ResolveOutfitClip()
    {
        int outfitId = data.selectedOutfitItemId;
        if (TryGetEntry(outfitVisualByItem, outfitId, out PlayerCosmeticClipEntry entry) && entry.visual != null)
        {
            Debug.Log($"[PlayerCustomization] Outfit id={outfitId} -> {ResolveItemName(outfitId)} visual={entry.visual.name}");
            return entry.visual;
        }

        return null;
    }

    private AnimationClip ResolveEyesClip(bool useWhite)
    {
        int eyesId = data.selectedEyesItemId;
        if (TryGetEntry(eyesVisualByItem, eyesId, out PlayerCosmeticClipEntry entry))
        {
            AnimationClip clip = useWhite ? (entry.visualWhite != null ? entry.visualWhite : entry.visual) : entry.visual;
            if (clip != null)
            {
                Debug.Log($"[PlayerCustomization] Eyes id={eyesId} -> {ResolveItemName(eyesId)} visual={clip.name}");
                return clip;
            }
        }

        return null;
    }

    private static bool TryGetEntry(PlayerCosmeticClipEntry[] entries, int idItem, out PlayerCosmeticClipEntry entry)
    {
        if (entries != null)
        {
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i] != null && entries[i].idItem == idItem)
                {
                    entry = entries[i];
                    return true;
                }
            }
        }

        entry = null;
        return false;
    }

    private static string ResolveItemName(int itemId)
    {
        return itemId switch
        {
            11 => "ovalos",
            12 => "rombos",
            13 => "cansado",
            14 => "estrella",
            15 => "happy",
            16 => "pirata",
            17 => "emputado",
            18 => "boy scout/base",
            19 => "engrane",
            20 => "rana",
            21 => "diablito",
            _ => "unknown"
        };
    }
}