using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace SysMetrix
{ 
    public static class JsonHelper
    {
        /// <summary>
        /// Serializes an object to JSON string with formatting
        /// </summary>
        public static string SerializeToJson(object obj)
        {
            var serializer = new JavaScriptSerializer();
            serializer.MaxJsonLength = int.MaxValue;

            // Serialize object
            var json = serializer.Serialize(obj);

            // Format JSON for readability
            return FormatJson(json);
        }

        /// <summary>
        /// Async version of SerializeToJson
        /// </summary>
        public static async Task<string> SerializeToJsonAsync(object obj)
        {
            return await Task.Run(() => SerializeToJson(obj));
        }

        /// <summary>
        /// Formats JSON string with proper indentation
        /// </summary>
        public static string FormatJson(string json)
        {
            var indentation = 0;
            var quoteChar = '"';
            var result = new StringBuilder();
            var isInsideQuotes = false;
            var previousChar = '\0';

            foreach (var ch in json)
            {
                if (ch == quoteChar && previousChar != '\\')
                {
                    isInsideQuotes = !isInsideQuotes;
                }

                if (!isInsideQuotes)
                {
                    if (ch == '{' || ch == '[')
                    {
                        result.Append(ch);
                        result.Append(Environment.NewLine);
                        indentation++;
                        result.Append(new string(' ', indentation * 2));
                    }
                    else if (ch == '}' || ch == ']')
                    {
                        result.Append(Environment.NewLine);
                        indentation--;
                        result.Append(new string(' ', indentation * 2));
                        result.Append(ch);
                    }
                    else if (ch == ',')
                    {
                        result.Append(ch);
                        result.Append(Environment.NewLine);
                        result.Append(new string(' ', indentation * 2));
                    }
                    else if (ch == ':')
                    {
                        result.Append(ch);
                        result.Append(" ");
                    }
                    else if (ch != ' ')
                    {
                        result.Append(ch);
                    }
                }
                else
                {
                    result.Append(ch);
                }

                previousChar = ch;
            }

            return result.ToString();
        }

        /// <summary>
        /// Async version of FormatJson
        /// </summary>
        public static async Task<string> FormatJsonAsync(string json)
        {
            return await Task.Run(() => FormatJson(json));
        }

        /// <summary>
        /// Serializes object to compact JSON (no formatting)
        /// </summary>
        public static string SerializeToCompactJson(object obj)
        {
            var serializer = new JavaScriptSerializer();
            serializer.MaxJsonLength = int.MaxValue;
            return serializer.Serialize(obj);
        }

        /// <summary>
        /// Async version of SerializeToCompactJson
        /// </summary>
        public static async Task<string> SerializeToCompactJsonAsync(object obj)
        {
            return await Task.Run(() => SerializeToCompactJson(obj));
        }

        /// <summary>
        /// Deserializes JSON string to object
        /// </summary>
        public static T DeserializeFromJson<T>(string json)
        {
            var serializer = new JavaScriptSerializer();
            serializer.MaxJsonLength = int.MaxValue;
            return serializer.Deserialize<T>(json);
        }

        /// <summary>
        /// Async version of DeserializeFromJson
        /// </summary>
        public static async Task<T> DeserializeFromJsonAsync<T>(string json)
        {
            return await Task.Run(() => DeserializeFromJson<T>(json));
        }
    }
     
} 