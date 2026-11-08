using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RoboClash.UI
{
    public sealed class RoboClashMinimapV2 : MaskableGraphic
    {
        private struct MapShape
        {
            public Vector2 Minimum;
            public Vector2 Maximum;
            public Color Fill;
            public Color Outline;
        }

        private static readonly Color FloorColor = new Color(0.035f, 0.075f, 0.11f, 0.98f);
        private static readonly Color GridColor = new Color(0.3f, 0.65f, 0.85f, 0.2f);
        private static readonly Color EdgeColor = new Color(0.3f, 0.85f, 1f, 0.8f);
        private static readonly Color WallFill = new Color(0.58f, 0.72f, 0.85f, 0.96f);
        private static readonly Color WallOutline = new Color(0.2f, 0.85f, 1f, 1f);
        private static readonly Color ObstacleFill = new Color(0.3f, 0.4f, 0.54f, 0.96f);
        private static readonly Color ObstacleOutline = new Color(0.45f, 0.62f, 0.8f, 0.95f);
        private static readonly Color HazardFill = new Color(1f, 0.3f, 0.15f, 0.85f);
        private static readonly Color HazardOutline = new Color(1f, 0.7f, 0.2f, 1f);
        private static readonly Color EnemyColor = new Color(1f, 0.22f, 0.25f, 1f);
        private static readonly Color PlayerColor = new Color(0.85f, 1f, 1f, 1f);

        [Min(1f)] public float viewRadius = 30f;
        public bool rotateWithPlayer = true;
        public bool requireRobotComponent = true;

        public bool HasMapData => hasFloor;

        private readonly List<MapShape> shapes = new List<MapShape>();
        private readonly List<Collider> colliders = new List<Collider>();
        private readonly List<Collider> floors = new List<Collider>();
        private readonly List<ArenaDamageable> damageables = new List<ArenaDamageable>();
        private readonly List<Transform> visibleEnemies = new List<Transform>();
        private Transform player;
        private Transform arenaRoot;
        private Camera playerCamera;
        private RoboClashRobotDefinition playerDefinition;
        private Vector2 floorMinimum;
        private Vector2 floorMaximum;
        private Vector2 mapWorldCenter;
        private bool hasFloor;
        private float enemyRefreshRemaining;
        private Vector2 mapCenter;
        private Vector2 playerPosition;
        private float mapRadius;
        private float mapScale;
        private float cosine;
        private float sine;

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
        }

        public void Configure(Transform playerTransform, Transform root, Camera viewCamera = null)
        {
            player = playerTransform;
            playerDefinition = player != null
                ? player.GetComponent<RoboClashRobotDefinition>()
                : null;
            arenaRoot = root;
            playerCamera = viewCamera != null ? viewCamera : Camera.main;
            Rebuild();
            RefreshEnemies();
        }

        public void Rebuild()
        {
            shapes.Clear();
            floors.Clear();
            hasFloor = false;

            if (arenaRoot == null)
            {
                SetVerticesDirty();
                return;
            }

            colliders.Clear();
            arenaRoot.GetComponentsInChildren(true, colliders);
            Collider biggest = null;
            var biggestArea = 0f;
            foreach (var arenaCollider in colliders)
            {
                if (!IsArenaCollider(arenaCollider))
                    continue;

                if (arenaCollider.GetComponentInParent<ArenaFloor>() != null)
                    floors.Add(arenaCollider);

                var bounds = arenaCollider.bounds;
                var area = bounds.size.x * bounds.size.z;
                if (area > biggestArea)
                {
                    biggestArea = area;
                    biggest = arenaCollider;
                }
            }

            if (floors.Count == 0 && biggest != null)
                floors.Add(biggest);

            foreach (var floor in floors)
            {
                if (!hasFloor)
                {
                    floorMinimum = new Vector2(floor.bounds.min.x, floor.bounds.min.z);
                    floorMaximum = new Vector2(floor.bounds.max.x, floor.bounds.max.z);
                    hasFloor = true;
                }
                else
                {
                    floorMinimum = Vector2.Min(floorMinimum,
                        new Vector2(floor.bounds.min.x, floor.bounds.min.z));
                    floorMaximum = Vector2.Max(floorMaximum,
                        new Vector2(floor.bounds.max.x, floor.bounds.max.z));
                }
            }

            if (!hasFloor || floorMaximum.x <= floorMinimum.x ||
                floorMaximum.y <= floorMinimum.y)
            {
                SetVerticesDirty();
                return;
            }

            var floorArea = (floorMaximum.x - floorMinimum.x) *
                            (floorMaximum.y - floorMinimum.y);
            foreach (var arenaCollider in colliders)
            {
                if (!IsArenaCollider(arenaCollider) || floors.Contains(arenaCollider))
                    continue;

                var bounds = arenaCollider.bounds;
                if (bounds.size.y < 0.25f || bounds.size.x * bounds.size.z > floorArea * 0.5f ||
                    bounds.size.x <= 0f || bounds.size.z <= 0f)
                    continue;

                var hazard = arenaCollider.GetComponentInParent<ArenaHazard>() != null;
                var wall = arenaCollider.GetComponentInParent<ArenaWall>() != null;
                shapes.Add(new MapShape
                {
                    Minimum = new Vector2(bounds.min.x, bounds.min.z),
                    Maximum = new Vector2(bounds.max.x, bounds.max.z),
                    Fill = hazard ? HazardFill : wall ? WallFill : ObstacleFill,
                    Outline = hazard ? HazardOutline : wall ? WallOutline : ObstacleOutline
                });
            }

            SetVerticesDirty();
        }

        private bool IsArenaCollider(Collider arenaCollider)
        {
            return arenaCollider != null && arenaCollider.enabled && !arenaCollider.isTrigger &&
                   arenaCollider.gameObject.activeInHierarchy &&
                   arenaCollider.GetComponentInParent<ArenaDamageable>() == null;
        }

        private void RefreshEnemies()
        {
            visibleEnemies.Clear();
            if (arenaRoot == null)
                return;

            if (playerCamera == null)
                playerCamera = Camera.main;

            damageables.Clear();
            arenaRoot.GetComponentsInChildren(true, damageables);
            foreach (var damageable in damageables)
            {
                if (damageable == null || damageable.IsDead ||
                    !damageable.gameObject.activeInHierarchy)
                    continue;

                var movement = damageable.GetComponent<RoboClashRobotMovement>();
                var definition = damageable.GetComponent<RoboClashRobotDefinition>();
                var robotAbilities = damageable.GetComponent<RoboClashRobotAbilities>();
                if (requireRobotComponent && robotAbilities == null)
                    continue;
                if (movement == null || movement.PlayerControlled || definition == null ||
                    (playerDefinition != null && definition.teamId == playerDefinition.teamId))
                    continue;

                if (IsVisibleToPlayer(damageable.transform))
                    visibleEnemies.Add(damageable.transform);
            }
        }

        private bool IsVisibleToPlayer(Transform enemy)
        {
            if (playerCamera == null || !playerCamera.isActiveAndEnabled)
                return false;

            var renderer = enemy.GetComponentInChildren<Renderer>();
            var target = renderer != null
                ? renderer.bounds.center
                : enemy.position + Vector3.up;
            var viewportPoint = playerCamera.WorldToViewportPoint(target);
            if (viewportPoint.z <= 0f || viewportPoint.x < 0f || viewportPoint.x > 1f ||
                viewportPoint.y < 0f || viewportPoint.y > 1f ||
                viewportPoint.z > playerCamera.farClipPlane)
                return false;

            var rayDirection = target - playerCamera.transform.position;
            var distance = rayDirection.magnitude;
            if (distance <= 0f)
                return true;

            var hits = Physics.RaycastAll(playerCamera.transform.position,
                rayDirection / distance, distance, Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);
            var nearestDistance = float.PositiveInfinity;
            Transform nearestTransform = null;
            foreach (var hit in hits)
            {
                if (player != null &&
                    (hit.transform == player || hit.transform.IsChildOf(player)))
                    continue;

                if (hit.distance < nearestDistance)
                {
                    nearestDistance = hit.distance;
                    nearestTransform = hit.transform;
                }
            }

            return nearestTransform == null || nearestTransform == enemy ||
                   nearestTransform.IsChildOf(enemy);
        }

        private void Update()
        {
            if (player == null)
                return;

            enemyRefreshRemaining -= Time.unscaledDeltaTime;
            if (enemyRefreshRemaining <= 0f)
            {
                enemyRefreshRemaining = 0.25f;
                RefreshEnemies();
            }

            SetVerticesDirty();
        }

        private Vector2 ToMap(Vector2 worldPosition)
        {
            var offset = worldPosition - mapWorldCenter;
            if (!rotateWithPlayer)
                return mapCenter + offset * mapScale;

            return mapCenter + new Vector2(
                offset.x * cosine + offset.y * sine,
                -offset.x * sine + offset.y * cosine) * mapScale;
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            if (player == null)
                return;

            var rect = GetPixelAdjustedRect();
            mapCenter = rect.center;
            mapRadius = Mathf.Min(rect.width, rect.height) * 0.5f;
            var worldPosition = player.position;
            playerPosition = new Vector2(worldPosition.x, worldPosition.z);
            mapWorldCenter = hasFloor
                ? (floorMinimum + floorMaximum) * 0.5f
                : playerPosition;
            var mapHalfDiagonal = hasFloor
                ? (floorMaximum - floorMinimum).magnitude * 0.5f
                : viewRadius;
            mapScale = (mapRadius - 8f) / Mathf.Max(1f, mapHalfDiagonal);
            var yaw = player.eulerAngles.y * Mathf.Deg2Rad;
            cosine = Mathf.Cos(yaw);
            sine = Mathf.Sin(yaw);

            if (hasFloor)
            {
                WorldRect(vertexHelper, floorMinimum, floorMaximum, 0f, FloorColor);
                DrawGrid(vertexHelper);
                var lowerLeft = floorMinimum;
                var upperLeft = new Vector2(floorMinimum.x, floorMaximum.y);
                var upperRight = floorMaximum;
                var lowerRight = new Vector2(floorMaximum.x, floorMinimum.y);
                WorldLine(vertexHelper, lowerLeft, upperLeft, 2f, EdgeColor);
                WorldLine(vertexHelper, upperLeft, upperRight, 2f, EdgeColor);
                WorldLine(vertexHelper, upperRight, lowerRight, 2f, EdgeColor);
                WorldLine(vertexHelper, lowerRight, lowerLeft, 2f, EdgeColor);
            }

            DrawShapes(vertexHelper);
            DrawVisionCone(vertexHelper, yaw);
            DrawEnemies(vertexHelper);
            DrawPlayerArrow(vertexHelper);
        }

        private void DrawGrid(VertexHelper vertexHelper)
        {
            const float step = 5f;
            var minimumX = floorMinimum.x;
            var maximumX = floorMaximum.x;
            var minimumZ = floorMinimum.y;
            var maximumZ = floorMaximum.y;
            for (var x = Mathf.Ceil(minimumX / step) * step; x <= maximumX; x += step)
                WorldLine(vertexHelper, new Vector2(x, minimumZ),
                    new Vector2(x, maximumZ), 1f, GridColor);
            for (var z = Mathf.Ceil(minimumZ / step) * step; z <= maximumZ; z += step)
                WorldLine(vertexHelper, new Vector2(minimumX, z),
                    new Vector2(maximumX, z), 1f, GridColor);
        }

        private void DrawShapes(VertexHelper vertexHelper)
        {
            for (var i = 0; i < shapes.Count; i++)
            {
                var shape = shapes[i];
                WorldRect(vertexHelper, shape.Minimum, shape.Maximum, 0.25f, shape.Outline);
                WorldRect(vertexHelper, shape.Minimum, shape.Maximum, 0f, shape.Fill);
            }
        }

        private void DrawVisionCone(VertexHelper vertexHelper, float yaw)
        {
            var forward = ToMap(playerPosition + new Vector2(Mathf.Sin(yaw), Mathf.Cos(yaw))) -
                          ToMap(playerPosition);
            if (forward.sqrMagnitude < 0.0001f)
                forward = Vector2.up;
            forward.Normalize();

            const int segments = 12;
            var halfAngle = 38f * Mathf.Deg2Rad;
            var playerMapPosition = ToMap(playerPosition);
            var centerIndex = vertexHelper.currentVertCount;
            AddVertex(vertexHelper, playerMapPosition, new Color(0.3f, 0.9f, 1f, 0.2f));
            var baseAngle = Mathf.Atan2(forward.y, forward.x);
            var coneLength = Mathf.Max(6f, mapScale * 12f);
            for (var i = 0; i <= segments; i++)
            {
                var angle = baseAngle - halfAngle + 2f * halfAngle * i / segments;
                var point = playerMapPosition + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) *
                            coneLength;
                AddVertex(vertexHelper, point, new Color(0.3f, 0.9f, 1f, 0f));
            }

            for (var i = 0; i < segments; i++)
                vertexHelper.AddTriangle(centerIndex, centerIndex + 1 + i, centerIndex + 2 + i);
        }

        private void DrawEnemies(VertexHelper vertexHelper)
        {
            var edgeRadius = mapRadius - 9f;
            foreach (var enemy in visibleEnemies)
            {
                if (enemy == null || !enemy.gameObject.activeInHierarchy)
                    continue;

                var enemyPosition = enemy.position;
                var point = ToMap(new Vector2(enemyPosition.x, enemyPosition.z));
                var offset = point - mapCenter;
                var isOutside = offset.magnitude > edgeRadius;
                if (isOutside && offset.sqrMagnitude > 0.0001f)
                    point = mapCenter + offset.normalized * edgeRadius;
                if (!isOutside)
                    Disc(vertexHelper, point, 8f,
                        new Color(EnemyColor.r, EnemyColor.g, EnemyColor.b, 0.25f));
                Disc(vertexHelper, point, isOutside ? 3.5f : 5f,
                    new Color(EnemyColor.r, EnemyColor.g, EnemyColor.b,
                        isOutside ? 0.7f : 1f));
            }
        }

        private void DrawPlayerArrow(VertexHelper vertexHelper)
        {
            var playerMapPosition = ToMap(playerPosition);
            var forward = rotateWithPlayer
                ? Vector2.up
                : new Vector2(Mathf.Sin(player.eulerAngles.y * Mathf.Deg2Rad),
                    Mathf.Cos(player.eulerAngles.y * Mathf.Deg2Rad));
            DrawArrow(vertexHelper, playerMapPosition, forward, 9.5f,
                new Color(0f, 0f, 0f, 0.9f));
            DrawArrow(vertexHelper, playerMapPosition, forward, 7.5f, PlayerColor);
        }

        private void WorldRect(VertexHelper vertexHelper, Vector2 minimum, Vector2 maximum,
            float padding, Color color)
        {
            AddQuad(vertexHelper,
                ToMap(new Vector2(minimum.x - padding, minimum.y - padding)),
                ToMap(new Vector2(minimum.x - padding, maximum.y + padding)),
                ToMap(new Vector2(maximum.x + padding, maximum.y + padding)),
                ToMap(new Vector2(maximum.x + padding, minimum.y - padding)), color);
        }

        private void WorldLine(VertexHelper vertexHelper, Vector2 start, Vector2 end,
            float width, Color color)
        {
            var first = ToMap(start);
            var second = ToMap(end);
            var direction = second - first;
            if (direction.sqrMagnitude < 0.0001f)
                return;
            direction.Normalize();
            var normal = new Vector2(-direction.y, direction.x) * (width * 0.5f);
            AddQuad(vertexHelper, first + normal, second + normal, second - normal, first - normal,
                color);
        }

        private static void DrawArrow(VertexHelper vertexHelper, Vector2 center, Vector2 forward,
            float size, Color color)
        {
            var perpendicular = new Vector2(-forward.y, forward.x);
            var tip = center + forward * size * 1.5f;
            var left = center - forward * size + perpendicular * size;
            var right = center - forward * size - perpendicular * size;
            var notch = center - forward * size * 0.35f;
            AddTriangle(vertexHelper, tip, left, notch, color);
            AddTriangle(vertexHelper, tip, notch, right, color);
        }

        private static void Disc(VertexHelper vertexHelper, Vector2 center, float radius, Color color)
        {
            const int segments = 14;
            var firstVertex = vertexHelper.currentVertCount;
            AddVertex(vertexHelper, center, color);
            for (var i = 0; i <= segments; i++)
            {
                var angle = i / (float)segments * Mathf.PI * 2f;
                AddVertex(vertexHelper, center +
                    new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius, color);
            }

            for (var i = 0; i < segments; i++)
                vertexHelper.AddTriangle(firstVertex, firstVertex + 1 + i, firstVertex + 2 + i);
        }

        private static void AddQuad(VertexHelper vertexHelper, Vector2 first, Vector2 second,
            Vector2 third, Vector2 fourth, Color color)
        {
            var index = vertexHelper.currentVertCount;
            AddVertex(vertexHelper, first, color);
            AddVertex(vertexHelper, second, color);
            AddVertex(vertexHelper, third, color);
            AddVertex(vertexHelper, fourth, color);
            vertexHelper.AddTriangle(index, index + 1, index + 2);
            vertexHelper.AddTriangle(index, index + 2, index + 3);
        }

        private static void AddTriangle(VertexHelper vertexHelper, Vector2 first,
            Vector2 second, Vector2 third, Color color)
        {
            var index = vertexHelper.currentVertCount;
            AddVertex(vertexHelper, first, color);
            AddVertex(vertexHelper, second, color);
            AddVertex(vertexHelper, third, color);
            vertexHelper.AddTriangle(index, index + 1, index + 2);
        }

        private static void AddVertex(VertexHelper vertexHelper, Vector2 position, Color color)
        {
            var vertex = UIVertex.simpleVert;
            vertex.position = position;
            vertex.color = color;
            vertexHelper.AddVert(vertex);
        }
    }
}
