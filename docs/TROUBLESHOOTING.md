# Troubleshooting Guide

## Common Issues and Solutions

### Build and Runtime Issues

#### .NET Version Compatibility

**Problem**: `NETSDK1045: The current .NET SDK does not support targeting .NET 9.0`

**Solution**: 
- If you encounter .NET 9.0 targeting issues, temporarily target .NET 8.0:
  ```xml
  <TargetFramework>net8.0</TargetFramework>
  ```
- Ensure you have the appropriate .NET SDK installed for your target framework
- Check your global.json file for SDK version constraints

#### Package Restoration Issues

**Problem**: Package restore failures or version conflicts

**Solution**:
```bash
# Clear NuGet cache
dotnet nuget locals all --clear

# Restore packages
dotnet restore

# If issues persist, delete bin/obj folders
find . -name "bin" -o -name "obj" | xargs rm -rf
dotnet restore
```

### Performance Issues

#### Slow Semantic Chunking

**Symptoms**: 
- Chunking takes longer than expected
- High memory usage during processing

**Solutions**:
1. **Enable Embedding Caching**:
   ```csharp
   var options = new SemanticChunkingOptions
   {
       EnableEmbeddingCaching = true,
       EmbeddingCacheSize = 1000 // Adjust based on memory constraints
   };
   ```

2. **Optimize Chunk Size**:
   ```csharp
   var options = new SemanticChunkingOptions
   {
       MaxTokensPerChunk = 512, // Start with 512, adjust based on performance
       BufferSize = 1 // Smaller buffer for faster processing
   };
   ```

3. **Tune Similarity Threshold**:
   ```csharp
   var options = new SemanticChunkingOptions
   {
       BreakpointPercentileThreshold = 0.75 // Lower = more splits, faster processing
   };
   ```

#### Slow MMR Computation

**Symptoms**: 
- MMR takes too long with large vector sets
- Memory allocation issues

**Solutions**:
1. **Reduce Vector Dimensions**: Use lower-dimensional embeddings if possible
2. **Limit TopK**: Don't request more results than needed
3. **Pre-filter Vectors**: Reduce candidate set before MMR computation
4. **Batch Processing**: Process vectors in smaller batches

### Token Counting Issues

#### Inaccurate Token Counts

**Problem**: Token counts don't match expected values from OpenAI models

**Solution**: Ensure you're using the correct tokenizer:
```csharp
// For GPT-4 compatibility (default)
var tokenCounter = MLTokenCounter.CreateGpt4();

// For specific models
var tokenCounter = MLTokenCounter.CreateTextEmbedding3Small();
var tokenCounter = MLTokenCounter.CreateTextEmbedding3Large();
```

#### TokenCounter Initialization Errors

**Problem**: Errors when creating MLTokenCounter

**Solution**:
```csharp
try 
{
    var tokenCounter = new MLTokenCounter();
}
catch (Exception ex)
{
    // Fallback to a simple word-based counter for development
    Console.WriteLine($"Failed to initialize ML tokenizer: {ex.Message}");
    // Implement fallback logic
}
```

### Embedding Generation Issues

#### Custom IEmbeddingGenerator Implementation

**Problem**: Custom embedding generators causing errors

**Common Issues & Solutions**:

1. **Null or Empty Embeddings**:
   ```csharp
   public async Task<Vector<double>> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
   {
       if (string.IsNullOrEmpty(text))
           throw new ArgumentException("Text cannot be null or empty", nameof(text));
       
       var embedding = await CallYourEmbeddingService(text);
       
       if (embedding == null || embedding.Count == 0)
           throw new InvalidOperationException("Embedding generation failed");
           
       return embedding;
   }
   ```

2. **Dimension Mismatch**:
   ```csharp
   // Ensure all embeddings have consistent dimensions
   private void ValidateEmbeddingDimensions(Vector<double> embedding, int expectedDimensions)
   {
       if (embedding.Count != expectedDimensions)
       {
           throw new InvalidOperationException(
               $"Embedding dimension mismatch. Expected: {expectedDimensions}, Got: {embedding.Count}");
       }
   }
   ```

3. **Rate Limiting**:
   ```csharp
   // Implement retry logic for rate-limited APIs
   public async Task<Vector<double>> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
   {
       const int maxRetries = 3;
       for (int i = 0; i < maxRetries; i++)
       {
           try
           {
               return await CallEmbeddingAPI(text);
           }
           catch (RateLimitException) when (i < maxRetries - 1)
           {
               await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, i)), cancellationToken);
           }
       }
       throw new InvalidOperationException("Max retries exceeded");
   }
   ```

### Testing Issues

#### Test Failures in Different Environments

**Problem**: Tests pass locally but fail in CI/CD

**Common Causes & Solutions**:

1. **Culture/Locale Differences**:
   ```csharp
   [Fact]
   public void Test_With_Culture_Independence()
   {
       // Use CultureInfo.InvariantCulture for numeric comparisons
       using (new CultureContext(CultureInfo.InvariantCulture))
       {
           // Your test logic
       }
   }
   ```

2. **Floating Point Precision**:
   ```csharp
   // Use appropriate precision for floating point comparisons
   result.Should().BeApproximately(expected, precision: 0.001);
   ```

3. **Random Seed Issues**:
   ```csharp
   // Use fixed seeds for deterministic tests
   private static readonly Random TestRandom = new Random(42);
   ```

### Memory and Performance Monitoring

#### High Memory Usage

**Diagnostic Steps**:
1. **Monitor Embedding Cache**: Large caches can consume significant memory
2. **Check Vector Dimensions**: Higher dimensions increase memory usage linearly
3. **Review Batch Sizes**: Large batches can cause memory spikes

**Solutions**:
```csharp
// Configure memory-conscious options
var options = new SemanticChunkingOptions
{
    EnableEmbeddingCaching = true,
    EmbeddingCacheSize = 500, // Reduce cache size
    MaxTokensPerChunk = 256   // Smaller chunks
};

// Monitor memory usage in production
GC.Collect();
var memoryBefore = GC.GetTotalMemory(false);
// Your operation
var memoryAfter = GC.GetTotalMemory(false);
Console.WriteLine($"Memory used: {memoryAfter - memoryBefore} bytes");
```

## Getting Help

### Debug Information Collection

When reporting issues, please include:

1. **Environment Info**:
   ```bash
   dotnet --info
   ```

2. **Package Versions**:
   ```bash
   dotnet list package
   ```

3. **Minimal Reproduction**:
   - Create a minimal test case that reproduces the issue
   - Include sample data and configuration

4. **Performance Information** (if relevant):
   - Vector dimensions and count
   - Chunk sizes and options
   - Timing measurements

### Support Channels

- **GitHub Issues**: [Create an issue](https://github.com/AiGeekSquad/AIContext/issues)
- **GitHub Discussions**: [Join the discussion](https://github.com/AiGeekSquad/AIContext/discussions)
- **Documentation**: [Check the wiki](https://github.com/AiGeekSquad/AIContext/wiki)

### Before Reporting Issues

1. **Check Existing Issues**: Search for similar problems
2. **Update to Latest Version**: Ensure you're using the latest release
3. **Review Documentation**: Check relevant documentation sections
4. **Test with Minimal Config**: Try with default settings first