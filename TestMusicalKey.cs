using System.ComponentModel.DataAnnotations;
using SetlistStudio.Core.Validation;

// Simple test for MusicalKeyAttribute behavior
var attribute = new MusicalKeyAttribute();
var context = new ValidationContext(new object()) { MemberName = "TestKey" };

// Test cases
string[] testCases = { "", "   ", "C", "InvalidKey" };
object[] nonStringCases = { 123, true, new object() };

Console.WriteLine("=== String Test Cases ===");
foreach (var test in testCases)
{
    var result = attribute.GetValidationResult(test, context);
    var isValid = attribute.IsValid(test);
    Console.WriteLine($"Value: '{test}' | IsValid: {isValid} | ValidationResult: {(result == ValidationResult.Success ? "Success" : result?.ErrorMessage)}");
}

Console.WriteLine("\n=== Non-String Test Cases ===");
foreach (var test in nonStringCases)
{
    var result = attribute.GetValidationResult(test, context);
    var isValid = attribute.IsValid(test);
    Console.WriteLine($"Value: {test} ({test.GetType()}) | IsValid: {isValid} | ValidationResult: {(result == ValidationResult.Success ? "Success" : result?.ErrorMessage)}");
}