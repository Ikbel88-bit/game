using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RoboClash.UI
{
    public sealed class RoboClashMinimap : MaskableGraphic
    {
        private struct MapShape
        {
            public Bounds Bounds;
            public Color Color;
        }

        private static readonly Color WallColor = new Color(0.12f, 0.68f, 0.83f, 0.9f);
        private static readonly Color ObstacleColor = new Color(0.43f, 0.62f, 0.72f, 0.75f);
        private static readonly Color HazardColor = new Color(1f, 0.25f, 0.1f, 0.9f);

        private readonly List<MapShape> shapes = new List<MapShape>();
        private RectTransform marker;
        private RobotArenaController player;
        private Vector2 arenaCenter;
        private Vector2 arenaSize;
        private bool hasMapData;

        public bool HasMapData => hasMapData;

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
        }

        public void Configure(Transform arenaRoot, RobotArenaController playerController,
            RectTransform playerMarker)
        {
            player = playerController;
            marker = playerMarker;
            shapes.Clear();
            hasMapData = false;

            if (arenaRoot == null)
            {
                Debug.LogWarning("RoboClash minimap could not build: the arena root is missing.", this);
                SetVerticesDirty();
                UpdatePlayerMarker();
                return;
            }

            var colliders = arenaRoot.GetComponentsInChildren<Collider>(true);
            var floorBounds = new Bounds();
            var hasFloor = false;
            foreach (var arenaCollider in colliders)
            {
                if (arenaCollider == null || !arenaCollider.enabled ||
                    arenaCollider.GetComponentInParent<ArenaDamageable>() != null ||
                    arenaCollider.isTrigger)
                    continue;

                var floor = arenaCollider.GetComponentInParent<ArenaFloor>();
                if (floor == null)
                    continue;

                if (!hasFloor)
                {
                    floorBounds = arenaCollider.bounds;
                    hasFloor = true;
                }
                else
                    floorBounds.Encapsulate(arenaCollider.bounds);
            }

            if (!hasFloor)
            {
                foreach (var arenaCollider in colliders)
                {
                    if (arenaCollider == null || !arenaCollider.enabled || arenaCollider.isTrigger ||
                        arenaCollider.GetComponentInParent<ArenaDamageable>() != null)
                        continue;

                    if (!hasFloor)
                    {
                        floorBounds = arenaCollider.bounds;
                        hasFloor = true;
                    }
                    else
                        floorBounds.Encapsulate(arenaCollider.bounds);
                }
            }

            if (!hasFloor || floorBounds.size.x <= 0f || floorBounds.size.z <= 0f)
            {
                Debug.LogWarning(
                    $"RoboClash minimap could not find valid arena bounds under '{arenaRoot.name}'.",
                    arenaRoot);
                SetVerticesDirty();
                UpdatePlayerMarker();
                return;
            }

            arenaCenter = new Vector2(floorBounds.center.x, floorBounds.center.z);
            arenaSize = new Vector2(floorBounds.size.x, floorBounds.size.z);

            foreach (var arenaCollider in colliders)
            {
                if (arenaCollider == null || !arenaCollider.enabled || arenaCollider.isTrigger ||
                    arenaCollider.GetComponentInParent<ArenaDamageable>() != null ||
                    arenaCollider.GetComponentInParent<ArenaFloor>() != null)
                    continue;

                var bounds = arenaCollider.bounds;
                if (bounds.size.x <= 0f || bounds.size.z <= 0f)
                    continue;

                var isHazard = arenaCollider.GetComponentInParent<ArenaHazard>() != null;
                var isWall = arenaCollider.GetComponentInParent<ArenaWall>() != null;
                shapes.Add(new MapShape
                {
                    Bounds = bounds,
                    Color = isHazard ? HazardColor : isWall ? WallColor : ObstacleColor
                });
            }

            hasMapData = true;
            SetVerticesDirty();
            UpdatePlayerMarker();
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            if (!hasMapData)
                return;

            var rect = rectTransform.rect;
            var scale = Mathf.Min(rect.width / arenaSize.x, rect.height / arenaSize.y);
            if (scale <= 0f)
                return;

            for (var i = 0; i < shapes.Count; i++)
            {
                var shape = shapes[i];
                var minimum = WorldToMapLocal(new Vector2(shape.Bounds.min.x, shape.Bounds.min.z), scale);
                var maximum = WorldToMapLocal(new Vector2(shape.Bounds.max.x, shape.Bounds.max.z), scale);
                minimum.x = Mathf.Clamp(minimum.x, rect.xMin, rect.xMax);
                minimum.y = Mathf.Clamp(minimum.y, rect.yMin, rect.yMax);
                maximum.x = Mathf.Clamp(maximum.x, rect.xMin, rect.xMax);
                maximum.y = Mathf.Clamp(maximum.y, rect.yMin, rect.yMax);
                if (maximum.x - minimum.x < 1f || maximum.y - minimum.y < 1f)
                    continue;

                var firstVertex = vertexHelper.currentVertCount;
                AddVertex(vertexHelper, new Vector2(minimum.x, minimum.y), shape.Color);
                AddVertex(vertexHelper, new Vector2(minimum.x, maximum.y), shape.Color);
                AddVertex(vertexHelper, new Vector2(maximum.x, maximum.y), shape.Color);
                AddVertex(vertexHelper, new Vector2(maximum.x, minimum.y), shape.Color);
                vertexHelper.AddTriangle(firstVertex, firstVertex + 1, firstVertex + 2);
                vertexHelper.AddTriangle(firstVertex, firstVertex + 2, firstVertex + 3);
            }
        }

        private void LateUpdate()
        {
            UpdatePlayerMarker();
        }

        private void UpdatePlayerMarker()
        {
            if (marker == null)
                return;

            if (!hasMapData || player == null || !player.gameObject.activeInHierarchy || player.IsDead)
            {
                marker.gameObject.SetActive(false);
                return;
            }

            marker.gameObject.SetActive(true);
            var rect = rectTransform.rect;
            var scale = Mathf.Min(rect.width / arenaSize.x, rect.height / arenaSize.y);
            var worldPosition = player.transform.position;
            var localPosition = WorldToMapLocal(new Vector2(worldPosition.x, worldPosition.z), scale);
            marker.anchoredPosition = new Vector2(
                Mathf.Clamp(localPosition.x, rect.xMin, rect.xMax),
                Mathf.Clamp(localPosition.y, rect.yMin, rect.yMax));
            marker.localRotation = Quaternion.Euler(0f, 0f, -player.transform.eulerAngles.y);
        }

        private Vector2 WorldToMapLocal(Vector2 worldPosition, float scale)
        {
            return (worldPosition - arenaCenter) * scale;
        }

        private static void AddVertex(VertexHelper vertexHelper, Vector2 position, Color vertexColor)
        {
            var vertex = UIVertex.simpleVert;
            vertex.position = position;
            vertex.color = vertexColor;
            vertexHelper.AddVert(vertex);
        }
    }
}
