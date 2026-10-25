using System;

namespace VoidHex.P2.Domain
{
    /// <summary>Coordenada hexagonal mínima que necesita el flujo de comandos.</summary>
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

        public static int Distance(HexCoordinates a, HexCoordinates b)
            => (Math.Abs(a.Q - b.Q) + Math.Abs(a.R - b.R) + Math.Abs(a.S - b.S)) / 2;

        public static bool operator ==(HexCoordinates a, HexCoordinates b) => a.Equals(b);

        public static bool operator !=(HexCoordinates a, HexCoordinates b) => !a.Equals(b);

        public bool Equals(HexCoordinates other) => Q == other.Q && R == other.R;

        public override bool Equals(object obj) => obj is HexCoordinates other && Equals(other);

        public override int GetHashCode() => (Q * 397) ^ R;

        public override string ToString() => "(" + Q + "," + R + ")";
    }
}
