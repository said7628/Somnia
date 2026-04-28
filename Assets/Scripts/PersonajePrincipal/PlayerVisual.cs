using System.Collections.Generic;
using UnityEngine;

public class PlayerVisual : MonoBehaviour
{
    [System.Serializable]
    public class PlayerColorClipEntry
    {
        public int idItem;
        public AnimationClip faceClip;
    }

    [System.Serializable]
    public class PlayerEyesClipEntry
    {
        public int idItem;
        public AnimationClip eyesClip;
        public AnimationClip eyesWhiteClip;
    }

    [System.Serializable]
    public class PlayerOutfitClipEntry
    {
        public int idItem;
        public AnimationClip outfitClip;
    }

    [Header("Animators")]
    [SerializeField] private Animator faceAnimator;
    [SerializeField] private Animator outfitAnimator;

    [Header("Eyes (Separados)")]
    [SerializeField] private Animator eyesNormalAnimator;
    [SerializeField] private Animator eyesWhiteAnimator;

    [Header("Animation Clips (legacy fallback)")]
    [SerializeField] private AnimationClip[] faceClips;
    [SerializeField] private AnimationClip[] outfitClips;
    [SerializeField] private AnimationClip[] eyesClips;
    [SerializeField] private AnimationClip[] eyesWhiteClips;

    [Header("Color Clips By Item Id")]
    [SerializeField] private PlayerColorClipEntry[] colorClipsByItemId;

    [Header("Eyes Clips By Item Id")]
    [SerializeField] private PlayerEyesClipEntry[] eyesClipsByItemId;

    [Header("Outfit Clips By Item Id")]
    [SerializeField] private PlayerOutfitClipEntry[] outfitClipsByItemId;

    private readonly Dictionary<int, PlayerColorClipEntry> colorMap = new();
    private readonly Dictionary<int, PlayerEyesClipEntry> eyesMap = new();
    private readonly Dictionary<int, PlayerOutfitClipEntry> outfitMap = new();

    private PlayerCustomizationManager data;
    private int currentColorItemId = 1;

    private void Awake()
    {
        BuildMaps();
    }

    private void Start()
    {
        data = FindObjectOfType<PlayerCustomizationManager>();
        ApplyCustomization();
    }

    public void ApplyCustomization()
    {
        if (data == null)
        {
            data = FindObjectOfType<PlayerCustomizationManager>();
        }

        if (data == null)
        {
            Debug.LogError("[PlayerVisual] ERROR missing PlayerCustomizationManager");
            return;
        }

        ApplyColorByItemId(data.selectedFaceColorItemId);
        ApplyEyesByItemId(data.selectedEyesItemId);
        ApplyOutfitByItemId(data.selectedOutfitItemId);
    }

    public void ApplyColorByItemId(int colorId)
    {
        bool found = colorMap.TryGetValue(colorId, out PlayerColorClipEntry entry) && entry.faceClip != null;
        AnimationClip clip = found ? entry.faceClip : ResolveLegacyColorClip();
        string clipName = clip != null ? clip.name : "null";

        Debug.Log($"[PlayerVisual] ApplyColor id={colorId} found={found} clip={clipName}");

        if (clip == null)
        {
            Debug.LogError($"[PlayerVisual] ERROR missing color clip mapping id={colorId}");
            return;
        }

        currentColorItemId = colorId;
        faceAnimator.Play(clip.name, 0, 0f);
    }

    public void ApplyEyesByItemId(int eyesId)
    {
        bool found = eyesMap.TryGetValue(eyesId, out PlayerEyesClipEntry entry);
        AnimationClip normalClip = found ? entry.eyesClip : ResolveLegacyEyesClip();
        AnimationClip whiteClip = found
            ? (entry.eyesWhiteClip != null ? entry.eyesWhiteClip : entry.eyesClip)
            : ResolveLegacyEyesWhiteClip();

        string normalName = normalClip != null ? normalClip.name : "null";
        string whiteName = whiteClip != null ? whiteClip.name : "null";
        Debug.Log($"[PlayerVisual] ApplyEyes id={eyesId} found={found} eyes={normalName} white={whiteName}");

        if (normalClip == null)
        {
            Debug.LogError($"[PlayerVisual] ERROR missing eyes clip mapping id={eyesId}");
            return;
        }

        eyesNormalAnimator.Play(normalClip.name, 0, 0f);

        if (eyesWhiteAnimator != null && whiteClip != null)
        {
            eyesWhiteAnimator.Play(whiteClip.name, 0, 0f);
        }
        else if (currentColorItemId == 10)
        {
            Debug.LogError($"[PlayerVisual] ERROR missing eyes clip mapping id={eyesId}");
        }
    }

    public void ApplyOutfitByItemId(int outfitId)
    {
        bool found = outfitMap.TryGetValue(outfitId, out PlayerOutfitClipEntry entry) && entry.outfitClip != null;
        AnimationClip clip = found ? entry.outfitClip : ResolveLegacyOutfitClip();
        string clipName = clip != null ? clip.name : "null";

        Debug.Log($"[PlayerVisual] ApplyOutfit id={outfitId} found={found} clip={clipName}");

        if (clip == null)
        {
            Debug.LogError($"[PlayerVisual] ERROR missing outfit clip mapping id={outfitId}");
            return;
        }

        outfitAnimator.Play(clip.name, 0, 0f);
    }

    private void BuildMaps()
    {
        colorMap.Clear();
        eyesMap.Clear();
        outfitMap.Clear();

        if (colorClipsByItemId != null)
        {
            foreach (PlayerColorClipEntry entry in colorClipsByItemId)
            {
                if (entry == null)
                {
                    continue;
                }

                colorMap[entry.idItem] = entry;
            }
        }

        if (eyesClipsByItemId != null)
        {
            foreach (PlayerEyesClipEntry entry in eyesClipsByItemId)
            {
                if (entry == null)
                {
                    continue;
                }

                eyesMap[entry.idItem] = entry;
            }
        }

        if (outfitClipsByItemId != null)
        {
            foreach (PlayerOutfitClipEntry entry in outfitClipsByItemId)
            {
                if (entry == null)
                {
                    continue;
                }

                outfitMap[entry.idItem] = entry;
            }
        }
    }

    private AnimationClip ResolveLegacyColorClip()
    {
        if (data != null && data.selectedFaceColor >= 0 && data.selectedFaceColor < faceClips.Length)
        {
            return faceClips[data.selectedFaceColor];
        }

        return null;
    }

    private AnimationClip ResolveLegacyEyesClip()
    {
        if (data != null && data.selectedEyes >= 0 && data.selectedEyes < eyesClips.Length)
        {
            return eyesClips[data.selectedEyes];
        }

        return null;
    }

    private AnimationClip ResolveLegacyEyesWhiteClip()
    {
        if (data != null && data.selectedEyes >= 0 && data.selectedEyes < eyesWhiteClips.Length)
        {
            return eyesWhiteClips[data.selectedEyes];
        }

        return null;
    }

    private AnimationClip ResolveLegacyOutfitClip()
    {
        if (data != null && data.selectedOutfit >= 0 && data.selectedOutfit < outfitClips.Length)
        {
            return outfitClips[data.selectedOutfit];
        }

        return null;
    }
}