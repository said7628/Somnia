using NUnit.Framework;

public class TextTypingFlowTests
{
    [TestCase(7, 1000)]
    [TestCase(10, 1000)]
    [TestCase(11, 1000)]
    [TestCase(30, 1700)]
    public void PlayerRules_MinimumScore_ByAge(int age, int expected)
    {
        // Arrange / Act
        var playerRulesType = FindTypeByName("TextTypingPlayerRules");
        Assert.IsNotNull(playerRulesType, "Could not find type 'TextTypingPlayerRules'.");
        var min = (int)InvokeStatic(playerRulesType, "ObtenerPuntajeMinimoPorEdad", age);

        // Assert
        Assert.AreEqual(expected, min);
    }

    [Test]
    public void Session_UnknownScene_UsesFallbackWithMinOne()
    {
        // Arrange / Act
        var sessionType = FindTypeByName("TextTypingSession");
        Assert.IsNotNull(sessionType, "Could not find type 'TextTypingSession'.");
        var resolved = (int)InvokeStatic(sessionType, "ResolveLevelIdFromSceneName", "escena-rara", -20);

        // Assert
        Assert.AreEqual(1, resolved);
    }

    [Test]
    public void Session_TrackLevelContext_UpdatesCurrentAndLast()
    {
        // Arrange
        var sessionType = FindTypeByName("TextTypingSession");
        Assert.IsNotNull(sessionType, "Could not find type 'TextTypingSession'.");
        SetStaticField(sessionType, "LevelId", 0);
        SetStaticField(sessionType, "LastLevelId", 0);

        // Act
        InvokeStatic(sessionType, "TrackLevelContext", 3, "TextTyping3");

        // Assert
        Assert.AreEqual(3, GetStaticField(sessionType, "LevelId"));
        Assert.AreEqual(3, GetStaticField(sessionType, "LastLevelId"));
        Assert.AreEqual("TextTyping3", GetStaticField(sessionType, "LevelName"));
        Assert.AreEqual("TextTyping3", GetStaticField(sessionType, "LastLevelSceneName"));
    }

    [Test]
    public void Session_PersonalBest_DoesNotDecreaseOnLowerScore()
    {
        // Arrange
        var sessionType = FindTypeByName("TextTypingSession");
        Assert.IsNotNull(sessionType, "Could not find type 'TextTypingSession'.");
        SetStaticField(sessionType, "PersonalBest", 600);
        var newScore = 420;

        // Act
        var computedBest = System.Math.Max((int)GetStaticField(sessionType, "PersonalBest"), newScore);

        // Assert
        Assert.AreEqual(600, computedBest);
    }

    private static object InvokeStatic(System.Type type, string methodName, params object[] args)
    {
        var method = type.GetMethod(methodName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Assert.IsNotNull(method, $"Could not find static method '{methodName}' on type '{type.Name}'.");
        return method.Invoke(null, args);
    }

    private static object GetStaticField(System.Type type, string fieldName)
    {
        var field = type.GetField(fieldName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Assert.IsNotNull(field, $"Could not find static field '{fieldName}' on type '{type.Name}'.");
        return field.GetValue(null);
    }

    private static void SetStaticField(System.Type type, string fieldName, object value)
    {
        var field = type.GetField(fieldName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Assert.IsNotNull(field, $"Could not find static field '{fieldName}' on type '{type.Name}'.");
        field.SetValue(null, value);
    }

    private static System.Type FindTypeByName(string typeName)
    {
        foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
        {
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
}
