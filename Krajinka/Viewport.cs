using OpenTK.Mathematics;

namespace Krajinka
{ 

    /// <summary>
    /// Pomocná třída pro výpočet viewportu v pixelech.
    /// </summary>
    internal class Viewport
    {
        public Vector2 BottomLeft = Vector2.Zero;

        public Vector2 TopRight = Vector2.One;

        public Vector2i ClientSize;

        public (Vector2i position, Vector2i size) GetPixelViewport()
        {
            var left = (int)(BottomLeft.X * ClientSize.X);
            var right = (int)(TopRight.X * ClientSize.X);
            var bottom = (int)(BottomLeft.Y * ClientSize.Y);
            var top = (int)(TopRight.Y * ClientSize.Y);
            var width = right - left;
            var height = top - bottom;
            return (new Vector2i(left, bottom), new Vector2i(width, height));
        }

        public float GetAspectRatio()
        {
            (Vector2i position, Vector2i size) = GetPixelViewport();
            if (size.Y <= 0)
            {
                return 1.0f;
            }

            return (float)size.X / size.Y;
        }
    }
}