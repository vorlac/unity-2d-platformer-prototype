using System;
using System.Collections.Generic;
using System.Reflection;

namespace Core.Defs
{
    using UnityEditorInternal;

    [Flags]
    public enum Layer
    {
        All = 0,
        TransparentFX = 1,
        IgnoreRaycast = 2,
        Water = 4,
        UI = 5,
        Character = 8,
        Obstacle = 9,
        Debug = 10,
        LevelBounds = 11
    }
    
    public static class Tag
    {
        public static class Name
        {
            public static readonly string Untagged = "Untagged";
            public static readonly string Respawn = "Respawn";
            public static readonly string Finish = "Finish";
            public static readonly string MainCamera = "MainCamera";
            public static readonly string Player = "Player";
            public static readonly string Through = "Through";
            public static readonly string Wall = "Wall";
            public static readonly string Ground = "Ground";
            public static readonly string Static = "Static";
            public static readonly string Platform = "Platform";
            public static readonly string Dynamic = "Dynamic";
        }

        private static Dictionary<string, int> NameToIDMap = new Dictionary<string, int>();
        private static Dictionary<int, string> IDToNameMap = new Dictionary<int, string>();

        static Tag()
        {
            InitTagDataStructures();
        }

        private static void InitTagDataStructures()
        {
            for (int i = 0; i < InternalEditorUtility.tags.Length; i++)
            {
                int tagID = i;
                string tagName = InternalEditorUtility.tags[i];
                NameToIDMap[tagName] = tagID;
                IDToNameMap[tagID] = tagName;
            }

            // validate all Tag.Name fields
            ValidateTagNames();
        }

        private static void ValidateTagNames()
        {
            Type tagNamesType = typeof(Name);
            var fieldInfo = tagNamesType.GetFields(BindingFlags.Static | BindingFlags.Public);
            foreach (var prop in fieldInfo)
            {
                var tagName = prop.GetValue(null) as string;
                if (!IsValid(tagName))
                    throw new Exception($"Tag name definition is invalid: {tagName}");
            }
        }

        public static bool IsValid(string tag)
        {
            return NameToIDMap.ContainsKey(tag);
        }

        public static string GetTagName(int tagID)
        {
            string ret = null;
            if (IDToNameMap.ContainsKey(tagID))
                ret = IDToNameMap[tagID];
            return ret;
        }

        public static int? GetTagID(string tagName)
        {
            int? ret = null;
            if (NameToIDMap.ContainsKey(tagName))
                ret = NameToIDMap[tagName];
            return ret;
        }
    }
}
