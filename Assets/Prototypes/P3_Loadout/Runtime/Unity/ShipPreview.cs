using UnityEngine;
using VoidHex.P3.Domain;
using VoidHex.UiKit;

namespace VoidHex.P3.Unity
{
    /// <summary>
    /// Vista 3D de la nave con modelo real (Kenney Space Kit).
    ///
    /// Representa el loadout sin decidir nada:
    ///   Movimiento → escala de la nave.
    ///   PA         → propulsores encendidos (emisión).
    ///   Escudos    → anillo de energía alrededor de la nave.
    ///
    /// No calcula: recibe <see cref="ShipStats"/> ya consolidado por el dominio.
    /// Espera el evento <c>Ready</c> del consumidor en lugar de asumir el orden de inyección.
    /// </summary>
    public sealed class ShipPreview : MonoBehaviour
    {
        [SerializeField] private LoadoutDemo demo;
        [SerializeField] private float baseScale = 0.85f;

        private Transform _ship;
        private Transform _shieldRing;
        private Transform _pad;
        private Renderer[] _shipRenderers;
        private Material _energyMaterial;
        private IEquipmentService _service;

        private static readonly Color Accent = new Color(0.25f, 0.85f, 0.95f);
        private static readonly Color PadColor = new Color(0.15f, 0.20f, 0.29f);

        private void Awake()
        {
            if (demo == null) demo = GetComponent<LoadoutDemo>();
            if (demo == null) return;

            demo.Ready += OnServiceReady;
            if (demo.IsReady) OnServiceReady(demo.Service);
        }

        private void OnDestroy()
        {
            if (demo == null) return;

            demo.Ready -= OnServiceReady;
            if (_service != null) _service.StatsChanged -= Apply;
        }

        private void OnServiceReady(IEquipmentService service)
        {
            _service = service;
            Build();
            service.StatsChanged += Apply;
            Apply(service.CurrentStats);
        }

        // ------------------------------------------------------------------ construcción

        private void Build()
        {
            _energyMaterial = ModelLibrary.CreateLitMaterial(Accent, emission: 1.2f);

            // Plataforma de hangar (modelo del kit) como base física de la nave.
            _pad = new GameObject("Pad").transform;
            _pad.SetParent(transform, false);
            _pad.localPosition = new Vector3(0f, -0.02f, 0f);
            var padModel = ModelLibrary.Spawn(ModelLibrary.PlatformCenter, _pad, 3.4f);
            if (padModel != null) ModelLibrary.TintRenderers(padModel, PadColor);

            _ship = new GameObject("Ship").transform;
            _ship.SetParent(transform, false);
            _ship.localPosition = new Vector3(0f, 0.22f, 0f);
            _ship.localRotation = Quaternion.Euler(0f, 26f, 0f);

            var model = ModelLibrary.Spawn(ModelLibrary.ShipSpeederB, _ship, 1.5f);
            if (model == null)
            {
                var fallback = GameObject.CreatePrimitive(PrimitiveType.Cube);
                fallback.transform.SetParent(_ship, false);
                fallback.transform.localScale = new Vector3(0.7f, 0.25f, 1.3f);
            }

            _shipRenderers = _ship.GetComponentsInChildren<Renderer>();
            _shieldRing = BuildShieldRing();
        }

        /// <summary>Anillo de 18 segmentos: se lee como aro de energía sin transparencias.</summary>
        private Transform BuildShieldRing()
        {
            var root = new GameObject("ShieldRing").transform;
            root.SetParent(transform, false);
            root.localPosition = new Vector3(0f, 0.16f, 0f);

            const int segments = 18;
            const float radius = 1.15f;

            for (var i = 0; i < segments; i++)
            {
                var angle = i * Mathf.PI * 2f / segments;
                var segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
                segment.name = "seg" + i;
                segment.transform.SetParent(root, false);
                segment.transform.localPosition =
                    new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                segment.transform.localScale = new Vector3(0.22f, 0.025f, 0.06f);
                segment.transform.localRotation = Quaternion.Euler(0f, -angle * Mathf.Rad2Deg, 0f);

                var collider = segment.GetComponent<Collider>();
                if (collider != null) Destroy(collider);

                segment.GetComponent<Renderer>().sharedMaterial = _energyMaterial;
            }

            return root;
        }

        // ------------------------------------------------------------------ representación

        private void Apply(ShipStats stats)
        {
            if (_ship == null) return;

            // Movimiento → tamaño. Base 3 de movimiento = escala base.
            var growth = 1f + (stats.Movement - 3) * 0.07f;
            _ship.localScale = Vector3.one * (baseScale * growth);

            // PA → intensidad de emisión del casco (nave "energizada").
            var glow = Mathf.Clamp01(stats.ActionPoints / 10f);
            foreach (var renderer in _shipRenderers)
            {
                if (renderer == null) continue;

                var material = renderer.material;
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", Accent * (0.04f + glow * 0.16f));
            }

            // Escudos → anillo visible y proporcional.
            if (_shieldRing != null)
            {
                var hasShield = stats.Shields > 0;
                _shieldRing.gameObject.SetActive(hasShield);

                if (hasShield)
                {
                    var radius = 0.94f + stats.Shields * 0.02f;
                    _shieldRing.localScale = new Vector3(radius, 1f, radius);
                    _energyMaterial.SetColor("_EmissionColor", Accent * (0.5f + stats.Shields * 0.1f));
                }
            }
        }
    }
}
