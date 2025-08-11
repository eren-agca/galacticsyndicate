using System.Globalization;
using System.Threading;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Unity Editor'ün "Türkçe İ" karakteri gibi yerel dil ayarlarından kaynaklanan
/// derleme hatalarını önlemek için, editörün çalışma kültürünü programlama için
/// güvenli olan "Invariant Culture" olarak ayarlar.
/// </summary>
[InitializeOnLoad]
public class ForceInvariantCulture
{
    static ForceInvariantCulture()
    {
        // Bu script, Unity Editor'ü her başlattığında veya bir script derlendiğinde çalışır.
            
        // Tüm yeni thread'lerin varsayılan kültürünü ayarla.
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

        // Mevcut ana thread'in kültürünü ayarla.
        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
        Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

        Debug.Log("[ForceInvariantCulture] Editor's culture has been set to Invariant to prevent locale-based compilation errors.");
    }
}