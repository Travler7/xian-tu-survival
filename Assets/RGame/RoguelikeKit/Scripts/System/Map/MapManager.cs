using System.Collections.Generic;
using UnityEngine;

namespace RGame.RoguelikeKit
{
    public class MapManager : MonoBehaviour
    {
        [SerializeField] private GeneratorMapChannelSO mGeneratorMapChannelSo;
        [SerializeField] private GameObject mProp;
        [SerializeField] private Material mMapMaterial;

        private float mTileSize = 1f;

        private void OnEnable()
        {
            mGeneratorMapChannelSo.OnEventRaised += GeneratorMap;
        }

        private void OnDisable()
        {
            mGeneratorMapChannelSo.OnEventRaised -= GeneratorMap;
        }

        private void GeneratorMap(MapConfigSO _config, Transform _parent)
        {
            CreateMap(_config, _parent);
            CreateDecals(_config, _parent);
            SpawnProp(_config, _parent);
            SpawnDecoration(_config, _parent);
            SpawnAmbience(_config, _parent);
        }

        private void CreateMap(MapConfigSO _config, Transform _parent)
        {
            var mapMaterial = _config.mapMaterial != null ? _config.mapMaterial : mMapMaterial;

            if (_config.backgroundSprite != null)
            {
                CreateContinuousBackground(_config, _parent, mapMaterial);
                return;
            }

            var startPos = new Vector3(-_config.Width * 0.5f, -_config.Height * 0.5f, 0);

            for (var y = 0; y < _config.Height; y++)
            {
                for (var x = 0; x < _config.Width; x++)
                {
                    var position = startPos + new Vector3(x, y, 0);
                    var tile = new GameObject($"Tile_{x}_{y}", typeof(SpriteRenderer));
                    tile.transform.position = position;
                    tile.transform.parent = _parent;

                    var spriteRenderer = tile.GetComponent<SpriteRenderer>();
                    spriteRenderer.material = mapMaterial;
                    var sprite = GetRandomTileSprite(_config);
                    spriteRenderer.sprite = sprite;
                    spriteRenderer.sortingLayerName = "Map";
                    spriteRenderer.sortingOrder = short.MinValue;
                    if (sprite != null)
                    {
                        float pixelsPerUnit = sprite.pixelsPerUnit;
                        float spriteWidth = sprite.rect.width / pixelsPerUnit;
                        float spriteHeight = sprite.rect.height / pixelsPerUnit;

                        float scaleX = mTileSize / spriteWidth;
                        float scaleY = mTileSize / spriteHeight;

                        tile.transform.localScale = new Vector3(scaleX, scaleY, 1f);

                        position = startPos + new Vector3(x * mTileSize, y * mTileSize, 0);
                        tile.transform.position = position;
                    }
                }
            }
        }

        private void CreateContinuousBackground(MapConfigSO _config, Transform _parent, Material mapMaterial)
        {
            var background = new GameObject("MapBackground", typeof(SpriteRenderer));
            background.transform.SetParent(_parent, false);

            var spriteRenderer = background.GetComponent<SpriteRenderer>();
            spriteRenderer.sprite = _config.backgroundSprite;
            spriteRenderer.material = mapMaterial;
            spriteRenderer.sortingLayerName = "Map";
            spriteRenderer.sortingOrder = short.MinValue;

            var sprite = _config.backgroundSprite;
            var spriteWidth = sprite.rect.width / sprite.pixelsPerUnit;
            var spriteHeight = sprite.rect.height / sprite.pixelsPerUnit;
            var mapWidth = _config.Width * mTileSize;
            var mapHeight = _config.Height * mTileSize;
            background.transform.localScale = new Vector3(mapWidth / spriteWidth, mapHeight / spriteHeight, 1f);
            // Center the rendered bounds on the gameplay map origin regardless of
            // whether the imported sprite uses a centered or corner pivot.
            var scaledSpriteCenter = new Vector3(
                sprite.bounds.center.x * background.transform.localScale.x,
                sprite.bounds.center.y * background.transform.localScale.y,
                0f);
            background.transform.position = new Vector3(
                -scaledSpriteCenter.x,
                -scaledSpriteCenter.y,
                0.5f);
        }

        private Sprite GetRandomTileSprite(MapConfigSO _config)
        {
            var totalWeight = 0;
            foreach (var tileData in _config.tiles) totalWeight += tileData.weight;

            var randomWeight = Random.Range(0, totalWeight);
            var currentWeight = 0;

            foreach (var tileData in _config.tiles)
            {
                currentWeight += tileData.weight;
                if (randomWeight < currentWeight) return tileData.sprite;
            }

            return null;
        }

        /// <summary>
        /// Scatters soft-edged, non-gameplay decals above the continuous background.
        /// The work is done on a jittered grid so the result reads as organic terrain
        /// variation instead of a repeating tile pattern.
        /// </summary>
        private void CreateDecals(MapConfigSO _config, Transform _parent)
        {
            if (_config.decals == null || _config.decals.Length == 0) return;

            var totalWeight = 0;
            foreach (var decal in _config.decals)
            {
                if (decal != null && decal.sprite != null) totalWeight += Mathf.Max(1, decal.weight);
            }

            if (totalWeight <= 0) return;

            var mapMaterial = _config.mapMaterial != null ? _config.mapMaterial : mMapMaterial;
            var holder = new GameObject("Decals");
            holder.transform.SetParent(_parent, false);

            var mapWidth = _config.Width * mTileSize;
            var mapHeight = _config.Height * mTileSize;
            var spacing = Mathf.Max(0.5f, _config.DecalSpacing);
            var cols = Mathf.Max(1, Mathf.CeilToInt(mapWidth / spacing));
            var rows = Mathf.Max(1, Mathf.CeilToInt(mapHeight / spacing));
            var cellWidth = mapWidth / cols;
            var cellHeight = mapHeight / rows;
            var origin = new Vector3(-mapWidth * 0.5f, -mapHeight * 0.5f, 0f);
            var clearSqr = _config.DecalClearRadius * _config.DecalClearRadius;

            for (var y = 0; y < rows; y++)
            {
                for (var x = 0; x < cols; x++)
                {
                    if (Random.value > _config.DecalDensity) continue;

                    var decal = PickDecal(_config.decals, totalWeight);
                    if (decal == null || decal.sprite == null) continue;

                    var basePosition = origin + new Vector3(
                        (x + Random.value) * cellWidth,
                        (y + Random.value) * cellHeight,
                        0f);

                    // Keep the area around the player spawn readable.
                    if (basePosition.sqrMagnitude < clearSqr) continue;

                    var clusterMin = Mathf.Min(decal.clusterMin, decal.clusterMax);
                    var clusterMax = Mathf.Max(decal.clusterMin, decal.clusterMax);
                    var cluster = Random.Range(clusterMin, clusterMax + 1);

                    for (var i = 0; i < cluster; i++)
                    {
                        var offset = i == 0 ? Vector2.zero : Random.insideUnitCircle * decal.clusterRadius;
                        var position = basePosition + new Vector3(offset.x, offset.y, 0f);
                        if (position.sqrMagnitude < clearSqr) continue;

                        var decalObject = new GameObject("Decal", typeof(SpriteRenderer));
                        decalObject.transform.SetParent(holder.transform, false);
                        decalObject.transform.position = position;
                        // Vegetation, stones and water carry baked top lighting, so the
                        // authored "up" direction is kept (maxRotation stays small for
                        // them). Only flat ground litter is allowed to spin freely.
                        decalObject.transform.rotation = Quaternion.Euler(
                            0f,
                            0f,
                            Random.Range(-decal.maxRotation, decal.maxRotation));

                        var scale = Random.Range(
                            Mathf.Min(decal.scaleRange.x, decal.scaleRange.y),
                            Mathf.Max(decal.scaleRange.x, decal.scaleRange.y));
                        decalObject.transform.localScale = new Vector3(scale, scale, 1f);

                        var renderer = decalObject.GetComponent<SpriteRenderer>();
                        renderer.sprite = decal.sprite;
                        renderer.material = mapMaterial;
                        renderer.sortingLayerName = "Map";
                        renderer.sortingOrder = short.MinValue + 1;
                        renderer.flipX = Random.value < 0.5f;

                        var color = Color.white;
                        color.a = Random.Range(
                            Mathf.Min(decal.minAlpha, decal.maxAlpha),
                            Mathf.Max(decal.minAlpha, decal.maxAlpha));
                        renderer.color = color;
                    }
                }
            }
        }

        /// <summary>
        /// Places props with a jittered grid, random rotation/scale and occasional
        /// clusters so the old rigid rows/columns pattern disappears.
        /// </summary>
        private void SpawnProp(MapConfigSO _config, Transform _parent)
        {
            ScatterPrefabs(_config, BuildPropEntries(_config), _config.PropCount, _config.PropJitter,
                _config.PropClearRadius, _parent, "Props");
        }

        /// <summary>
        /// Standing scenery with no gameplay attached (no loot, no colliders).
        /// </summary>
        private void SpawnDecoration(MapConfigSO _config, Transform _parent)
        {
            if (_config.decorations == null || _config.decorations.Length == 0) return;

            var entries = new List<PropData>();
            foreach (var decoration in _config.decorations)
            {
                if (decoration != null && decoration.prefab != null) entries.Add(decoration);
            }

            ScatterPrefabs(_config, entries, _config.DecorationCount, _config.PropJitter,
                _config.DecorationClearRadius, _parent, "Decorations");
        }

        /// <summary>
        /// Drifting ambience (leaves / spirit motes) that follows the camera on a small pool.
        /// </summary>
        private void SpawnAmbience(MapConfigSO _config, Transform _parent)
        {
            if (_config.ambienceSprites == null || _config.ambienceSprites.Length == 0) return;
            if (_config.AmbienceCount <= 0) return;

            var holder = new GameObject("Ambience");
            holder.transform.SetParent(_parent, false);

            var drift = holder.AddComponent<AmbientDrift>();
            drift.Configure(
                _config.ambienceSprites,
                _config.AmbienceCount,
                _config.AmbienceScaleRange,
                _config.AmbienceSpeedRange,
                _config.AmbienceSwayRange,
                _config.AmbienceAlpha,
                _config.AmbienceArea);
        }

        private void ScatterPrefabs(MapConfigSO _config, List<PropData> entries, int count, float jitter,
            float clearRadius, Transform _parent, string holderName)
        {
            if (entries == null || entries.Count == 0) return;

            count = Mathf.Max(0, count);
            if (count == 0) return;

            var totalWeight = 0;
            foreach (var entry in entries) totalWeight += Mathf.Max(1, entry.weight);
            if (totalWeight <= 0) return;

            var holder = new GameObject(holderName);
            holder.transform.SetParent(_parent, false);

            var mapWidth = _config.Width * mTileSize;
            var mapHeight = _config.Height * mTileSize;
            var aspect = mapHeight / Mathf.Max(0.0001f, mapWidth);
            var rows = Mathf.Max(1, Mathf.CeilToInt(Mathf.Sqrt(count * aspect)));
            var cols = Mathf.Max(1, Mathf.CeilToInt((float)count / rows));
            var cellWidth = mapWidth / cols;
            var cellHeight = mapHeight / rows;
            var origin = new Vector3(-mapWidth * 0.5f, -mapHeight * 0.5f, 0f);
            jitter = Mathf.Clamp(jitter, 0f, 0.5f);
            var clearSqr = clearRadius * clearRadius;

            var spawned = 0;

            for (var y = 0; y < rows && spawned < count; y++)
            {
                for (var x = 0; x < cols && spawned < count; x++)
                {
                    var entry = PickProp(entries, totalWeight);
                    if (entry == null || entry.prefab == null) continue;

                    var clusterMin = Mathf.Min(entry.clusterMin, entry.clusterMax);
                    var clusterMax = Mathf.Max(entry.clusterMin, entry.clusterMax);
                    var cluster = Random.Range(clusterMin, clusterMax + 1);

                    for (var i = 0; i < cluster && spawned < count; i++)
                    {
                        var offsetX = (x + 0.5f + Random.Range(-jitter, jitter)) * cellWidth;
                        var offsetY = (y + 0.5f + Random.Range(-jitter, jitter)) * cellHeight;

                        if (i > 0)
                        {
                            var direction = Random.Range(0f, Mathf.PI * 2f);
                            var radius = Random.Range(0.3f, Mathf.Max(0.31f, entry.clusterRadius));
                            offsetX += Mathf.Cos(direction) * radius;
                            offsetY += Mathf.Sin(direction) * radius;
                        }

                        var position = origin + new Vector3(offsetX, offsetY, 0f);
                        if (position.sqrMagnitude < clearSqr) continue;

                        var angle = Random.Range(
                            Mathf.Min(entry.rotationRange.x, entry.rotationRange.y),
                            Mathf.Max(entry.rotationRange.x, entry.rotationRange.y));

                        var instance = Instantiate(
                            entry.prefab,
                            position,
                            Quaternion.Euler(0f, 0f, angle),
                            holder.transform);

                        var scale = Random.Range(
                            Mathf.Min(entry.scaleRange.x, entry.scaleRange.y),
                            Mathf.Max(entry.scaleRange.x, entry.scaleRange.y));
                        instance.transform.localScale = instance.transform.localScale * scale;

                        ApplyDepthSorting(instance);

                        spawned++;
                    }
                }
            }
        }

        /// <summary>
        /// Depth-sorts a spawn so characters and scenery overlap correctly: -y*100 on the
        /// root, with any child renderer (a contact shadow) sitting just below it. Prefabs
        /// that carry their own DynamicSortOrder override this at runtime.
        /// </summary>
        private static void ApplyDepthSorting(GameObject instance)
        {
            var order = (int)(-instance.transform.position.y * 100f);
            foreach (var renderer in instance.GetComponentsInChildren<SpriteRenderer>(true))
            {
                renderer.sortingOrder = renderer.gameObject == instance ? order : order - 50;
            }
        }

        private List<PropData> BuildPropEntries(MapConfigSO _config)
        {
            var entries = new List<PropData>();

            if (_config.props != null)
            {
                foreach (var prop in _config.props)
                {
                    if (prop != null && prop.prefab != null) entries.Add(prop);
                }
            }

            // Legacy path: keep working when only the single prop on MapManager is assigned.
            if (entries.Count == 0 && mProp != null)
            {
                entries.Add(new PropData
                {
                    prefab = mProp,
                    weight = 1,
                    scaleRange = _config.PropScaleRange,
                    rotationRange = _config.PropRotationRange
                });
            }

            return entries;
        }

        private static DecalData PickDecal(DecalData[] decals, int totalWeight)
        {
            var roll = Random.Range(0, totalWeight);
            var current = 0;

            foreach (var decal in decals)
            {
                if (decal == null || decal.sprite == null) continue;
                current += Mathf.Max(1, decal.weight);
                if (roll < current) return decal;
            }

            return null;
        }

        private static PropData PickProp(List<PropData> entries, int totalWeight)
        {
            var roll = Random.Range(0, totalWeight);
            var current = 0;

            foreach (var entry in entries)
            {
                current += Mathf.Max(1, entry.weight);
                if (roll < current) return entry;
            }

            return entries.Count > 0 ? entries[entries.Count - 1] : null;
        }
    }
}
