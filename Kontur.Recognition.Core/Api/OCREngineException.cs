using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Kontur.Recognition.Api
{
	public class OCREngineException : Exception
	{
		// TODO: refactor to implement logic of combining error code constant with external resource ID
		public enum OCREngineErrorCode
		{
			OCREngineTemporaryUnavailable,
			InitializationError,
			InternalError,
			InputFileNotFound,
		}
		
		private readonly OCREngineErrorCode errorCode;

		public OCREngineErrorCode ErrorCode { get { return errorCode; } }

		public static OCREngineException AsTemporaryUnavailable(Exception ex)
		{
			var comException = ex as COMException;
			if (comException != null)
			{
				switch ((uint)comException.ErrorCode)
				{
					case 0x80004005: // Error communicating with ABBYY Product Licensing Service
					case 0x800706BE: // RPC call failed
					case 0x800706BA: // RPC server is unavailable
					{
						return new OCREngineException(OCREngineErrorCode.OCREngineTemporaryUnavailable, string.Format("COM:0x{0:X8}", comException.ErrorCode), ex);
					}
					case 0x80080005: // Retrieving the COM class factory for component with CLSID {100020D3-0000-1056-976E-008048D53AE3} failed
					{
						var messageText = comException.Message;
						if (messageText.Contains("100020D3-0000-1056-976E-008048D53AE3")
							|| messageText.Contains("100020D2-0000-1056-976E-008048D53AE3"))
						{
							return new OCREngineException(OCREngineErrorCode.OCREngineTemporaryUnavailable, string.Format("COM:0x{0:X8}", comException.ErrorCode), ex);
						}
						break;
					}
				}
			}

			if (ex is OutOfMemoryException)
			{
				return new OCREngineException(OCREngineErrorCode.OCREngineTemporaryUnavailable, "OutOfMemoryException", ex);
			}

			return null;
		}

		public OCREngineException(OCREngineErrorCode errorCode, params object[] messageParams)
			: base(DecodeErrorMessage(errorCode, messageParams), DetectException(messageParams))
		{
			this.errorCode = errorCode;
		}

		private static Exception DetectException(IEnumerable<object> messageParams)
		{
			return messageParams.OfType<Exception>().FirstOrDefault();
		}


		private static string DecodeErrorMessage(OCREngineErrorCode errorCode, params object [] messageParams)
		{
			string msgTemplate;
			switch (errorCode)
			{
				case OCREngineErrorCode.OCREngineTemporaryUnavailable:
				{
					msgTemplate = "Библиотека оптического распознавания временно недоступна. Код ошибки: {0}";
					break;
				}
				case OCREngineErrorCode.InitializationError:
				{
					msgTemplate = "При инициализации библиотеки оптического распознавания возникла следующая ошибка: {0}";
					break;
				}
				case OCREngineErrorCode.InternalError:
				{
					msgTemplate = "Непредвиденная ошибка библиотеки оптического распознавания: {0}";
					break;
				}
				case OCREngineErrorCode.InputFileNotFound:
				{
					msgTemplate = "Входной файл не найден: {0}";
					break;
				}
				default:
				{
					throw new ArgumentException(string.Format("Unprocessed enum member: {0}", errorCode));
				}
			}
			var paramsToUse = new object[messageParams.Length];
			for (int iIdx = 0; iIdx < paramsToUse.Length; iIdx++)
			{
				var p = messageParams[iIdx];
				if (p is Exception)
				{
					p = (p as Exception).Message;
				}
				paramsToUse[iIdx] = p;
			}

			return string.Format(msgTemplate, paramsToUse);
		}
	}
}