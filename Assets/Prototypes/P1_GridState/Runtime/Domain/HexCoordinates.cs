using System;
using System.Collections.Generic;

namespace VoidHex.P1.Domain
{
    /// <summary>
    /// Coordenada hexagonal en el espacio cúbico (q, r, s) con la restricción q + r + s = 0.
    /// Tipo de valor inmutable: no asigna memoria en el heap.
    /// </summary>
    public readonly struct HexCoordinates : IEquatable<HexCoordinates>
    {
        public int Q { get; }
        public int R { get; }
        public int S { get; }

        public HexCoordinates(int q, int r)
        {
            Q = q;
            R = r;
            S = -q - r;
        }

        public static HexCoordinates Origin => new HexCoordinates(0, 0);

        /// <summary>Las seis direcciones en orden fijo. El orden es parte del contrato de determinismo.</summary>
        public static IReadOnlyList<HexCoordinates> Directions { get; } = Array.AsReadOnly(new[]
        {
            new HexCoordinates(0, -1),  // NE
            new HexCoordinates(1, -1),  // E
            new HexCoordinates(1, 0),   // SE
            new HexCoordinates(0, 1),   // SW
            new HexCoordinates(-1, 1),  // W
            new HexCoordinates(-1, 0),  // NW
        });

        public static int Distance(HexCoordinates a, HexCoordinates b)
        {
            return (Math.Abs(a.Q - b.Q) + Math.Abs(a.R - b.R) + Math.Abs(a.S - b.S)) / 2;
        }

        public static HexCoordinates operator +(HexCoordinates a, HexCoordinates b)
        {
            return new HexCoordinates(a.Q + b.Q, a.R + b.R);
        }

        public static bool operator ==(HexCoordinates a, HexCoordinates b) => a.Equals(b);

        public static bool operator !=(HexCoordinates a, HexCoordinates b) => !a.Equals(b);

        public bool Equals(HexCoordinates other) => Q == other.Q && R == other.R;

        public override bool Equals(object obj) => obj is HexCoordinates other && Equals(other);

        public override int GetHashCode() => (Q * 397) ^ R;

        public override string ToString() => "(" + Q + "," + R + ")";
    }
}
