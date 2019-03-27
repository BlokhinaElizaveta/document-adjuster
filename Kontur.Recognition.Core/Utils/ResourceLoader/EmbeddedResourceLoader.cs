using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Kontur.Recognition.Utils.JetBrains.Annotations;

namespace Kontur.Recognition.Utils.ResourceLoader
{
	/// <summary>
	/// Class to provide access to resources embedded into assembly
	/// </summary>
	public class EmbeddedResourceLoader : IResourceLoader
	{
		[NotNull]
		private readonly string defaultNamespace;

		[NotNull]
		private readonly Assembly defaultAssembly;

		[NotNull]
		private readonly ConcurrentDictionary<string, byte[]> cache = new ConcurrentDictionary<string, byte[]>();

		/// <summary>
		/// Constructs loader for embedded resources. Given root type is used as a root for relative paths. Assembly is chosen by given root type.
		/// </summary>
		/// <param name="defaultAssembly">The assembly to use to locate resources.</param>
		/// <param name="defaultNamespace">The namespace from which recource relative paths should be traversed</param>
		/// <param name="relativePath">The path to resources</param>
		public EmbeddedResourceLoader([NotNull] Assembly defaultAssembly, [CanBeNull] string relativePath = null, [CanBeNull] string defaultNamespace = null)
		{
			this.defaultNamespace = TraversePath(defaultNamespace, relativePath);
			this.defaultAssembly = defaultAssembly;
		}

		/// <summary>
		/// Constructs loader for embedded resources. Given root type is used as a root for relative paths. Assembly is chosen by given root type.
		/// </summary>
		/// <param name="rootType">The type to use as a root of relative paths</param>
		/// <param name="relativePath">The path to resources</param>
		public EmbeddedResourceLoader([NotNull] Type rootType, [CanBeNull] string relativePath = null)
			: this(rootType.Assembly, relativePath, rootType.Namespace)
		{
		}

		/// <summary>
		/// Constructs loader for embedded resources. It is assumed that this constructor gets called from class which in turn determines the assembly to use.
		/// </summary>
		/// <param name="relativePath">The path to resources</param>
		public EmbeddedResourceLoader([CanBeNull] string relativePath = null)
			: this(Assembly.GetCallingAssembly(), relativePath)
		{
		}


		[NotNull]
		private static string TraversePath([CanBeNull] string rootNamespace, [CanBeNull] string relativePath)
		{
			rootNamespace = rootNamespace ?? "";

			if (relativePath == null) 
				return rootNamespace;

			var namespaceParts = rootNamespace.Contains('.') ? rootNamespace.Split('.').ToList() : new List<string>();
			foreach (var part in relativePath.Split('\\', '/'))
			{
				if (part == "..")
				{
					namespaceParts.RemoveAt(namespaceParts.Count - 1);
				}
				else if ((part == ".") || string.IsNullOrEmpty(part))
				{
					// just do nothing
				}
				else
				{
					namespaceParts.Add(part);
				}
			}
			var buffer = new StringBuilder(rootNamespace.Length + relativePath.Length);
			foreach (var part in namespaceParts)
			{
				buffer.Append(part).Append(".");
			}
			buffer.Length--;
			return buffer.ToString();
		}

		private string ComposeRelativePath(string resourceRelativePath)
		{
			return TraversePath(defaultNamespace, resourceRelativePath);
		}

		private byte[] LookupCache(string name)
		{
			byte[] result;
			return cache.TryGetValue(name, out result) ? result : null;
		}

		private void UpdateCache(string name, byte [] entry)
		{
			cache.AddOrUpdate(name, s => entry, (s, bytes) => entry);
		}

		private string BuildupCacheKey(Assembly assembly, string resourcePath)
		{
			var assemblyName = assembly.FullName;
			var result = new StringBuilder(assemblyName.Length + resourcePath.Length + 2);
			result.Append(assemblyName).Append("::").Append(resourcePath);
			return result.ToString();
		}

		/// <summary>
		/// Retrieves given resource as a byte array. In case resource is missing the null is returned
		/// </summary>
		/// <param name="referenceObjectType">The type which specifies assembly to use 
		/// for resource loading and namespace to use as a starting point for resource relative path</param>
		/// <param name="resourceRelativePath">Relative path to the resource</param>
		/// <param name="bypassCache">If true, then content will not be stored in cache</param>
		/// <returns></returns>
		[CanBeNull]
		public byte[] LoadResource([NotNull] Type referenceObjectType, string resourceRelativePath, bool bypassCache = false)
		{
			var referenceNamespace = referenceObjectType.Namespace;
			var referenceAssembly = referenceObjectType.Assembly;
			var resourcePath = TraversePath(referenceNamespace, resourceRelativePath);
			return LoadResource(referenceAssembly, resourcePath, bypassCache);
		}

		/// <summary>
		/// Retrieves given resource as a byte array. In case resource is missing the null is returned
		/// </summary>
		/// <param name="name">Name of the resource</param>
		/// <param name="bypassCache">If true, then content will not be stored in cache</param>
		/// <returns></returns>
		[CanBeNull]
		public byte[] LoadResource(string name, bool bypassCache = false)
		{
			return LoadResource(defaultAssembly, ComposeRelativePath(name), bypassCache);
		}

		/// <summary>
		/// Retrieves given resource as a stream. In case resource is missing the null is returned
		/// </summary>
		/// <param name="referenceObjectType">The type which specifies assembly to use 
		/// for resource loading and namespace to use as a starting point for resource relative path</param>
		/// <param name="resourceRelativePath">Relative path to the resource</param>
		/// <param name="bypassCache">If true, then content will not be stored in cache</param>
		/// <returns></returns>
		[CanBeNull]
		public Stream LoadResourceAsStream([NotNull] Type referenceObjectType, string resourceRelativePath, bool bypassCache = false)
		{
			var referenceNamespace = referenceObjectType.Namespace;
			var referenceAssembly = referenceObjectType.Assembly;
			var resourcePath = TraversePath(referenceNamespace, resourceRelativePath);
			return LoadResourceAsStream(referenceAssembly, resourcePath, bypassCache);
		}

		/// <summary>
		/// Retrieves given resource as a stream. In case resource is missing the null is returned
		/// </summary>
		/// <param name="name">Name of the resource</param>
		/// <param name="bypassCache">If true, then content will not be stored in cache</param>
		/// <returns></returns>
		[CanBeNull]
		public Stream LoadResourceAsStream(string name, bool bypassCache = false)
		{
			return LoadResourceAsStream(defaultAssembly, ComposeRelativePath(name), bypassCache);
		}

		[CanBeNull]
		private Stream LoadResourceAsStream([NotNull] Assembly referenceAssembly, [NotNull] string resourcePath, bool bypassCache = false)
		{
			var cacheKeyName = BuildupCacheKey(referenceAssembly, resourcePath);
			var result = LookupCache(cacheKeyName);
			if (result != null)
				return new MemoryStream(result);

			if (bypassCache)
			{
				return TryGetResourceStream(referenceAssembly, resourcePath);
			}

			var content = LoadResource(referenceAssembly, resourcePath, false);
			return content != null ? new MemoryStream(content) : null;
		}

		[CanBeNull]
		private byte[] LoadResource([NotNull] Assembly referenceAssembly, [NotNull] string resourcePath, bool bypassCache)
		{
			var cacheKeyName = BuildupCacheKey(referenceAssembly, resourcePath);
			var result = LookupCache(cacheKeyName);
			if (result != null)
				return result;

			using (var stream = TryGetResourceStream(referenceAssembly, resourcePath))
			{
				if (stream == null)
					return null;

				int predictedStreamLength;
				try
				{
					predictedStreamLength = (int)stream.Length;
				}
				catch (Exception)
				{
					predictedStreamLength = 4096;
				}
				using (var memoryStream = new MemoryStream(predictedStreamLength))
				{
					stream.CopyTo(memoryStream);
					result = memoryStream.ToArray();
					if (!bypassCache)
					{
						UpdateCache(cacheKeyName, result);
					}
					return result;
				}
			}
		}

		private Stream TryGetResourceStream([NotNull] Assembly referenceAssembly, [NotNull] string resourcePath)
		{
			try
			{
				return referenceAssembly.GetManifestResourceStream(resourcePath);
			}
			catch (Exception)
			{
				return null;
			}
		}
	}
}