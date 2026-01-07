using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

//***********************************************************************************
// Contributed by author jl-randazzo github.com/jl-randazzo
//***********************************************************************************
namespace NavMeshPlus.Components.Editors
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(NavMeshModifierTilemap))]
    internal class NavMeshModifierTilemapEditor : Editor
    {
        private SerializedProperty m_AffectedAgents;
        private SerializedProperty m_TileModifiers;

        private void OnEnable()
        {
            m_AffectedAgents = serializedObject.FindProperty("m_AffectedAgents");
            m_TileModifiers = serializedObject.FindProperty("m_TileModifiers");
        }

        public override void OnInspectorGUI()
        {
            var modifierTilemap = target as NavMeshModifierTilemap;

            serializedObject.Update();

            NavMeshComponentsGUIUtility.AgentMaskPopup("Affected Agents", m_AffectedAgents);

            EditorGUILayout.PropertyField(m_TileModifiers);

            if (modifierTilemap.HasDuplicateTileModifiers())
                EditorGUILayout.HelpBox(
                    "There are duplicate Tile entries in the tilemap modifiers! Only the first will be used.",
                    MessageType.Warning);

            EditorGUILayout.Space();

            var tilemap = modifierTilemap.GetComponent<Tilemap>();
            if (tilemap)
            {
                if (GUILayout.Button("Add Used Tiles")) AddUsedTiles(tilemap, modifierTilemap);
            }
            else
            {
                EditorGUILayout.HelpBox("Missing required component 'Tilemap'", MessageType.Error);
            }

            if (serializedObject.ApplyModifiedProperties()) modifierTilemap.CacheModifiers();
        }

        private void AddUsedTiles(Tilemap tilemap, NavMeshModifierTilemap modifierTilemap)
        {
            var tileModifiers = modifierTilemap.GetModifierMap();

            var bounds = tilemap.cellBounds;
            for (var i = bounds.xMin; i <= bounds.xMax; i++)
            for (var j = bounds.yMin; j <= bounds.yMax; j++)
            for (var k = bounds.zMin; k <= bounds.zMax; k++)
                if (tilemap.GetTile(new Vector3Int(i, j, k)) is TileBase tileBase)
                    if (!tileModifiers.ContainsKey(tileBase))
                    {
                        tileModifiers.Add(tileBase, new NavMeshModifierTilemap.TileModifier());

                        var idx = m_TileModifiers.arraySize;
                        m_TileModifiers.InsertArrayElementAtIndex(idx);
                        var newElem = m_TileModifiers.GetArrayElementAtIndex(idx);
                        var tileProperty =
                            newElem.FindPropertyRelative(nameof(NavMeshModifierTilemap.TileModifier.tile));
                        tileProperty.objectReferenceValue = tileBase;
                    }
        }

        [CustomPropertyDrawer(typeof(NavMeshModifierTilemap.TileModifier))]
        private class TileModifierPropertyDrawer : PropertyDrawer
        {
            private static Dictionary<Object, Texture2D> Previews;

            private Rect ClaimAdvance(ref Rect position, float height)
            {
                var retVal = position;
                retVal.height = height;
                position.y += height;
                position.height -= height;
                return retVal;
            }

            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                var expandRect = ClaimAdvance(ref position, 20);
                property.isExpanded = EditorGUI.Foldout(expandRect, property.isExpanded, label);
                if (property.isExpanded)
                {
                    var tileProperty = property.FindPropertyRelative(nameof(NavMeshModifierTilemap.TileModifier.tile));
                    var tileRect = ClaimAdvance(ref position, 40);
                    tileRect.width -= 40;

                    var previewRect = tileRect;
                    previewRect.width = 40;
                    previewRect.x += tileRect.width;
                    tileRect.height /= 2;

                    // Adding the tile selector and a preview image.
                    EditorGUI.PropertyField(tileRect, tileProperty);
                    var tileBase = tileProperty.objectReferenceValue as TileBase;
                    var tileData = new TileData();
                    Texture textureToDraw;
                    try
                    {
                        tileBase?.GetTileData(Vector3Int.zero, null, ref tileData);
                        textureToDraw = tileData.sprite?.texture;
                    }
                    catch
                    {
                        try
                        {
                            textureToDraw = GetPreview(tileBase);
                        }
                        catch
                        {
                            textureToDraw = EditorGUIUtility.IconContent("console.erroricon.sml").image;
                        }
                    }

                    if (textureToDraw)
                        EditorGUI.DrawPreviewTexture(previewRect, textureToDraw, null, ScaleMode.ScaleToFit, 0);

                    var toggleRect = ClaimAdvance(ref position, 20);
                    var overrideAreaProperty =
                        property.FindPropertyRelative(nameof(NavMeshModifierTilemap.TileModifier.overrideArea));
                    EditorGUI.PropertyField(toggleRect, overrideAreaProperty);

                    if (overrideAreaProperty.boolValue)
                    {
                        var areaRect = ClaimAdvance(ref position, 20);
                        var areaProperty =
                            property.FindPropertyRelative(nameof(NavMeshModifierTilemap.TileModifier.area));
                        EditorGUI.indentLevel++;
                        EditorGUI.PropertyField(areaRect, areaProperty);
                        EditorGUI.indentLevel--;
                    }
                }
            }

            private static Texture2D GetPreview(Object objectToPreview)
            {
                var maxResolution = 128;
                Previews ??= new Dictionary<Object, Texture2D>();
                if (!Previews.TryGetValue(objectToPreview, out var preview) || preview == null)
                {
                    var path = AssetDatabase.GetAssetPath(objectToPreview);
                    if (objectToPreview)
                    {
                        var editor = CreateEditor(objectToPreview);
                        preview = editor.RenderStaticPreview(path, null, maxResolution, maxResolution);
                        preview.Apply();
                        DestroyImmediate(editor);
                        Previews[objectToPreview] = preview;
                    }
                }

                return preview;
            }

            public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
            {
                if (property.isExpanded)
                {
                    var overrideAreaProperty =
                        property.FindPropertyRelative(nameof(NavMeshModifierTilemap.TileModifier.overrideArea));
                    if (overrideAreaProperty.boolValue) return 100;
                    return 80;
                }

                return 20;
            }
        }
    }
}