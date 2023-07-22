using System.Collections.Generic;
using UnityEngine;

namespace Utils
{
    using Core.Geom;

    static class World
    {
        static public int GetLayerID(string layerName)
        {
            return LayerMask.NameToLayer(layerName);
        }

        static public string GetLayerName(int layerID)
        {
            return LayerMask.LayerToName(layerID);
        }

        static public List<int> GetLayerIDs(LayerMask layerMask)
        {
            List<int> layerIDs = new List<int>();

            const int SCENE_MAX_LAYER_COUNT = 32;
            for (int layerID = 0; layerID < SCENE_MAX_LAYER_COUNT; ++layerID)
            {
                int currMask = (1 << layerID);
                if ((layerMask.value & currMask) != 0)
                    layerIDs.Add(layerID);
            }

            return layerIDs;
        }

        static public HashSet<GameObject> GetSceneObjects(LayerMask layerMask)
        {
            var layerIDs = World.GetLayerIDs(layerMask);
            return World.GetLayerObjects(layerIDs);
        }

        static public GameObject[] GetTagObjects(string tag)
        {
            return GameObject.FindGameObjectsWithTag(tag);
        }

        static public HashSet<GameObject> GetLayerObjects(List<int> layerIDs)
        {
            HashSet<GameObject> layerObjects = new HashSet<GameObject>();

            var objects = UnityEngine.Object.FindObjectsOfType(typeof(GameObject));
            foreach (GameObject obj in objects)
            {
				// TODO: figure out how/why some objects were null
                if (obj == null)
                    continue;

                // add the object if it's in one of the layers
                if (layerIDs.Exists(elem => elem == obj.layer))
                    layerObjects.Add(obj);
            }

            return layerObjects;
        }

        static public List<GameObject> GetTagObjects(List<string> tags)
        {
            List<GameObject> tagObjects = new List<GameObject>();

            foreach (var tag in tags)
                tagObjects.AddRange(GameObject.FindGameObjectsWithTag(tag));
            
            return tagObjects;
        }

        static public HashSet<GameObject> GetWorldObjects()
        {
            var layerIDs = World.GetLayerIDs(0xFFFF);
            return World.GetLayerObjects(layerIDs);
        }

        static public Rect2 GetWorldBounds()
        {
            Rect2 worldRect = Rect2.Empty;

            var layerObjects = World.GetWorldObjects();
            foreach (var obj in layerObjects)
            {
                var objRenderer = obj.GetComponent<Renderer>();
                if (objRenderer != null)
                {
                    Rect2 objRect = Rect2.FromBounds(objRenderer.bounds);
                    worldRect.ExpandTo(objRect);
                }
            }
            
            return worldRect;
        }

        static public Vector2 GetMousePointWorldCoords()
        {
            return Camera.main.ScreenToWorldPoint(Input.mousePosition);
        }
    }
}
