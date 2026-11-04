using UnityEngine;
using System;

public class DotNetVersionCheck : MonoBehaviour
{
    void Start()
    {
        Debug.Log($"Versão .NET em uso: {Environment.Version}");
        Debug.Log($"Versão Unity: {Application.unityVersion}");

        try
        {
            var type = Type.GetType("Mono.Runtime");
            Debug.Log(type != null ? "Mono está disponível" : "Mono não está disponível");
        }
        catch
        {
            Debug.Log("Mono não detectado");
        }
    }
}