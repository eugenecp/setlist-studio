using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SetlistStudio.Core.Security;
using SetlistStudio.Core.Validation;
using System.Reflection;

namespace SetlistStudio.Web.Security
{
    /// <summary>
    /// Validates and sanitizes all incoming request data to prevent security vulnerabilities.
    /// This filter addresses CWE-117 (Log Injection) and CWE-79 (XSS) at the input level.
    /// </summary>
    public class InputSanitizationAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            // Validate and sanitize all action parameters
            var parametersToSanitize = context.ActionArguments.Where(p => p.Value != null).ToList();
            foreach (var parameter in parametersToSanitize)
            {
                var sanitizedValue = SanitizeObject(parameter.Value!);
                context.ActionArguments[parameter.Key] = sanitizedValue;
            }

            // Validate model state after sanitization
            if (!context.ModelState.IsValid)
            {
                var errors = context.ModelState
                    .Where(x => x.Value?.Errors.Count > 0)
                    .ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                    );

                context.Result = new BadRequestObjectResult(new
                {
                    message = "Validation failed",
                    errors = errors
                });
            }

            base.OnActionExecuting(context);
        }

        /// <summary>
        /// Recursively sanitizes an object and its properties.
        /// </summary>
        /// <param name="obj">The object to sanitize</param>
        /// <returns>The sanitized object</returns>
        private static object? SanitizeObject(object? obj)
        {
            if (obj == null)
                return obj;

            var type = obj.GetType();

            // Handle primitive types and strings
            if (type == typeof(string))
                return SanitizeStringValue((string)obj);

            // Handle value types (int, bool, etc.)
            if (type.IsValueType || type.IsPrimitive)
                return obj;

            // Handle dictionaries specially to preserve type
            if (obj is System.Collections.IDictionary dictionary)
                return SanitizeDictionary(dictionary);

            // Handle other collections
            if (obj is System.Collections.IEnumerable enumerable && !(obj is string))
                return SanitizeCollection(enumerable);

            // Handle complex objects
            return SanitizeComplexObject(obj);
        }

        /// <summary>
        /// Sanitizes string values using configured sanitization rules.
        /// </summary>
        /// <param name="value">The string to sanitize</param>
        /// <returns>The sanitized string</returns>
        private static string SanitizeStringValue(string value)
        {
            var sanitizer = new SanitizedStringAttribute { AllowHtml = false, AllowSpecialCharacters = true };
            return sanitizer.SanitizeInput(value);
        }

        /// <summary>
        /// Sanitizes dictionary values while preserving the dictionary structure.
        /// </summary>
        /// <param name="dictionary">The dictionary to sanitize</param>
        /// <returns>The sanitized dictionary</returns>
        private static object SanitizeDictionary(System.Collections.IDictionary dictionary)
        {
            var keys = new List<object>();
            foreach (var key in dictionary.Keys)
            {
                keys.Add(key);
            }

            foreach (var key in keys)
            {
                var value = dictionary[key];
                if (value != null)
                {
                    var sanitizedValue = SanitizeObject(value);
                    dictionary[key] = sanitizedValue;
                }
            }
            return dictionary;
        }

        /// <summary>
        /// Sanitizes collection items and returns a new sanitized collection.
        /// </summary>
        /// <param name="enumerable">The collection to sanitize</param>
        /// <returns>A new collection with sanitized items</returns>
        private static List<object> SanitizeCollection(System.Collections.IEnumerable enumerable)
        {
            var list = new List<object>();
            foreach (var item in enumerable)
            {
                var sanitizedItem = SanitizeObject(item);
                if (sanitizedItem != null)
                {
                    list.Add(sanitizedItem);
                }
            }
            return list;
        }

        /// <summary>
        /// Sanitizes properties of complex objects using reflection.
        /// </summary>
        /// <param name="obj">The complex object to sanitize</param>
        /// <returns>The sanitized object</returns>
        private static object SanitizeComplexObject(object obj)
        {
            var type = obj.GetType();
            var properties = GetSanitizableProperties(type);

            foreach (var property in properties)
            {
                SanitizeProperty(obj, property);
            }

            return obj;
        }

        /// <summary>
        /// Gets properties that can be sanitized (readable and writable).
        /// </summary>
        /// <param name="type">The type to examine</param>
        /// <returns>Collection of sanitizable properties</returns>
        private static IEnumerable<PropertyInfo> GetSanitizableProperties(Type type)
        {
            return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.CanWrite);
        }

        /// <summary>
        /// Safely sanitizes a single property value.
        /// </summary>
        /// <param name="obj">The object containing the property</param>
        /// <param name="property">The property to sanitize</param>
        private static void SanitizeProperty(object obj, PropertyInfo property)
        {
            try
            {
                var value = property.GetValue(obj);
                if (value != null)
                {
                    var sanitizedValue = SanitizeObject(value);
                    property.SetValue(obj, sanitizedValue);
                }
            }
            catch
            {
                // If we can't sanitize a property, skip it
                // This handles cases where properties might have complex setters or validation
            }
        }
    }
}