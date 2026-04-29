using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public class PlayerVisual : MonoBehaviour
{
    public enum Direction
    {
        Down = 0,
        Up = 1,
        Right = 2,
        Left = 3
    }

    [Serializable]
    public class ColorAnimatorByItemId
    {
        public int idItem;
        public RuntimeAnimatorController controller;

        [Header("Legacy fallback")]
        public AnimationClip faceClip;
    }

    [Serializable]
    public class EyesAnimatorByItemId
    {
        public int idItem;
        public RuntimeAnimatorController eyesController;
        public RuntimeAnimatorController eyesWhiteController;

        [Header("Legacy fallback")]
        public AnimationClip eyesClip;
        public AnimationClip eyesWhiteClip;
    }

    [Serializable]
    public class OutfitAnimatorByItemId
    {
        public int idItem;
        public RuntimeAnimatorController controller;

        [Header("Legacy fallback")]
        public AnimationClip outfitClip;
    }

    private const string ParamIsMoving = "IsMoving";
    private const string ParamMoveX = "MoveX";
    private const string ParamMoveY = "MoveY";
    private const string ParamDirection = "Direction";

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

    [Header("Color Animators By Item Id")]
    [SerializeField] private ColorAnimatorByItemId[] colorClipsByItemId;

    [Header("Eyes Animators By Item Id")]
    [SerializeField] private EyesAnimatorByItemId[] eyesClipsByItemId;

    [Header("Outfit Animators By Item Id")]
    [SerializeField] private OutfitAnimatorByItemId[] outfitClipsByItemId;

    [Header("WebGL Sprite Rendering")]
    [SerializeField] private Material webGLSafeSpriteMaterial;
    [SerializeField] private bool forceWebGLSafeSpriteRenderingInEditor;

    private readonly Dictionary<int, ColorAnimatorByItemId> colorMap = new();
    private readonly Dictionary<int, EyesAnimatorByItemId> eyesMap = new();
    private readonly Dictionary<int, OutfitAnimatorByItemId> outfitMap = new();

    private PlayerCustomizationManager data;
    private int selectedColorItemId = 1;
    private int selectedEyesItemId = 11;
    private int selectedOutfitItemId = 18;
    private Material runtimeWebGLSafeMaterial;

    private void Awake()
    {
        BuildMaps();
        EnsureEyesLayering();
        EnsureWebGLSafeSpriteRendering();
    }

    private void Start()
    {
        data = FindObjectOfType<PlayerCustomizationManager>();
        StartCoroutine(ApplyCustomizationWhenReady());
        EnsureWebGLSafeSpriteRendering();
        StartCoroutine(ReapplyWebGLSafeSpriteRenderingAfterFirstFrame());
    }

    private void OnDestroy()
    {
        if (data != null)
        {
            data.OnEquipmentApplied -= HandleEquipmentApplied;
        }
    }

    private IEnumerator ApplyCustomizationWhenReady()
    {
        if (data == null)
        {
            data = FindObjectOfType<PlayerCustomizationManager>();
        }

        if (data == null)
        {
            Debug.LogWarning("[PlayerVisual.LoadOrder][WARN] Equipment not ready, using base fallback ids");
            ApplyEquipment(1, 11, 18);
            yield break;
        }

        Debug.Log("[PlayerVisual.LoadOrder] Waiting for PlayerCustomizationManager equipment");
        data.OnEquipmentApplied -= HandleEquipmentApplied;
        data.OnEquipmentApplied += HandleEquipmentApplied;

        if (data.IsEquipmentReady)
        {
            Debug.Log($"[PlayerVisual.LoadOrder] Equipment ready color={data.selectedFaceColorItemId} eyes={data.selectedEyesItemId} outfit={data.selectedOutfitItemId}");
            Debug.Log("[PlayerVisual.LoadOrder] Applying equipment after backend load");
            ApplyEquipment(data.selectedFaceColorItemId, data.selectedEyesItemId, data.selectedOutfitItemId);
            yield break;
        }

        float timeout = 5f;
        while (!data.IsEquipmentReady && timeout > 0f)
        {
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        if (data.IsEquipmentReady)
        {
            Debug.Log($"[PlayerVisual.LoadOrder] Equipment ready color={data.selectedFaceColorItemId} eyes={data.selectedEyesItemId} outfit={data.selectedOutfitItemId}");
            Debug.Log("[PlayerVisual.LoadOrder] Applying equipment after backend load");
            ApplyEquipment(data.selectedFaceColorItemId, data.selectedEyesItemId, data.selectedOutfitItemId);
            yield break;
        }

        Debug.LogWarning("[PlayerVisual.LoadOrder][WARN] Equipment not ready, using base fallback ids");
        ApplyEquipment(1, 11, 18);
    }

    private void HandleEquipmentApplied(int colorId, int eyesId, int outfitId)
    {
        Debug.Log($"[PlayerVisual.LoadOrder] Equipment ready color={colorId} eyes={eyesId} outfit={outfitId}");
        Debug.Log("[PlayerVisual.LoadOrder] Applying equipment after backend load");
        ApplyEquipment(colorId, eyesId, outfitId);
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

        ApplyEquipment(data.selectedFaceColorItemId, data.selectedEyesItemId, data.selectedOutfitItemId);
    }

    public void ApplyEquipment(int colorId, int eyesId, int outfitId)
    {
        selectedColorItemId = colorId;
        selectedEyesItemId = eyesId;
        selectedOutfitItemId = outfitId;

        Debug.Log($"[PlayerVisual] Equipped color={selectedColorItemId} eyes={selectedEyesItemId} outfit={selectedOutfitItemId}");

        ApplyColorByItemId(colorId);
        ApplyEyesByItemId(eyesId);
        ApplyOutfitByItemId(outfitId);
        EnsureWebGLSafeSpriteRendering();
        LogRendererDiagnostics();

        SetMovementState(false, Vector2.down);
    }

    public void ApplyColorByItemId(int colorId)
    {
        selectedColorItemId = colorId;
        bool appliedController = false;

        if (colorMap.TryGetValue(colorId, out ColorAnimatorByItemId entry) && entry != null)
        {
            if (entry.controller != null && faceAnimator != null)
            {
                faceAnimator.runtimeAnimatorController = entry.controller;
                Debug.Log($"[PlayerVisual] Applied color controller for id={colorId} controller={entry.controller.name}");
                appliedController = true;
            }
            else
            {
                Debug.LogError($"[PlayerVisual] ERROR missing color controller mapping id={colorId}");
            }

            if (!appliedController && entry.faceClip != null)
            {
                PlayLegacyClip(faceAnimator, entry.faceClip);
            }
        }
        else
        {
            Debug.LogError($"[PlayerVisual] ERROR missing color controller mapping id={colorId}");
        }

        if (!appliedController)
        {
            AnimationClip legacyClip = ResolveLegacyColorClip();
            if (legacyClip != null)
            {
                PlayLegacyClip(faceAnimator, legacyClip);
            }
        }
    }

    public void ApplyEyesByItemId(int eyesId)
    {
        selectedEyesItemId = eyesId;
        bool appliedEyesController = false;
        bool appliedEyesWhiteController = false;

        if (eyesMap.TryGetValue(eyesId, out EyesAnimatorByItemId entry) && entry != null)
        {
            if (entry.eyesController != null && eyesNormalAnimator != null)
            {
                eyesNormalAnimator.runtimeAnimatorController = entry.eyesController;
                Debug.Log($"[PlayerVisual] Applied eyes controller for id={eyesId} controller={entry.eyesController.name}");
                appliedEyesController = true;
            }
            else
            {
                Debug.LogError($"[PlayerVisual] ERROR missing eyes controller mapping id={eyesId}");
            }

            if (entry.eyesWhiteController != null && eyesWhiteAnimator != null)
            {
                eyesWhiteAnimator.runtimeAnimatorController = entry.eyesWhiteController;
                Debug.Log($"[PlayerVisual] Applied eyesWhite controller for id={eyesId} controller={entry.eyesWhiteController.name}");
                appliedEyesWhiteController = true;
            }
            else
            {
                Debug.LogError($"[PlayerVisual] ERROR missing eyesWhite controller mapping id={eyesId}");
            }

            if (!appliedEyesController && entry.eyesClip != null)
            {
                PlayLegacyClip(eyesNormalAnimator, entry.eyesClip);
            }

            if (!appliedEyesWhiteController)
            {
                AnimationClip whiteFallback = entry.eyesWhiteClip != null ? entry.eyesWhiteClip : entry.eyesClip;
                if (whiteFallback != null)
                {
                    PlayLegacyClip(eyesWhiteAnimator, whiteFallback);
                }
            }
        }
        else
        {
            Debug.LogError($"[PlayerVisual] ERROR missing eyes controller mapping id={eyesId}");
            Debug.LogError($"[PlayerVisual] ERROR missing eyesWhite controller mapping id={eyesId}");
        }

        if (!appliedEyesController)
        {
            AnimationClip legacyEyes = ResolveLegacyEyesClip();
            if (legacyEyes != null)
            {
                PlayLegacyClip(eyesNormalAnimator, legacyEyes);
            }
        }

        if (!appliedEyesWhiteController)
        {
            AnimationClip legacyEyesWhite = ResolveLegacyEyesWhiteClip();
            if (legacyEyesWhite != null)
            {
                PlayLegacyClip(eyesWhiteAnimator, legacyEyesWhite);
            }
        }
    }

    public void ApplyOutfitByItemId(int outfitId)
    {
        selectedOutfitItemId = outfitId;
        bool appliedController = false;

        if (outfitMap.TryGetValue(outfitId, out OutfitAnimatorByItemId entry) && entry != null)
        {
            if (entry.controller != null && outfitAnimator != null)
            {
                outfitAnimator.runtimeAnimatorController = entry.controller;
                Debug.Log($"[PlayerVisual] Applied outfit controller for id={outfitId} controller={entry.controller.name}");
                appliedController = true;
            }
            else
            {
                Debug.LogError($"[PlayerVisual] ERROR missing outfit controller mapping id={outfitId}");
            }

            if (!appliedController && entry.outfitClip != null)
            {
                PlayLegacyClip(outfitAnimator, entry.outfitClip);
            }
        }
        else
        {
            Debug.LogError($"[PlayerVisual] ERROR missing outfit controller mapping id={outfitId}");
        }

        if (!appliedController)
        {
            AnimationClip legacyClip = ResolveLegacyOutfitClip();
            if (legacyClip != null)
            {
                PlayLegacyClip(outfitAnimator, legacyClip);
            }
        }
    }

    public void SetMovementState(bool isMoving, Vector2 input)
    {
        Vector2 dominantInput = CalculateDominantInput(input, isMoving);
        Direction direction = ResolveDirection(dominantInput);

        ApplyAnimatorMovement(faceAnimator, isMoving, dominantInput, direction);
        ApplyAnimatorMovement(outfitAnimator, isMoving, dominantInput, direction);
        ApplyAnimatorMovement(eyesNormalAnimator, isMoving, dominantInput, direction);
        ApplyAnimatorMovement(eyesWhiteAnimator, isMoving, dominantInput, direction);

        Debug.Log($"[PlayerVisual] Movement params IsMoving={isMoving} MoveX={dominantInput.x} MoveY={dominantInput.y} Direction={(int)direction} color={selectedColorItemId} eyes={selectedEyesItemId} outfit={selectedOutfitItemId}");
    }

    public void SetMovementState(bool isMoving, Direction direction)
    {
        SetMovementState(isMoving, DirectionToVector(direction));
    }

    public void ForceIdle(Direction direction)
    {
        SetMovementState(false, DirectionToVector(direction));
    }

    private static Vector2 CalculateDominantInput(Vector2 input, bool isMoving)
    {
        if (!isMoving || input.sqrMagnitude <= 0.0001f)
        {
            return new Vector2(0f, -1f);
        }

        if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
        {
            return new Vector2(Mathf.Sign(input.x), 0f);
        }

        if (Mathf.Abs(input.y) > 0f)
        {
            return new Vector2(0f, Mathf.Sign(input.y));
        }

        return new Vector2(0f, -1f);
    }

    private static Direction ResolveDirection(Vector2 dominantInput)
    {
        if (dominantInput.y > 0.1f)
        {
            return Direction.Up;
        }

        if (dominantInput.x > 0.1f)
        {
            return Direction.Right;
        }

        if (dominantInput.x < -0.1f)
        {
            return Direction.Left;
        }

        return Direction.Down;
    }

    private static Vector2 DirectionToVector(Direction direction)
    {
        return direction switch
        {
            Direction.Up => Vector2.up,
            Direction.Right => Vector2.right,
            Direction.Left => Vector2.left,
            _ => Vector2.down
        };
    }

    private static void ApplyAnimatorMovement(Animator animator, bool isMoving, Vector2 dominantInput, Direction direction)
    {
        if (animator == null)
        {
            return;
        }

        SetBoolIfExists(animator, ParamIsMoving, isMoving);
        SetFloatIfExists(animator, ParamMoveX, dominantInput.x);
        SetFloatIfExists(animator, ParamMoveY, dominantInput.y);
        SetIntIfExists(animator, ParamDirection, (int)direction);
    }

    private static void SetBoolIfExists(Animator animator, string name, bool value)
    {
        if (HasParameter(animator, name, AnimatorControllerParameterType.Bool))
        {
            animator.SetBool(name, value);
        }
    }

    private static void SetFloatIfExists(Animator animator, string name, float value)
    {
        if (HasParameter(animator, name, AnimatorControllerParameterType.Float))
        {
            animator.SetFloat(name, value);
        }
    }

    private static void SetIntIfExists(Animator animator, string name, int value)
    {
        if (HasParameter(animator, name, AnimatorControllerParameterType.Int))
        {
            animator.SetInteger(name, value);
        }
    }

    private static bool HasParameter(Animator animator, string name, AnimatorControllerParameterType type)
    {
        if (animator == null)
        {
            return false;
        }

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.type == type && parameter.name == name)
            {
                return true;
            }
        }

        return false;
    }

    private static void PlayLegacyClip(Animator animator, AnimationClip clip)
    {
        if (animator == null || clip == null)
        {
            return;
        }

        animator.Play(clip.name, 0, 0f);
    }

    private void EnsureEyesLayering()
    {
        if (eyesWhiteAnimator == null || eyesNormalAnimator == null)
        {
            return;
        }

        SpriteRenderer eyesWhiteRenderer = eyesWhiteAnimator.GetComponent<SpriteRenderer>();
        SpriteRenderer eyesRenderer = eyesNormalAnimator.GetComponent<SpriteRenderer>();
        if (eyesWhiteRenderer == null || eyesRenderer == null)
        {
            return;
        }

        if (eyesWhiteRenderer.sortingLayerID != eyesRenderer.sortingLayerID)
        {
            eyesWhiteRenderer.sortingLayerID = eyesRenderer.sortingLayerID;
        }

        eyesWhiteRenderer.sortingOrder = 101;
        eyesRenderer.sortingOrder = 102;

        if (faceAnimator != null)
        {
            SpriteRenderer faceRenderer = faceAnimator.GetComponent<SpriteRenderer>();
            if (faceRenderer != null)
            {
                faceRenderer.sortingOrder = 100;
            }
        }

        if (outfitAnimator != null)
        {
            SpriteRenderer outfitRenderer = outfitAnimator.GetComponent<SpriteRenderer>();
            if (outfitRenderer != null)
            {
                outfitRenderer.sortingOrder = 103;
            }
        }

        Debug.Log("[PlayerVisual] EyesWhite below Eyes");
    }

    private void EnsureWebGLSafeSpriteRendering()
    {
        bool shouldApplyWebGLSafePath = Application.platform == RuntimePlatform.WebGLPlayer || forceWebGLSafeSpriteRenderingInEditor;
        if (!shouldApplyWebGLSafePath)
        {
            return;
        }

        Debug.Log("[PlayerVisual.WebGLFix] Applying WebGL-safe sprite rendering");

        Material safeMaterial = ResolveWebGLSafeMaterial();
        if (safeMaterial == null)
        {
            Debug.LogError("[PlayerVisual.WebGLFix][ERROR] Missing shader/material fallback");
            return;
        }

        ApplyWebGLSafeMaterial(faceAnimator, "Face", 100);
        ApplyWebGLSafeMaterial(eyesWhiteAnimator, "Eyes Blancos", 101);
        ApplyWebGLSafeMaterial(eyesNormalAnimator, "Eyes", 102);
        ApplyWebGLSafeMaterial(outfitAnimator, "Outfit", 103);

        Material ResolveWebGLSafeMaterial()
        {
            if (webGLSafeSpriteMaterial != null)
            {
                return webGLSafeSpriteMaterial;
            }

            if (runtimeWebGLSafeMaterial != null)
            {
                return runtimeWebGLSafeMaterial;
            }

            Shader spriteShader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (spriteShader == null)
            {
                spriteShader = Shader.Find("Sprites/Default");
            }
            if (spriteShader == null)
            {
                return null;
            }

            runtimeWebGLSafeMaterial = new Material(spriteShader)
            {
                name = "[PlayerVisual] WebGL Safe Sprite Material"
            };
            return runtimeWebGLSafeMaterial;
        }

        void ApplyWebGLSafeMaterial(Animator targetAnimator, string layerName, int sortingOrder)
        {
            if (targetAnimator == null)
            {
                return;
            }

            SpriteRenderer renderer = targetAnimator.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                return;
            }

            renderer.enabled = true;
            if (renderer.color.a <= 0f)
            {
                Color c = renderer.color;
                c.a = 1f;
                renderer.color = c;
                Debug.LogWarning($"[PlayerVisual.WebGLFix][WARN] Renderer had alpha 0, forced alpha 1 renderer={renderer.name}");
            }
            renderer.color = Color.white;
            renderer.drawMode = SpriteDrawMode.Simple;
            renderer.maskInteraction = SpriteMaskInteraction.None;
            renderer.sortingOrder = sortingOrder;
            renderer.material = safeMaterial;
            if (renderer.sprite == null)
            {
                Debug.LogError($"[PlayerVisual.WebGLFix][ERROR] Missing sprite on renderer={renderer.name}");
            }
            string spriteName = renderer.sprite != null ? renderer.sprite.name : "null";
            string textureName = renderer.sprite != null && renderer.sprite.texture != null ? renderer.sprite.texture.name : "null";
            string materialName = renderer.sharedMaterial != null ? renderer.sharedMaterial.name : "null";
            string safeShader = renderer.sharedMaterial != null && renderer.sharedMaterial.shader != null ? renderer.sharedMaterial.shader.name : "null";
            Debug.Log($"[PlayerVisual.WebGLFix] Renderer={renderer.name} sprite={spriteName} texture={textureName} material={materialName} shader={safeShader} color={renderer.color} order={renderer.sortingOrder} drawMode={renderer.drawMode} bounds={renderer.bounds}");
        }
    }

    private System.Collections.IEnumerator ReapplyWebGLSafeSpriteRenderingAfterFirstFrame()
    {
        yield return null;
        EnsureEyesLayering();
        EnsureWebGLSafeSpriteRendering();
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            Debug.Log("[PlayerVisual.WebGLFix] Re-applied after animator first frame");
        }
    }

    private void LogRendererDiagnostics()
    {
        LogRenderer(faceAnimator, "Face");
        LogRenderer(outfitAnimator, "Outfit");
        LogRenderer(eyesNormalAnimator, "Eyes");
        LogRenderer(eyesWhiteAnimator, "EyesWhite");

        static void LogRenderer(Animator targetAnimator, string layerName)
        {
            if (targetAnimator == null)
            {
                Debug.LogWarning($"[PlayerVisual.WebGLDebug] layer={layerName} missing animator reference");
                return;
            }

            SpriteRenderer renderer = targetAnimator.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                Debug.LogWarning($"[PlayerVisual.WebGLDebug] layer={layerName} missing SpriteRenderer on {targetAnimator.name}");
                return;
            }

            string spriteName = renderer.sprite != null ? renderer.sprite.name : "null";
            string materialName = renderer.sharedMaterial != null ? renderer.sharedMaterial.name : "null";
            string shaderName = renderer.sharedMaterial != null && renderer.sharedMaterial.shader != null
                ? renderer.sharedMaterial.shader.name
                : "null";
            string controllerName = targetAnimator.runtimeAnimatorController != null
                ? targetAnimator.runtimeAnimatorController.name
                : "null";

            Debug.Log(
                $"[PlayerVisual.WebGLDebug] layer={layerName} renderer={renderer.name} sprite={spriteName} material={materialName} " +
                $"shader={shaderName} sortingLayer={renderer.sortingLayerName} sortingOrder={renderer.sortingOrder} " +
                $"color={renderer.color} alpha={renderer.color.a:F2} enabled={renderer.enabled} controller={controllerName}"
            );
        }
    }

    private void BuildMaps()
    {
        colorMap.Clear();
        eyesMap.Clear();
        outfitMap.Clear();

        if (colorClipsByItemId != null)
        {
            foreach (ColorAnimatorByItemId entry in colorClipsByItemId)
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
            foreach (EyesAnimatorByItemId entry in eyesClipsByItemId)
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
            foreach (OutfitAnimatorByItemId entry in outfitClipsByItemId)
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