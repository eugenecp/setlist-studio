using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SetlistStudio.Core.Security;
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
            foreach (var parameter in context.ActionArguments.ToList())
            {
                if (parameter.Value != null)
                {
                    var sanitizedValue = SanitizeObject(parameter.Value);
                    context.ActionArguments[parameter.Key] = sanitizedValue;
                }
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
            {
                return SecureLoggingHelper.SanitizeMessage((string)obj) ?? string.Empty;
            }

            // Handle value types (int, bool, etc.)
            if (type.IsValueType || type.IsPrimitive)
            {
                return obj;
            }

            // Handle collections
            if (obj is System.Collections.IEnumerable enumerable && !(obj is string))
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

            // Handle complex objects
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.CanWrite);

            foreach (var property in properties)
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
                    continue;
                }
            }

            return obj;
        }
    }
}