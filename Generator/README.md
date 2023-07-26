# OneOfUnity.Generator

## This is not a package

This is the tool that generates the `.generated.cs` files and makes sure package files are synchronized.

To use it initialize an empty Unity project and download the entire repository into the `Assets` folder. Once scripts compile a new dropdown will appear on the toolbar in your editor called `Tools` with a single button called `Regenerate OneOf files`. Pressing said button will cause the `Generator()` function to run.

## Contribution guide

When making any updates make sure to update the 
```csharp
private const string version
```
variable.