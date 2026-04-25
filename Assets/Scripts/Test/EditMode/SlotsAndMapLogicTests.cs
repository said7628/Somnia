using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class SlotsAndMapLogicTests
{
    [Test]
    public void GuardadoBeta_GetDefaultSlotName_ReturnsExpectedConfiguredName()
    {
        // Arrange
        var guardadoType = FindTypeByName("GuardadoBeta");
        Assert.IsNotNull(guardadoType, "Could not find type 'GuardadoBeta'.");
        var go = new GameObject("guardado");
        var guardado = go.AddComponent(guardadoType);
        SetPrivateField(guardado, "slot1DefaultName", "Mi Partida 1");

        // Act
        var slotName = (string)InvokePrivate(guardado, "GetDefaultSlotName", 1);

        // Assert
        Assert.AreEqual("Mi Partida 1", slotName);
        Object.DestroyImmediate(go);
    }

    [Test]
    public void GuardadoBeta_ComputeHighestIsland_WithNullProgress_ReturnsOne()
    {
        // Arrange / Act
        var guardadoType = FindTypeByName("GuardadoBeta");
        Assert.IsNotNull(guardadoType, "Could not find type 'GuardadoBeta'.");
        var highest = (int)InvokePrivateStatic(guardadoType, "ComputeHighestIsland", null);

        // Assert
        Assert.AreEqual(1, highest);
    }

    [Test]
    public void MapaIslasUI_ComputeHighestIsland_WhenNoProgress_ReturnsOne()
    {
        // Arrange
        var mapaType = FindTypeByName("MapaIslasUI");
        Assert.IsNotNull(mapaType, "Could not find type 'MapaIslasUI'.");
        var go = new GameObject("mapa");
        var mapa = go.AddComponent(mapaType);

        // Act
        var highest = (int)InvokePrivate(mapa, "ComputeHighestIsland", null);

        // Assert
        Assert.AreEqual(1, highest);
        Object.DestroyImmediate(go);
    }

    private static object InvokePrivate(object target, string method, params object[] args)
    {
        var mi = target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic);
        return mi.Invoke(target, args);
    }

    private static object InvokePrivateStatic(System.Type type, string method, params object[] args)
    {
        var mi = type.GetMethod(method, BindingFlags.Static | BindingFlags.NonPublic);
        return mi.Invoke(null, args);
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        var fi = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        fi.SetValue(target, value);
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
