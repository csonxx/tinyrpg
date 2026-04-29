using UnityEngine;

namespace Core.Accessibility
{
    /// <summary>
    /// Colorblind simulation modes using color matrix transformations.
    /// Matrices are applied as a linear color transformation in shader space.
    /// Based on color blindness simulation research (Machado et al., 2009).
    /// </summary>
    public enum ColorblindMode
    {
        None = 0,
        Deuteranopia = 1,
        Protanopia = 2
    }

    /// <summary>
    /// Provides color matrices for colorblind simulation.
    /// These 3x3 matrices transform sRGB colors to simulate how they appear
    /// to users with different types of color vision deficiency.
    /// </summary>
    public static class ColorblindMatrices
    {
        /// <summary>
        /// Returns the 3x3 color transformation matrix for the given colorblind mode.
        /// The matrix is in row-major order suitable for shader material properties.
        /// </summary>
        /// <param name="mode">The colorblind mode to get the matrix for.</param>
        /// <returns>A 3x3 matrix as an array of 9 float values (row-major).</returns>
        public static float[] GetMatrix(ColorblindMode mode)
        {
            return mode switch
            {
                // Deuteranopia: red-green confusion, greens shifted toward blue-green
                // Based on Machado et al. simulation of deuteranopia
                ColorblindMode.Deuteranopia => new float[]
                {
                    0.625f, 0.375f, 0.0f,   // Red row
                    0.7f,   0.3f,   0.0f,   // Green row
                    0.0f,   0.3f,   0.7f    // Blue row
                },

                // Protanopia: red-green confusion, reds shifted toward orange
                // Based on Machado et al. simulation of protanopia
                ColorblindMode.Protanopia => new float[]
                {
                    0.567f, 0.433f, 0.0f,   // Red row
                    0.558f, 0.442f, 0.0f,   // Green row
                    0.0f,   0.242f, 0.758f  // Blue row
                },

                // None: identity matrix (no change)
                _ => new float[]
                {
                    1.0f, 0.0f, 0.0f,  // Red row
                    0.0f, 1.0f, 0.0f,  // Green row
                    0.0f, 0.0f, 1.0f   // Blue row
                }
            };
        }
    }
}
