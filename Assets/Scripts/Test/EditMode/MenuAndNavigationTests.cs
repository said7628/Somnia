using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

public class MenuAndNavigationTests
{
    [Test]
    public void MenuManager_NewGameAction_LoadsExpectedScene()
    {
        // Arrange
        var menuManagerType = FindTypeByName("MenuManager");
        Assert.IsNotNull(menuManagerType, "Could not find type 'MenuManager'.");
        var go = new GameObject("menu");
        var manager = go.AddComponent(menuManagerType);
        string loadedScene = null;
        SetStaticField(menuManagerType, "SceneLoader", (Action<string>)(scene => loadedScene = scene));

        // Act
        InvokePrivate(manager, "IrANuevoJuego");

        // Assert
        Assert.AreEqual("Nuevo juego", loadedScene);
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void NuevoJuegoManager_MapAction_LoadsMapaScene()
    {
        // Arrange
        var nuevoJuegoManagerType = FindTypeByName("NuevoJuegoManager");
        Assert.IsNotNull(nuevoJuegoManagerType, "Could not find type 'NuevoJuegoManager'.");
        var go = new GameObject("nuevo-juego");
        var manager = go.AddComponent(nuevoJuegoManagerType);
        string loadedScene = null;
        SetStaticField(nuevoJuegoManagerType, "SceneLoader", (Action<string>)(scene => loadedScene = scene));

        // Act
        InvokePrivate(manager, "IrAMapa");

        // Assert
        Assert.AreEqual("Mapa", loadedScene);
        UnityEngine.Object.DestroyImmediate(go);
    }

    [Test]
    public void NavegacionIsla1_SetButtonState_DisablesButtonAndSetsTooltipWhenLocked()
    {
        // Arrange
        var navegacionType = FindTypeByName("Navegacion_Isla1");
        Assert.IsNotNull(navegacionType, "Could not find type 'Navegacion_Isla1'.");
        var button = new Button();

        // Act
        InvokePrivateStatic(navegacionType, "SetButtonState", button, false, "Level 2 locked");

        // Assert
        Assert.IsFalse(button.enabledSelf);
        Assert.AreEqual("Level 2 locked", button.tooltip);
    }

    private static void InvokePrivate(object target, string method)
    {
        var mi = target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic);
        mi.Invoke(target, null);
    }

    private static void InvokePrivateStatic(Type type, string method, params object[] args)
    {
        var mi = type.GetMethod(method, BindingFlags.Static | BindingFlags.NonPublic);
        mi.Invoke(null, args);
    }

    private static void SetStaticField(Type type, string fieldName, object value)
    {
        var fi = type.GetField(fieldName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        fi.SetValue(null, value);
    }

    private static Type FindTypeByName(string typeName)
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
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
