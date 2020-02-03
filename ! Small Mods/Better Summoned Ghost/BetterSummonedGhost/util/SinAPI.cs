﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.Reflection;
using System.IO;
using System.Text.RegularExpressions;

namespace BetterSummonedGhost
{
    public static class At // Access Tools
    {
        public static BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static;

        //reflection call
        public static object Call(object obj, string method, params object[] args)
        {
            var methodInfo = obj.GetType().GetMethod(method, flags);
            if (methodInfo != null)
            {
                return methodInfo.Invoke(obj, args);
            }
            return null;
        }

        // set value
        public static void SetValue<T>(T value, Type type, object obj, string field)
        {
            FieldInfo fieldInfo = type.GetField(field, flags);
            if (fieldInfo != null)
            {
                fieldInfo.SetValue(obj, value);
            }
        }

        // get value
        public static object GetValue(Type type, object obj, string value)
        {
            FieldInfo fieldInfo = type.GetField(value, flags);
            if (fieldInfo != null)
            {
                return fieldInfo.GetValue(obj);
            }
            else
            {
                return null;
            }
        }

        // inherit base values
        public static void InheritBaseValues(object _derived, object _base)
        {
            foreach (FieldInfo fi in _base.GetType().GetFields(flags))
            {
                try { _derived.GetType().GetField(fi.Name).SetValue(_derived, fi.GetValue(_base)); } catch { }
            }

            return;
        }
    }

    public static class Jt // Json Tools
    {
        public static void SaveJsonOverwrite(string path, object obj)
        {
            if (File.Exists(path)) { File.Delete(path); }

            File.WriteAllText(path, JsonUtility.ToJson(obj, true));
        }

        // will try to save as the name.json, then name_2.json, then name_3.json, with a limit of 500
        private static void SaveJsonRecursive(object obj, string folderPath, string name, int count)
        {
            string path = "";
            if (count == 1) { path = folderPath + "/" + name + ".json"; }
            else            { path = folderPath + "/" + name + "_" + count + ".json"; }

            if (File.Exists(path))
            {
                if (count >= 500) { return; }

                SaveJsonRecursive(obj, folderPath, name, count + 1);
            }
            else
            {
                File.WriteAllText(path, JsonUtility.ToJson(obj, true));
            }
        }

        // json appender
        // the Dictionary Key is the name of the JSON file or whatever key you want the values to be. The key name cannot be the same as any existing top-level field name in the JSON.
        // the Dictionary value is the raw json dump from JSONUtility.ToJson()

        // if planning on un-appending these jsons back into serialized objects, the keys need to be all in the format: "Keyname_Count", eg "DropTable_0", "DropTable_1", etc
        // When un-appending, pass the keyname without the underscore or number to the un-appender function. Eg JsonDeAppender(Type t, "DropTable", ref json)

        public static string AppendJsonList(string orig, Dictionary<string, string> toAppend)
        {
            string saveFix = orig.Substring(0, orig.Length - 2); // remove the closing '}' and new line

            int i = 0;
            foreach (KeyValuePair<string, string> entry in toAppend)
            {
                if (i == 0) { saveFix += ","; }
                i++;
                saveFix += "\r\n	\"" + entry.Key + "\" : {"; // add the ', "objectname" : {'
                saveFix += entry.Value.Substring(1, entry.Value.Length - 2); // add the json without the opening and closing brackets
                saveFix += "	}";
                if (i < toAppend.Count)
                    saveFix += ",";
            }

            return saveFix += "\r\n}"; // restore the closing '}'
        }

        // Json Un-Appender (fixer)

        // Give it a Json string generated by the function above (AppendJsonList), and it returns the list of appended objects, and the original, unappended string.
        
        // Type t: the expected return type of the appended object(s)
        // string fieldNames: the Dictionary Key of the appended objects, assuming they all follow the format 'FieldName_Count'
        // ref string orig: Give it the entire json as a reference, it will cut off the appended jsons that it returns in the list
        // return: List<object> - your list of returned Type t objects

        public static List<object> JsonDeAppender(Type t, string fieldNames, ref string orig)
        {
            List<object> returnList = new List<object>();

            int firstmatch = -1;

            for (int i = 0; i < 99; i++)
            {
                Regex rx = new Regex(fieldNames + "_" + i);

                if (rx.Match(orig) is Match match1 && match1.Success)
                {
                    if (i == 1) { firstmatch = match1.Index; }

                    int trimStart = fieldNames.Length + i.ToString().Length + 4; // gets the number of characters in your field name, plus the count (to string), plus 4
                    int startpos = match1.Index + trimStart; // the starting point of the appended values, after the opening '{'
                    int length = orig.Length - 2 - startpos; // by default, it will end where the json ends

                    // check if theres more fields
                    string s2 = fieldNames + "_" + (i + 1);
                    Regex rx2 = new Regex(s2);
                    if (rx2.Match(orig) is Match match2 && match2.Success)
                    {
                        length = match2.Index - 5 - startpos; // if theres another type appended, set the trim end to the start of this field name
                    }

                    // add the actual FromJsonOverwrite Type t object to our return list
                    string fix = orig.Substring(startpos, length);
                    object newObject = Activator.CreateInstance(t); // Activator class is OP
                    JsonUtility.FromJsonOverwrite(fix, newObject);

                    returnList.Add(newObject);
                }
                else
                {
                    break;
                }
            }

            // fix the orig string to not contain the fields
            if (firstmatch != -1) { orig = orig.Substring(0, firstmatch - 5) + "}"; }

            return returnList;
        }
    }
}
