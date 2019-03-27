using System.IO;
using System.Text;
using System.Xml.Linq;
using Kontur.Recognition.Utils.JetBrains.Annotations;

namespace Kontur.Recognition.Utils
{
	public static class XMLUtils
	{
		public static XContainer OpenXMLDocument(string path, [NotNull] Encoding encoding = null)
		{
			using (var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
			{
				return OpenXMLDocumentImpl(stream, encoding);
			}
		}

		public static XContainer OpenXMLDocument(Stream content, [NotNull] Encoding encoding = null)
		{
			return OpenXMLDocumentImpl(content, encoding);
		}

		private static XContainer OpenXMLDocumentImpl(Stream content, Encoding encoding)
		{
			// TODO: implement encoding autodetection (by XML headers)
			if (encoding == null)
			{
				var xmlReader = XmlSecureReaderCreator.Create(content);
				return XDocument.Load(xmlReader);
			}

			using (var reader = new StreamReader(content, encoding))
			{
				var xmlReader = XmlSecureReaderCreator.Create(reader);
				return XDocument.Load(xmlReader);
			}
		}
	}
}