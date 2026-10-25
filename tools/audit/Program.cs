using System;

namespace VoidHex.Audit
{
    /// <summary>
    /// Ejecuta la lógica pura de los tres prototipos fuera de Unity y compara los resultados con las
    /// aserciones escritas en los tests. Detecta tests que fallarían antes de abrir el editor.
    ///
    /// No reemplaza al Test Runner: es un verificador previo. Cada prototipo vive en su propio archivo
    /// porque los tres definen su propio <c>HexCoordinates</c> y no pueden compartir un único using.
    /// </summary>
    internal static class Program
    {
        internal static int Failures { get; private set; }

        private static int Main()
        {
            Console.WriteLine("=== P1 · GridState (ISP + Observer) ===");
            P1Audit.Run();

            Console.WriteLine();
            Console.WriteLine("=== P2 · CommandPipeline (ICommand + async) ===");
            P2Audit.RunAsync().GetAwaiter().GetResult();

            Console.WriteLine();
            Console.WriteLine("=== P3 · Loadout (DI + data-driven) ===");
            P3Audit.Run();

            Console.WriteLine();
            Console.WriteLine(Failures == 0
                ? "RESULTADO: todas las aserciones coinciden con el código."
                : $"RESULTADO: {Failures} aserción(es) NO coinciden.");
            return Failures == 0 ? 0 : 1;
        }

        internal static void Check(string label, object actual, object expected)
        {
            var ok = Equals(actual, expected);
            if (!ok) Failures++;
            Console.WriteLine($"  [{(ok ? "OK  " : "FAIL")}] {label}: esperado={expected} obtenido={actual}");
        }

        internal static void Note(string text) => Console.WriteLine($"         · {text}");
    }
}
