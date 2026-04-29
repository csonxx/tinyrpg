using System;
using Core.Accessibility;
using Core.Scene;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Unit tests for the Accessibility System.
/// Covers text size mode, colorblind matrices, reduce motion transition override,
/// and settings persistence.
///
/// Requires a bootstrapped Unity runtime environment (PlayMode).
/// </summary>
public class AccessibilitySystemTests
{
    #region TextSizeMode Tests

    [Test]
    [TestCase(TextSizeMode.Small, "0.8")]
    [TestCase(TextSizeMode.Normal, "1.0")]
    [TestCase(TextSizeMode.Large, "1.4")]
    public void TextSizeScales_GetScaleValue_ReturnsCorrectString(TextSizeMode mode, string expected)
    {
        string result = TextSizeScales.GetScaleValue(mode);
        Assert.AreEqual(expected, result);
    }

    [Test]
    [TestCase(TextSizeMode.Small, 0.8f)]
    [TestCase(TextSizeMode.Normal, 1.0f)]
    [TestCase(TextSizeMode.Large, 1.4f)]
    public void TextSizeScales_GetScaleMultiplier_ReturnsCorrectFloat(TextSizeMode mode, float expected)
    {
        float result = TextSizeScales.GetScaleMultiplier(mode);
        Assert.AreEqual(expected, result, 0.0001f);
    }

    [Test]
    public void TextSizeScales_UnknownMode_ReturnsNormal([Values(-1, 99)] int unknownIndex)
    {
        var mode = (TextSizeMode)unknownIndex;
        Assert.AreEqual("1.0", TextSizeScales.GetScaleValue(mode));
        Assert.AreEqual(1.0f, TextSizeScales.GetScaleMultiplier(mode));
    }

    #endregion

    #region ColorblindMode Tests

    [Test]
    public void ColorblindMatrices_None_ReturnsIdentityMatrix()
    {
        float[] matrix = ColorblindMatrices.GetMatrix(ColorblindMode.None);

        Assert.AreEqual(9, matrix.Length);
        Assert.AreEqual(1f, matrix[0], 0.0001f); // row0: (1,0,0)
        Assert.AreEqual(0f, matrix[1], 0.0001f);
        Assert.AreEqual(0f, matrix[2], 0.0001f);
        Assert.AreEqual(0f, matrix[3], 0.0001f); // row1: (0,1,0)
        Assert.AreEqual(1f, matrix[4], 0.0001f);
        Assert.AreEqual(0f, matrix[5], 0.0001f);
        Assert.AreEqual(0f, matrix[6], 0.0001f); // row2: (0,0,1)
        Assert.AreEqual(0f, matrix[7], 0.0001f);
        Assert.AreEqual(1f, matrix[8], 0.0001f);
    }

    [Test]
    public void ColorblindMatrices_Deuteranopia_HasValidValues()
    {
        float[] matrix = ColorblindMatrices.GetMatrix(ColorblindMode.Deuteranopia);

        Assert.AreEqual(9, matrix.Length);

        // All values should be in [0, 1]
        foreach (float v in matrix)
        {
            Assert.GreaterOrEqual(v, 0f);
            Assert.LessOrEqual(v, 1f);
        }

        // Each row should sum to approximately 1 (color is preserved)
        float row0Sum = matrix[0] + matrix[1] + matrix[2];
        float row1Sum = matrix[3] + matrix[4] + matrix[5];
        float row2Sum = matrix[6] + matrix[7] + matrix[8];
        Assert.AreEqual(1f, row0Sum, 0.0001f, "Deuteranopia row 0 should sum to 1");
        Assert.AreEqual(1f, row1Sum, 0.0001f, "Deuteranopia row 1 should sum to 1");
        Assert.AreEqual(1f, row2Sum, 0.0001f, "Deuteranopia row 2 should sum to 1");
    }

    [Test]
    public void ColorblindMatrices_Protanopia_HasValidValues()
    {
        float[] matrix = ColorblindMatrices.GetMatrix(ColorblindMode.Protanopia);

        Assert.AreEqual(9, matrix.Length);

        // All values should be in [0, 1]
        foreach (float v in matrix)
        {
            Assert.GreaterOrEqual(v, 0f);
            Assert.LessOrEqual(v, 1f);
        }

        // Each row should sum to approximately 1 (color is preserved)
        float row0Sum = matrix[0] + matrix[1] + matrix[2];
        float row1Sum = matrix[3] + matrix[4] + matrix[5];
        float row2Sum = matrix[6] + matrix[7] + matrix[8];
        Assert.AreEqual(1f, row0Sum, 0.0001f, "Protanopia row 0 should sum to 1");
        Assert.AreEqual(1f, row1Sum, 0.0001f, "Protanopia row 1 should sum to 1");
        Assert.AreEqual(1f, row2Sum, 0.0001f, "Protanopia row 2 should sum to 1");
    }

    [Test]
    public void ColorblindMatrices_Deuteranopia_NotIdentity()
    {
        float[] matrix = ColorblindMatrices.GetMatrix(ColorblindMode.Deuteranopia);
        float[] identity = ColorblindMatrices.GetMatrix(ColorblindMode.None);

        // Should be different from identity
        bool anyDifferent = false;
        for (int i = 0; i < 9; i++)
        {
            if (Mathf.Abs(matrix[i] - identity[i]) > 0.0001f)
            {
                anyDifferent = true;
                break;
            }
        }
        Assert.IsTrue(anyDifferent, "Deuteranopia matrix should differ from identity");
    }

    [Test]
    public void ColorblindMatrices_Protanopia_NotIdentity()
    {
        float[] matrix = ColorblindMatrices.GetMatrix(ColorblindMode.Protanopia);
        float[] identity = ColorblindMatrices.GetMatrix(ColorblindMode.None);

        // Should be different from identity
        bool anyDifferent = false;
        for (int i = 0; i < 9; i++)
        {
            if (Mathf.Abs(matrix[i] - identity[i]) > 0.0001f)
            {
                anyDifferent = true;
                break;
            }
        }
        Assert.IsTrue(anyDifferent, "Protanopia matrix should differ from identity");
    }

    [Test]
    public void ColorblindMatrices_Deuteranopia_AndProtanopia_Differ()
    {
        float[] deut = ColorblindMatrices.GetMatrix(ColorblindMode.Deuteranopia);
        float[] prot = ColorblindMatrices.GetMatrix(ColorblindMode.Protanopia);

        bool anyDifferent = false;
        for (int i = 0; i < 9; i++)
        {
            if (Mathf.Abs(deut[i] - prot[i]) > 0.0001f)
            {
                anyDifferent = true;
                break;
            }
        }
        Assert.IsTrue(anyDifferent, "Deuteranopia and Protanopia matrices should differ");
    }

    #endregion

    #region PlayerPrefsAccessibilityBackend Tests

    [Test]
    public void PlayerPrefsBackend_DefaultValues()
    {
        var backend = new PlayerPrefsAccessibilityBackend();

        Assert.AreEqual(TextSizeMode.Normal, backend.TextSize);
        Assert.AreEqual(ColorblindMode.None, backend.ColorblindMode);
        Assert.IsFalse(backend.ReduceMotionEnabled);
    }

    [Test]
    public void PlayerPrefsBackend_SetAndGetTextSize()
    {
        var backend = new PlayerPrefsAccessibilityBackend();

        backend.TextSize = TextSizeMode.Large;
        Assert.AreEqual(TextSizeMode.Large, backend.TextSize);

        backend.TextSize = TextSizeMode.Small;
        Assert.AreEqual(TextSizeMode.Small, backend.TextSize);
    }

    [Test]
    public void PlayerPrefsBackend_SetAndGetColorblindMode()
    {
        var backend = new PlayerPrefsAccessibilityBackend();

        backend.ColorblindMode = ColorblindMode.Deuteranopia;
        Assert.AreEqual(ColorblindMode.Deuteranopia, backend.ColorblindMode);

        backend.ColorblindMode = ColorblindMode.Protanopia;
        Assert.AreEqual(ColorblindMode.Protanopia, backend.ColorblindMode);
    }

    [Test]
    public void PlayerPrefsBackend_SetAndGetReduceMotion()
    {
        var backend = new PlayerPrefsAccessibilityBackend();

        backend.ReduceMotionEnabled = true;
        Assert.IsTrue(backend.ReduceMotionEnabled);

        backend.ReduceMotionEnabled = false;
        Assert.IsFalse(backend.ReduceMotionEnabled);
    }

    [Test]
    public void PlayerPrefsBackend_SaveAndLoad_PreservesValues()
    {
        var backend = new PlayerPrefsAccessibilityBackend();

        backend.TextSize = TextSizeMode.Large;
        backend.ColorblindMode = ColorblindMode.Protanopia;
        backend.ReduceMotionEnabled = true;
        backend.Save();

        // Create a new backend instance to simulate reload
        var newBackend = new PlayerPrefsAccessibilityBackend();
        newBackend.Load();

        Assert.AreEqual(TextSizeMode.Large, newBackend.TextSize);
        Assert.AreEqual(ColorblindMode.Protanopia, newBackend.ColorblindMode);
        Assert.IsTrue(newBackend.ReduceMotionEnabled);
    }

    #endregion

    #region Reduce Motion Transition Override Tests

    [Test]
    public void GetEffectiveTransitionType_NullInstance_ReturnsUnmodified()
    {
        // Without a bootstrapped AccessibilitySystem singleton, should return as-is
        // (This test documents the null-safety behavior)
        // Note: In a real test environment, _instance would be null
        // We test the static method's null guard
        var result = AccessibilitySystem.GetEffectiveTransitionType(TransitionType.FADE_GREY);
        Assert.AreEqual(TransitionType.FADE_GREY, result);
    }

    [Test]
    public void GetEffectiveTransitionType_ReduceMotionOff_ReturnsUnmodified(
        [Values(TransitionType.FADE_GREY, TransitionType.FADE_BLACK, TransitionType.CROSSFADE)]
        TransitionType input)
    {
        // When reduce motion is disabled, all types pass through unchanged
        // This tests that CROSSFADE is not changed when reduce motion is off
        var result = AccessibilitySystem.GetEffectiveTransitionType(input);
        Assert.AreEqual(input, result);
    }

    // Note: Testing with reduce motion ENABLED requires a bootstrapped AccessibilitySystem
    // with a mock or real backend, which is beyond the scope of pure unit tests.
    // Integration tests in tests/integration/accessibility/ would cover the enabled case.

    #endregion
}
