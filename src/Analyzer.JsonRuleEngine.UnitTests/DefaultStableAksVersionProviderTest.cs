// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.Operators;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Templates.Analyzer.RuleEngines.JsonEngine.UnitTests
{
    [TestClass]
    public class DefaultStableAksVersionProviderTests
    {
        [TestMethod]
        public void Instance_WhenAccessed_ReturnsSameInstanceAlways()
        {
            // Test that Instance property returns the same singleton instance
            var instance1 = DefaultStableAksVersionProvider.Instance;
            var instance2 = DefaultStableAksVersionProvider.Instance;
            
            Assert.IsNotNull(instance1);
            Assert.AreSame(instance1, instance2);
        }

        [TestMethod]
        public void Instance_WhenAccessedMultipleTimes_IsSingleton()
        {
            // Test singleton behavior across multiple accesses
            var instances = new List<DefaultStableAksVersionProvider>();
            
            for (int i = 0; i < 5; i++)
            {
                instances.Add(DefaultStableAksVersionProvider.Instance);
            }
            
            // All instances should be the same reference
            var firstInstance = instances.First();
            Assert.IsTrue(instances.All(instance => ReferenceEquals(instance, firstInstance)));
        }

        [TestMethod]
        public void TryGetStableVersions_WithNormalizedLocation_ReturnsConsistentResults()
        {
            // This is an integration test that will actually call the API
            // It tests the caching behavior and normalization
            var provider = DefaultStableAksVersionProvider.Instance;
            
            // First call - should initialize cache
            var result1 = provider.TryGetStableVersions("eastus", out var versions1);
            
            // Second call - should use cached data
            var result2 = provider.TryGetStableVersions("eastus", out var versions2);
            
            // Results should be consistent
            Assert.AreEqual(result1, result2);
            
            if (result1)
            {
                Assert.IsNotNull(versions1);
                Assert.IsNotNull(versions2);
                Assert.AreEqual(versions1.Count, versions2.Count);
            }
        }

        [TestMethod]
        public void TryGetStableVersions_WithDifferentCasing_NormalizesCorrectly()
        {
            var provider = DefaultStableAksVersionProvider.Instance;
            
            // Test case normalization
            var result1 = provider.TryGetStableVersions("eastus", out var versions1);
            var result2 = provider.TryGetStableVersions("EASTUS", out var versions2);
            var result3 = provider.TryGetStableVersions("EastUS", out var versions3);
            
            // All should return the same result due to normalization
            Assert.AreEqual(result1, result2);
            Assert.AreEqual(result2, result3);
            
            if (result1 && result2 && result3)
            {
                Assert.AreEqual(versions1.Count, versions2.Count);
                Assert.AreEqual(versions2.Count, versions3.Count);
            }
        }

        [TestMethod]
        public void TryGetStableVersions_WithSpacesInLocation_NormalizesCorrectly()
        {
            var provider = DefaultStableAksVersionProvider.Instance;
            
            // Test space normalization
            var result1 = provider.TryGetStableVersions("westeurope", out var versions1);
            var result2 = provider.TryGetStableVersions("west europe", out var versions2);
            var result3 = provider.TryGetStableVersions("West Europe", out var versions3);
            
            // All should return the same result due to normalization
            Assert.AreEqual(result1, result2);
            Assert.AreEqual(result2, result3);
            
            if (result1 && result2 && result3)
            {
                Assert.AreEqual(versions1.Count, versions2.Count);
                Assert.AreEqual(versions2.Count, versions3.Count);
            }
        }

        [TestMethod]
        public void TryGetStableVersions_WithUnknownLocation_ReturnsFalse()
        {
            var provider = DefaultStableAksVersionProvider.Instance;
            
            // Test with clearly non-existent regions
            var result1 = provider.TryGetStableVersions("nonexistentregion", out var versions1);
            var result2 = provider.TryGetStableVersions("mars-central", out var versions2);
            var result3 = provider.TryGetStableVersions("atlantis-south", out var versions3);
            
            Assert.IsFalse(result1);
            Assert.IsFalse(result2);
            Assert.IsFalse(result3);
            Assert.IsNull(versions1);
            Assert.IsNull(versions2);
            Assert.IsNull(versions3);
        }

        [TestMethod]
        public void TryGetStableVersions_WithNullLocation_ReturnsFalse()
        {
            var provider = DefaultStableAksVersionProvider.Instance;
            
            var result = provider.TryGetStableVersions(null, out var versions);
            
            Assert.IsFalse(result);
            Assert.IsNull(versions);
        }

        [TestMethod]
        public void TryGetStableVersions_WithEmptyLocation_ReturnsFalse()
        {
            var provider = DefaultStableAksVersionProvider.Instance;
            
            var result1 = provider.TryGetStableVersions("", out var versions1);
            var result2 = provider.TryGetStableVersions("   ", out var versions2);
            
            Assert.IsFalse(result1);
            Assert.IsFalse(result2);
            Assert.IsNull(versions1);
            Assert.IsNull(versions2);
        }

        [TestMethod]
        public void TryGetStableVersions_CachingBehavior_OnlyInitializesOnce()
        {
            var provider = DefaultStableAksVersionProvider.Instance;
            
            // Multiple calls should use the same cached data
            var startTime = DateTime.UtcNow;
            
            // First call
            provider.TryGetStableVersions("eastus", out _);
            var firstCallTime = DateTime.UtcNow - startTime;
            
            startTime = DateTime.UtcNow;
            
            // Second call should be much faster (cached)
            provider.TryGetStableVersions("westus", out _);
            var secondCallTime = DateTime.UtcNow - startTime;
            
            // The second call should be significantly faster than the first
            // (this is a heuristic test - actual timing may vary)
            Assert.IsTrue(secondCallTime < firstCallTime || secondCallTime.TotalMilliseconds < 100,
                $"Second call ({secondCallTime.TotalMilliseconds}ms) should be faster than first call ({firstCallTime.TotalMilliseconds}ms)");
        }

        [TestMethod]
        public void TryGetStableVersions_WhenDataExists_ReturnsValidVersionSets()
        {
            var provider = DefaultStableAksVersionProvider.Instance;
            
            // Test with a known region (assuming eastus exists)
            var result = provider.TryGetStableVersions("eastus", out var versions);
            
            if (result)
            {
                Assert.IsNotNull(versions);
                Assert.IsTrue(versions.Count > 0);
                
                // Verify versions are in expected format (semantic versioning)
                foreach (var version in versions)
                {
                    Assert.IsNotNull(version);
                    Assert.IsTrue(version.Contains("."), $"Version {version} should contain dots");
                    
                    // Basic format check - should start with number
                    Assert.IsTrue(char.IsDigit(version[0]), $"Version {version} should start with a digit");
                }
            }
        }

        [TestMethod]
        public void TryGetStableVersions_ThreadSafety_HandlesMultipleSimultaneousRequests()
        {
            var provider = DefaultStableAksVersionProvider.Instance;
            var results = new bool[10];
            var versionCounts = new int[10];
            var exceptions = new List<Exception>();
            
            // Test thread safety with parallel requests
            System.Threading.Tasks.Parallel.For(0, 10, i =>
            {
                try
                {
                    results[i] = provider.TryGetStableVersions("eastus", out var versions);
                    versionCounts[i] = versions?.Count ?? 0;
                }
                catch (Exception ex)
                {
                    lock (exceptions)
                    {
                        exceptions.Add(ex);
                    }
                }
            });
            
            // Should not have any exceptions
            Assert.AreEqual(0, exceptions.Count, $"Thread safety test failed with exceptions: {string.Join(", ", exceptions.Select(e => e.Message))}");
            
            // All calls should return the same result
            if (results.Any(r => r))
            {
                Assert.IsTrue(results.All(r => r == results[0]), "All parallel calls should return the same result");
                Assert.IsTrue(versionCounts.All(c => c == versionCounts[0]), "All parallel calls should return the same version count");
            }
        }

        [TestMethod]
        public void NormalizeRegionName_Integration_WorksThroughPublicInterface()
        {
            var provider = DefaultStableAksVersionProvider.Instance;
            
            // Test that normalization works through the public interface
            // This indirectly tests the private NormalizeRegionName method
            var testCases = new[]
            {
                ("East US", "eastus"),
                ("WEST EUROPE", "westeurope"),
                ("Central India", "centralindia"),
                ("  North Central US  ", "northcentralus")
            };
            
            foreach (var (input, expected) in testCases)
            {
                var result1 = provider.TryGetStableVersions(input, out var versions1);
                var result2 = provider.TryGetStableVersions(expected, out var versions2);
                
                // Both should return the same result
                Assert.AreEqual(result1, result2, $"Results should be the same for '{input}' and '{expected}'");
                
                if (result1 && result2)
                {
                    Assert.AreEqual(versions1.Count, versions2.Count, 
                        $"Version counts should be the same for '{input}' and '{expected}'");
                }
            }
        }
    }
}