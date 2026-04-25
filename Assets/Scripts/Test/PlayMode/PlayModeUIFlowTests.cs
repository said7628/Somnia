using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class PlayModeUIFlowTests
{
    [UnityTest]
    public IEnumerator SaveRanura_ShowDataAndEmpty_UpdatesVisualState()
    {
        // Arrange
        var fixture = BuildSlotFixture();

        // Act
        InvokeMethod(fixture.Slot, "ShowData", 3);
        yield return null;

        // Assert
        Assert.IsFalse(fixture.EmptyObject.activeSelf);
        Assert.IsTrue(fixture.Preview.gameObject.activeSelf);
        Assert.AreSame(fixture.Island3, fixture.Preview.sprite);

        // Act
        InvokeMethod(fixture.Slot, "ShowEmpty");
        yield return null;

        // Assert
        Assert.IsTrue(fixture.EmptyObject.activeSelf);
        Assert.IsFalse(fixture.Preview.gameObject.activeSelf);

        Object.Destroy(fixture.Root);
    }

    [UnityTest]
    public IEnumerator ScoreManager_CorrectAndWrongAnswer_UpdatesScoreAndMultiplierText()
    {
        // Arrange
        var scoreManagerType = FindTypeByName("ScoreManager");
        Assert.IsNotNull(scoreManagerType, "Could not find type 'ScoreManager' in loaded assemblies.");
        var root = new GameObject("score");
        var scoreTextGo = new GameObject("scoreText", typeof(TextMeshProUGUI));
        var multTextGo = new GameObject("multText", typeof(TextMeshProUGUI));
        var manager = root.AddComponent(scoreManagerType);

        SetField(manager, "scoreText", scoreTextGo.GetComponent<TextMeshProUGUI>());
        SetField(manager, "multiplierText", multTextGo.GetComponent<TextMeshProUGUI>());

        yield return null; // Start()

        // Act
        for (int i = 0; i < 5; i++)
        {
            InvokeMethod(manager, "CorrectAnswer");
        }

        // Assert
        Assert.AreEqual(300, InvokeMethod(manager, "GetScore")); // 4*50 + (5th with x2)
        Assert.AreEqual("x2", multTextGo.GetComponent<TextMeshProUGUI>().text);

        // Act
        InvokeMethod(manager, "WrongAnswer");

        // Assert
        Assert.AreEqual("x1", multTextGo.GetComponent<TextMeshProUGUI>().text);

        Object.Destroy(root);
        Object.Destroy(scoreTextGo);
        Object.Destroy(multTextGo);
    }

    [UnityTest]
    public IEnumerator ResultadoNivelUI_LoadsScoreAndBestFromSession()
    {
        // Arrange
        var sessionType = FindTypeByName("TextTypingSession");
        var resultadoUiType = FindTypeByName("ResultadoNivelUITextTyping");
        Assert.IsNotNull(sessionType, "Could not find type 'TextTypingSession' in loaded assemblies.");
        Assert.IsNotNull(resultadoUiType, "Could not find type 'ResultadoNivelUITextTyping' in loaded assemblies.");

        SetStaticField(sessionType, "CurrentScore", 480);
        SetStaticField(sessionType, "MinimumScore", 500);
        SetStaticField(sessionType, "PersonalBest", 650);
        SetStaticField(sessionType, "Passed", false);

        var go = new GameObject("resultado");
        var score = new GameObject("score", typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        var min = new GameObject("min", typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        var max = new GameObject("max", typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        var state = new GameObject("state", typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();

        var ui = go.AddComponent(resultadoUiType);
        SetField(ui, "scoreValue", score);
        SetField(ui, "minScoreValue", min);
        SetField(ui, "maxScoreValue", max);
        SetField(ui, "estadoValue", state);

        // Act
        yield return null; // Start()

        // Assert
        Assert.AreEqual("480", score.text);
        Assert.AreEqual("500", min.text);
        Assert.AreEqual("650", max.text);
        Assert.AreEqual("No completado", state.text);

        Object.Destroy(go);
    }

    private static SlotFixture BuildSlotFixture()
    {
        var saveRanuraType = FindTypeByName("SaveRanura");
        Assert.IsNotNull(saveRanuraType, "Could not find type 'SaveRanura' in loaded assemblies.");

        var root = new GameObject("slot", typeof(RectTransform), typeof(Button), saveRanuraType);
        var empty = new GameObject("vacio");
        empty.transform.SetParent(root.transform);

        var previewGo = new GameObject("preview", typeof(Image));
        previewGo.transform.SetParent(root.transform);
        var preview = previewGo.GetComponent<Image>();

        var numGo = new GameObject("num", typeof(TextMeshProUGUI));
        numGo.transform.SetParent(root.transform);

        var slot = root.GetComponent(saveRanuraType);
        var island1 = CreateSprite();
        var island2 = CreateSprite();
        var island3 = CreateSprite();

        SetField(slot, "vacioObject", empty);
        SetField(slot, "previewImage", preview);
        SetField(slot, "numText", numGo.GetComponent<TextMeshProUGUI>());
        SetField(slot, "slotButton", root.GetComponent<Button>());
        SetField(slot, "isla1Sprite", island1);
        SetField(slot, "isla2Sprite", island2);
        SetField(slot, "isla3Sprite", island3);

        return new SlotFixture { Root = root, Slot = slot, EmptyObject = empty, Preview = preview, Island3 = island3 };
    }

    private static Sprite CreateSprite()
    {
        var texture = new Texture2D(2, 2);
        return Sprite.Create(texture, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));
    }

    private static void SetField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        field.SetValue(target, value);
    }

    private static object InvokeMethod(object target, string methodName, params object[] args)
    {
        var method = target.GetType().GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(method, $"Could not find method '{methodName}' on type '{target.GetType().Name}'.");
        return method.Invoke(target, args);
    }

    private static void SetStaticField(System.Type type, string fieldName, object value)
    {
        var field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(field, $"Could not find static field '{fieldName}' on type '{type.Name}'.");
        field.SetValue(null, value);
    }

    private static System.Type FindTypeByName(string typeName)
    {
        foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
        {
            var directType = assembly.GetType(typeName);
            if (directType != null)
            {
                return directType;
            }

            foreach (var type in assembly.GetTypes())
            {
                if (type.Name == typeName)
                {
                    return type;
                }
            }
        }

        return null;
    }

    private class SlotFixture
    {
        public GameObject Root;
        public Component Slot;
        public GameObject EmptyObject;
        public Image Preview;
        public Sprite Island3;
    }
}
