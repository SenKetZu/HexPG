using System.Collections.Generic;
using UnityEngine;
using VoidHex.P1.Domain;

namespace VoidHex.P1.Unity
{
    /// <summary>
    /// Vista del mapa. Se suscribe a <see cref="IGridStatePublisher"/> y recolorea ÚNICAMENTE la celda
    /// que cambió, usando un MaterialPropertyBlock (sin instanciar materiales).
    ///
    /// No existe ningún Update(): sin evento, no hay trabajo. Es la propiedad que defiende el prototipo.
    /// </summary>
    public sealed class GridCellRenderer : MonoBehaviour
    {
        [SerializeField] private GridBootstrap bootstrap;
        [SerializeField] private float cellSize = 0.88f;
        [SerializeField] private float cellThickness = 0.08f;

        private readonly Dictionary<HexCoordinates, Renderer> _views = new Dictionary<HexCoordinates, Renderer>();
        private MaterialPropertyBlock _block;
        private Material _material;
        private IGridStatePublisher _publisher;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        public int CellCount => _views.Count;

        private void OnEnable()
        {
            if (bootstrap == null) bootstrap = GetComponent<GridBootstrap>();
            if (bootstrap == null) return;

            _publisher = bootstrap.Publisher;
            _publisher.Changed += HandleChanged;
            BuildCells();
        }

        private void OnDisable()
        {
            if (_publisher != null)
            {
                _publisher.Changed -= HandleChanged;
                _publisher = null;
            }
        }

        private void BuildCells()
        {
            _block ??= new MaterialPropertyBlock();
            _material ??= CreateMaterial();

            var root = new GameObject("Cells").transform;
            root.SetParent(transform, false);

            for (var q = 0; q < bootstrap.Width; q++)
            {
                for (var r = 0; r < bootstrap.Height; r++)
                {
                    var cell = new HexCoordinates(q, r);
                    var view = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    view.name = "cell" + cell;
                    view.transform.SetParent(root, false);
                    view.transform.localPosition = ToWorld(cell, cellThickness * 0.5f);
                    view.transform.localScale = new Vector3(cellSize, cellThickness, cellSize);

                    var collider = view.GetComponent<Collider>();
                    if (collider != null) Destroy(collider);

                    var renderer = view.GetComponent<Renderer>();
                    renderer.sharedMaterial = _material;
                    _views[cell] = renderer;

                    if (bootstrap.Query.TryGetCell(cell, out var data)) Paint(renderer, data);
                }
            }
        }

        private void HandleChanged(GridChange change)
        {
            bootstrap.RegisterChange();

            if (!_views.TryGetValue(change.Coordinates, out var renderer)) return;
            if (!bootstrap.Query.TryGetCell(change.Coordinates, out var cell)) return;

            // Solo se repinta la celda afectada: ni una más.
            Paint(renderer, cell);
        }

        private void Paint(Renderer renderer, HexCell cell)
        {
            _block.Clear();
            _block.SetColor(BaseColorId, ColorFor(cell));
            _block.SetColor(ColorId, ColorFor(cell));
            renderer.SetPropertyBlock(_block);
        }

        private static Color ColorFor(HexCell cell)
        {
            if (cell.IsOccupied) return new Color(0.65f, 0.25f, 0.85f);
            if (!cell.IsPassable) return new Color(0.42f, 0.22f, 0.12f);
            if (cell.MovementCost > 1) return new Color(0.16f, 0.30f, 0.55f);
            return new Color(0.14f, 0.19f, 0.25f);
        }

        /// <summary>Conversión axial a mundo: solo para dibujar, la lógica no conoce Unity.</summary>
        public static Vector3 ToWorld(HexCoordinates cell, float y = 0f)
        {
            return new Vector3(1.5f * cell.Q, y, 1.7320508f * (cell.R + cell.Q * 0.5f));
        }

        private Material CreateMaterial()
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            return new Material(shader) { name = "P1_CellMaterial" };
        }
    }
}
