using System;

namespace VoidHex.GameLoop.Logic
{
    /// <summary>
    /// Proyección entre coordenadas axiales y mundo, sin dependencia de UnityEngine.
    ///
    /// Vive en la capa lógica para poder testearla: es la matemática que decide qué casilla se clickeó,
    /// y un error ahí se siente como "el juego no responde", no como un bug de matemática.
    ///
    /// Debe usar la MISMA separación que el renderizador (P1: GridCellRenderer.Spacing),
    /// de lo contrario el clic y el dibujo se desalinean.
    /// </summary>
    public static class HexProjection
    {
        public const float Spacing = 0.7f;

        private const float Sqrt3 = 1.7320508f;

        public readonly struct Point
        {
            public readonly float X;
            public readonly float Z;

            public Point(float x, float z)
            {
                X = x;
                Z = z;
            }

            public override string ToString() => "(" + X.ToString("F2") + ", " + Z.ToString("F2") + ")";
        }

        /// <summary>Centro de la casilla en el plano XZ.</summary>
        public static Point ToWorld(int q, int r)
            => new Point(1.5f * q * Spacing, Sqrt3 * (r + q * 0.5f) * Spacing);

        /// <summary>
        /// Casilla que contiene un punto del plano. Devuelve false si cae fuera del mapa,
        /// así el clic en el vacío no produce una coordenada inválida.
        /// </summary>
        public static bool TryToCell(float x, float z, int width, int height, out int q, out int r)
        {
            if (width <= 0 || height <= 0)
            {
                q = 0;
                r = 0;
                return false;
            }

            var qf = x / (1.5f * Spacing);
            var rf = z / (Sqrt3 * Spacing) - qf * 0.5f;

            Round(qf, rf, out q, out r);

            return q >= 0 && q < width && r >= 0 && r < height;
        }

        /// <summary>Redondeo cúbico: redondea los tres ejes y corrige el que más se desvió.</summary>
        private static void Round(float qf, float rf, out int q, out int r)
        {
            var sf = -qf - rf;

            var rq = (int)Math.Round(qf);
            var rr = (int)Math.Round(rf);
            var rs = (int)Math.Round(sf);

            var dq = Math.Abs(rq - qf);
            var dr = Math.Abs(rr - rf);
            var ds = Math.Abs(rs - sf);

            if (dq > dr && dq > ds) rq = -rr - rs;
            else if (dr > ds) rr = -rq - rs;

            q = rq;
            r = rr;
        }
    }
}
