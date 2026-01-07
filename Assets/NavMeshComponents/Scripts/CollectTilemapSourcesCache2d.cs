using System.Collections.Generic;
using NavMeshPlus.Components;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Tilemaps;

namespace NavMeshPlus.Extensions
{
    [ExecuteAlways]
    [AddComponentMenu("Navigation/Navigation CacheTilemapSources2d", 30)]
    public class CollectTilemapSourcesCache2d : NavMeshExtension
    {
        [SerializeField] private Tilemap _tilemap;
        [SerializeField] private NavMeshModifier _modifier;
        [SerializeField] private NavMeshModifierTilemap _modifierTilemap;
        private Dictionary<Vector3Int, int> _lookup;
        private Dictionary<TileBase, NavMeshModifierTilemap.TileModifier> _modifierMap;

        private List<NavMeshBuildSource> _sources;

        protected override void Awake()
        {
            _modifier ??= _tilemap.GetComponent<NavMeshModifier>();
            _modifierTilemap ??= _tilemap.GetComponent<NavMeshModifierTilemap>();
            _modifierMap = _modifierTilemap.GetModifierMap();
            Order = -1000;
            base.Awake();
        }

        protected override void OnDestroy()
        {
#if UNITY_EDITOR || UNITY_2022_2_OR_NEWER
            Tilemap.tilemapTileChanged -= OnTilemapTileChanged;
#endif
            base.OnDestroy();
        }

#if UNITY_EDITOR || UNITY_2022_2_OR_NEWER
        private void OnTilemapTileChanged(Tilemap tilemap, Tilemap.SyncTile[] syncTiles)
        {
            if (tilemap == _tilemap)
                foreach (var syncTile in syncTiles)
                {
                    var position = syncTile.position;
                    if (syncTile.tile != null && _modifierMap.TryGetValue(syncTile.tile, out var tileModifier))
                    {
                        var i = _lookup[position];
                        var source = _sources[i];
                        source.area = tileModifier.area;
                        _sources[i] = source;
                    }
                    else if (_modifier.overrideArea)
                    {
                        var i = _lookup[position];
                        var source = _sources[i];
                        source.area = _modifier.area;
                        _sources[i] = source;
                    }
                }
        }
#endif


        public AsyncOperation UpdateNavMesh(NavMeshData data)
        {
            return NavMeshBuilder.UpdateNavMeshDataAsync(data, NavMeshSurfaceOwner.GetBuildSettings(), _sources,
                data.sourceBounds);
        }

        public AsyncOperation UpdateNavMesh()
        {
            return UpdateNavMesh(NavMeshSurfaceOwner.navMeshData);
        }

        public override void PostCollectSources(NavMeshSurface surface, List<NavMeshBuildSource> sources,
            NavMeshBuilderState navNeshState)
        {
            _sources = sources;
            if (_lookup == null)
            {
                _lookup = new Dictionary<Vector3Int, int>();
                for (var i = 0; i < _sources.Count; i++)
                {
                    var source = _sources[i];
                    var position = _tilemap.WorldToCell(source.transform.GetPosition());
                    _lookup[position] = i;
                }
            }
#if UNITY_EDITOR || UNITY_2022_2_OR_NEWER
            Tilemap.tilemapTileChanged -= OnTilemapTileChanged;
            Tilemap.tilemapTileChanged += OnTilemapTileChanged;
#endif
        }
    }
}