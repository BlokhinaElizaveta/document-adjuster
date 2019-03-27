using System;
using System.IO;
using System.Net;
using System.Xml;

namespace Kontur.Recognition.Utils
{
	/// <summary>
	/// Creates xml readers with build-in protection from DoS attacks. 
	/// Read more https://msdn.microsoft.com/en-us/magazine/ee335713.aspx
	/// </summary>
	public static class XmlSecureReaderCreator
	{
		private const int MaxCharactersFromEntities = 1024;

		public static XmlReader Create(Stream xmlStream)
		{
			// allow entity parsing but do so more safely
			var settings = new XmlReaderSettings
			{
				DtdProcessing = DtdProcessing.Parse,
				MaxCharactersFromEntities = MaxCharactersFromEntities,
				XmlResolver = null
			};

			return XmlReader.Create(xmlStream, settings);
		}

		public static XmlReader Create(StreamReader streamReader)
		{
			var settings = new XmlReaderSettings
			{
				DtdProcessing = DtdProcessing.Parse,
				MaxCharactersFromEntities = MaxCharactersFromEntities,
				XmlResolver = null
			};

			return XmlReader.Create(streamReader, settings);
		}

		public static XmlReader Create(TextReader textReader)
		{
			var settings = new XmlReaderSettings
			{
				DtdProcessing = DtdProcessing.Parse,
				MaxCharactersFromEntities = MaxCharactersFromEntities,
				XmlResolver = null
			};

			return XmlReader.Create(textReader, settings);
		}

		public static XmlTextReader Create(Stream xmlStream, XmlParserContext context, XmlNodeType nodeType)
		{
			var enableNamespaceSupport = (context != null) && (context.NamespaceManager != null);

			return new XmlTextReader(xmlStream, nodeType, context)
			{
				DtdProcessing = DtdProcessing.Parse,
				XmlResolver = null,
				Namespaces = enableNamespaceSupport
			};
		}

		public static XmlTextReader Create(Stream xmlStream, XmlParserContext context, XmlNodeType nodeType, bool namespacesSupport)
		{
			return new XmlTextReader(xmlStream, nodeType, context)
			{
				DtdProcessing = DtdProcessing.Parse,
				XmlResolver = null,
				Namespaces = namespacesSupport
			};
		}

		// This code can be used to resolve external entities securely
		private class XmlSecureResolver : XmlUrlResolver
		{
			private const int TimeoutMs = 3000;
			private const int BufferSizeInBytes = 1024;
			private const int MaxResponseSizeInBytes = 1024 * 1024;

			public override object GetEntity(Uri absoluteUri, string role, Type ofObjectToReturn)
			{
				if (absoluteUri.IsLoopback)
				{
					return null;
				}

				var request = WebRequest.Create(absoluteUri);
				request.Timeout = TimeoutMs;
				var response = request.GetResponse();

				if (response == null)
				{
					throw new XmlException("Could not resolve external entity");
				}

				var responseStream = response.GetResponseStream();

				if (responseStream == null)
				{
					throw new XmlException("Could not resolve external entity");
				}

				responseStream.ReadTimeout = TimeoutMs;

				var copyStream = new MemoryStream();
				var buffer = new byte[BufferSizeInBytes];
				int bytesRead;
				var totalBytesRead = 0;

				do
				{
					bytesRead = responseStream.Read(buffer, 0, buffer.Length);
					totalBytesRead += bytesRead;
					if (totalBytesRead > MaxResponseSizeInBytes)
						throw new XmlException("Could not resolve external entity");
					copyStream.Write(buffer, 0, bytesRead);
				} while (bytesRead > 0);

				copyStream.Seek(0, SeekOrigin.Begin);
				return copyStream;
			}
		}
	}
}
