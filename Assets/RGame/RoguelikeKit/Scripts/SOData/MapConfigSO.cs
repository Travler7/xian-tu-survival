#region
using System;
using UnityEngine;

#endregion

namespace RGame.RoguelikeKit
{
    [CreateAssetMenu(fileName = "WaveConfig", menuName = "RGame/RoguelikeKit/Map Config")]
    public class MapConfigSO : ScriptableObject
    {
        [Tooltip("Array of tiles used to generate the map. Each tile has a sprite and a weight.")]
        public TileData[] tiles;

        [Tooltip("Optional material used by generated map tiles. When empty, MapManager uses its default material.")]
        public Material mapMaterial;

        [Tooltip("Optional continuous background used instead of per-tile generation. When empty, MapManager keeps the tiled map workflow.")]
        public Sprite backgroundSprite;

        public int Width = 50;
        public int Height = 50;

        [Range(0, 100)] public int PropCount = 15;

        [Header("Ground Decals (pure decoration, no gameplay)")]
        [Tooltip("Weighted soft-edged decals scattered above the background. Leave empty to skip the decal pass.")]
        public DecalData[] decals;

        [Tooltip("Average world-space distance between decal candidates. Smaller = denser.")]
        [Min(0.5f)] public float DecalSpacing = 4f;

        [Range(0f, 1f)]
        [Tooltip("Chance that a decal candidate inside a cell is actually spawned.")]
        public float DecalDensity = 0.75f;

        [Tooltip("Decals are kept away from the map centre (player spawn) by this many world units.")]
        [Min(0f)] public float DecalClearRadius = 5f;

        [Header("Props")]
        [Tooltip("Weighted prop prefabs. Leave empty to fall back to the single prop assigned on MapManager.")]
        public PropData[] props;

        [Range(0f, 0.5f)]
        [Tooltip("Random offset inside a grid cell, as a fraction of the cell size. 0 = rigid grid.")]
        public float PropJitter = 0.45f;

        [Tooltip("Uniform scale range used for the legacy single-prop fallback.")]
        public Vector2 PropScaleRange = new Vector2(0.85f, 1.15f);

        [Tooltip("Rotation range in degrees for the legacy single-prop fallback. Keep small for objects with a directional drop shadow.")]
        public Vector2 PropRotationRange = new Vector2(-18f, 18f);

        [Range(1, 4)] public int PropClusterMin = 1;
        [Range(1, 5)] public int PropClusterMax = 2;

        [Tooltip("Props are kept away from the map centre (player spawn) by this many world units.")]
        [Min(0f)] public float PropClearRadius = 6f;

        [Header("Decorations (standing scenery, no gameplay)")]
        [Tooltip("Weighted decoration prefabs scattered like props but with no loot and no colliders.")]
        public PropData[] decorations;

        [Min(0)] public int DecorationCount = 40;

        [Tooltip("Decorations are kept away from the map centre (player spawn) by this many world units.")]
        [Min(0f)] public float DecorationClearRadius = 4f;

        [Header("Ambience (drifting particles)")]
        [Tooltip("Sprites that drift around the camera: falling leaves, spirit motes, mist.")]
        public Sprite[] ambienceSprites;

        [Min(0)] public int AmbienceCount = 46;

        public Vector2 AmbienceScaleRange = new Vector2(0.18f, 0.5f);

        [Tooltip("Fall / drift speed range in world units per second.")]
        public Vector2 AmbienceSpeedRange = new Vector2(0.15f, 0.5f);

        [Tooltip("Horizontal sway range in world units per second.")]
        public Vector2 AmbienceSwayRange = new Vector2(-0.45f, 0.45f);

        [Range(0f, 1f)] public float AmbienceAlpha = 0.7f;

        public Vector2 AmbienceArea = new Vector2(36f, 22f);
    }

    [Serializable]
    public class TileData
    {
        [Tooltip("The sprite used for this tile.")]
        public Sprite sprite;

        [Tooltip("The weight of this tile for random generation. Higher weight means higher probability of being chosen.")]
        public int weight;
    }

    [Serializable]
    public class DecalData
    {
        public Sprite sprite;

        [Min(1)] public int weight = 1;

        [Tooltip("Uniform world-space scale range for this decal.")]
        public Vector2 scaleRange = new Vector2(1.6f, 3.5f);

        [Range(0f, 1f)] public float minAlpha = 0.25f;

        [Range(0f, 1f)] public float maxAlpha = 0.6f;

        [Range(0f, 180f)]
        [Tooltip("Maximum absolute rotation in degrees. Keep small (5-25) for anything with baked top lighting or a natural up direction: grass, flowers, bamboo, stones, water. Use 180 only for flat ground litter such as leaves, moss and pebbles.")]
        public float maxRotation = 180f;

        [Range(1, 6)]
        [Tooltip("How many copies spawn around the picked spot. Use 2-4 for grass and pebbles so they read as patches instead of isolated dots.")]
        public int clusterMin = 1;

        [Range(1, 6)]
        public int clusterMax = 1;

        [Min(0f)]
        [Tooltip("Scatter radius used by the extra cluster copies.")]
        public float clusterRadius = 0.6f;
    }

    [Serializable]
    public class PropData
    {
        public GameObject prefab;

        [Min(1)] public int weight = 1;

        public Vector2 scaleRange = new Vector2(0.85f, 1.15f);

        public Vector2 rotationRange = new Vector2(-18f, 18f);

        [Range(1, 6)] public int clusterMin = 1;
        [Range(1, 6)] public int clusterMax = 1;
        [Min(0f)] public float clusterRadius = 0.8f;
    }
}
