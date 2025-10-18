using SetlistStudio.Core.Validation;

var attr = new SanitizedStringAttribute();

// Test whitespace
var input1 = "   ";
var result1 = attr.SanitizeInput(input1);
Console.WriteLine($"Input: '{input1}' -> Output: '{result1}' (Length: {result1.Length})");

// Test special characters
var attr2 = new SanitizedStringAttribute { AllowSpecialCharacters = false };
var input2 = "Song@Title#With$Special%Characters^&*";
var result2 = attr2.SanitizeInput(input2);
Console.WriteLine($"Input: '{input2}' -> Output: '{result2}'");

// Test JavaScript
var input3 = "Click javascript:alert('xss') here";
var result3 = attr.SanitizeInput(input3);
Console.WriteLine($"Input: '{input3}' -> Output: '{result3}'");

// Test null/empty
var result4 = attr.SanitizeInput(null);
Console.WriteLine($"Input: null -> Output: '{result4}'");