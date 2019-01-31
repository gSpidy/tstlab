using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

public static class EssentialExtensions {
    
    #region Transform Extensions
    
        public static IEnumerable<IndexerPair<Transform>> ChildsEnumerated(this Transform tr)
        {
            return Enumerable.Range(0, tr.childCount)
                .Select(x => new IndexerPair<Transform> {index = x, item = tr.GetChild(x)});
        }
    
        public static IEnumerable<IndexerPair<T>> ChildsEnumerated<T>(this Transform tr) where T:Component
        {
            return ChildsEnumerated(tr)
                .Select(x => new IndexerPair<T> {index = x.index, item = x.item.GetComponent<T>()});
        }
    
        /// <summary>
        /// скопировать позицию и поворот (свои и всех чилдренов)
        /// используется, например, при спавне рэгдоллов
        /// 
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        public static void CopyTo(this Transform from, Transform to)
        {
            to.position = from.position;
            to.rotation = from.rotation;
    
            var min = Mathf.Min(from.childCount, to.childCount);
            for (var i = 0; i < min; i++)
                from.GetChild(i).CopyTo(to.GetChild(i));
        }

        public static void SlerpRotation(this Transform host, Quaternion b, float t)
        {
            host.rotation = Quaternion.Slerp(host.rotation,b,t);
        }
    
    #endregion
    
    
    #region Object.Instantiate Extensions
    
        public static T InstantiateMe<T>(this T original) where T : Object
        {
            return Object.Instantiate(original);
        }
        
        public static T InstantiateMe<T>(this T original, Vector3 position, Quaternion rotation) where T : Object
        {
            return Object.Instantiate(original, position, rotation);
        }
    
        public static T InstantiateMe<T>(this T original, Vector3 position, Quaternion rotation, Transform parent) where T : Object
        {
            return Object.Instantiate(original, position, rotation, parent);
        }
    
        public static T InstantiateMe<T>(this T original, Transform parent, bool worldPositionStays) where T : Object
        {
            return Object.Instantiate(original, parent, worldPositionStays);
        }
        
        public static T InstantiateMe<T>(this T original, Transform parent) where T : Object
        {
            return original.InstantiateMe(parent, false);
        }
    
    #endregion
}
