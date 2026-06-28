using System.Collections.Generic;
using UnityEngine;
using System.Reflection;
using UnityEngine.Events;
using System.Linq;
using UnityEditor;

namespace UnityEventReferenceViewer
{
    public class EventReferenceInfoDictionary
    {
        public Dictionary<GameObject, Dictionary<MonoBehaviour, List<EventReferenceInfo>>> dict;
        public EventReferenceInfoDictionary() 
        {
            dict = new Dictionary<GameObject, Dictionary<MonoBehaviour, List<EventReferenceInfo>>>();
        }
    }
    public class EventReferenceInfo
    {
        public MonoBehaviour listener { get; set; }
        public string methodName { get; set; }
    }

    public class UnityEventReferenceFinder : MonoBehaviour
    {
        public static EventReferenceInfoDictionary FindAllUnityEventsReferences()
        {
            var assets = AssetDatabase.FindAssets("t:prefab", new string[] { });
            var events = new EventReferenceInfoDictionary();
            foreach (var a in assets)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(a));
                foreach (var behavior in prefab.GetComponentsInChildren<MonoBehaviour>())
                {
                    var bTypeInfo = behavior.GetType().GetTypeInfo();
                    var bEvents = bTypeInfo.DeclaredFields.Where(f => f.FieldType.IsSubclassOf(typeof(UnityEventBase))).ToList();
                    foreach (var e in bEvents)
                    {
                        var unityEvent = e.GetValue(behavior) as UnityEventBase;

                        int count = unityEvent.GetPersistentEventCount();
                        var infos = new List<EventReferenceInfo>();
                        for (int i = 0; i < count; i++)
                        {
                            var obj = unityEvent.GetPersistentTarget(i);
                            var method = unityEvent.GetPersistentMethodName(i);

                            var einfo = new EventReferenceInfo();
                            einfo.listener = obj as MonoBehaviour;
                            einfo.methodName = obj.GetType().Name.ToString() + "." + method;
                            
                            infos.Add(einfo);
                        }

                        if (infos.Count > 0)
                        {
                            if (!events.dict.ContainsKey(prefab))
                                events.dict.Add(prefab, new Dictionary<MonoBehaviour, List<EventReferenceInfo>>());
                            if(!events.dict[prefab].ContainsKey(behavior))
                                events.dict[prefab][behavior] = new List<EventReferenceInfo>();
                            events.dict[prefab][behavior].AddRange(infos);
                        }
                    }
                }
            }

            return events;
        }
    }
}