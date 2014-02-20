#region Copyright (C) 2007-2014 Team MediaPortal

/*
    Copyright (C) 2007-2014 Team MediaPortal
    http://www.team-mediaportal.com

    This file is part of MediaPortal 2

    MediaPortal 2 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    MediaPortal 2 is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with MediaPortal 2. If not, see <http://www.gnu.org/licenses/>.
*/

#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using InvalidDataException = MediaPortal.Utilities.Exceptions.InvalidDataException;

namespace UPnP.Infrastructure.Utils.HTTP
{
  /// <summary>
  /// Encapsulates an abstract "simple" HTTP message, which will be subclassed into <see cref="SimpleHTTPRequest"/> and
  /// <see cref="SimpleHTTPResponse"/>.
  /// </summary>
  /// <remarks>
  /// This class can send and receive simple HTTP messages, i.e. it can handle all kinds of headers, can send
  /// a message body, but only supports reading a message body from a stream when either the CONTENT-LENGTH header
  /// is set or the stream is closed by the other side after the body content.
  /// </remarks>
  public abstract class SimpleHTTPMessage
  {
    public const string DEFAULT_HTTP_VERSION = "HTTP/1.1";
    public const char CR = '\r';
    public const char LF = '\n';
    static readonly string CRLF = string.Format("{0}{1}", CR, LF);

    const int HEADER_BUF_INC = 2048;
    const int BODY_BUFF_INC = 4096;

    protected string _httpVersion = DEFAULT_HTTP_VERSION;
    protected IDictionary<string, string> _headers = new Dictionary<string, string>();
    protected byte[] _bodyBuffer = null;

    public string HttpVersion
    {
      get { return _httpVersion; }
      set { _httpVersion = value; }
    }

    public byte[] MessageBody
    {
      get { return _bodyBuffer; }
      set { _bodyBuffer = value; }
    }

    public string this[string index]
    {
      get
      {
        index = index.ToUpperInvariant();
        return _headers.ContainsKey(index) ? _headers[index.ToUpperInvariant()] : null;
      }
    }

    public bool ContainsHeader(string key)
    {
      return _headers.ContainsKey(key.ToUpperInvariant());
    }

    public void SetHeader(string key, string value)
    {
      _headers[key.ToUpperInvariant()] = value;
    }

    public byte[] Encode()
    {
      StringBuilder builder = new StringBuilder(1024);
      builder.Append(EncodeStartingLine());
      builder.Append(CRLF);
      AddEncodedHeaders(builder);
      builder.Append(CRLF);
      return EncodeHeaderAndBody(builder.ToString(), _bodyBuffer);
    }

    /// <summary>
    /// Encodes the starting line of this outgoing HTTP message. Has to be implemented by sub classes.
    /// </summary>
    /// <returns>Starting line of this outgoing HTTP message, for example "HTTP/1.1 200 OK" for a HTTP response.
    /// Must not contain the line end.</returns>
    protected abstract string EncodeStartingLine();

    /// <summary>
    /// Given the string <paramref name="builder"/> which already contains the starting line of the outgoing
    /// http message, this method adds the part containing the HTTP headers of this instance.
    /// </summary>
    protected void AddEncodedHeaders(StringBuilder builder)
    {
      foreach (KeyValuePair<string, string> line in _headers)
      {
        builder.Append(line.Key);
        builder.Append(": ");
        builder.Append(line.Value);
        builder.Append(CRLF);
      }
    }

    /// <summary>
    /// Given the <paramref name="header"/> which is already build up containing the http header of this
    /// instance, this method encodes that header and adds the the specified <paramref name="messageBody"/> byte array
    /// to the resulting byte array.
    /// </summary>
    /// <returns>Byte array containing the full HTTP message data to be sent.</returns>
    protected static byte[] EncodeHeaderAndBody(string header, byte[] messageBody)
    {
      byte[] headerData = UPnPConsts.UTF8_NO_BOM.GetBytes(header);
      if (messageBody == null || messageBody.Length == 0)
        return headerData;

      byte[] result = new byte[headerData.Length + messageBody.Length];
      Array.Copy(headerData, result, headerData.Length);
      Array.Copy(messageBody, 0, result, 0, headerData.Length);
      return result;
    }

    protected static void IncreaseBuffer<T>(T[] buffer, uint increment)
    {
      T[] oldBuffer = buffer;
      buffer = new T[oldBuffer.Length + 2048];
      Array.Copy(oldBuffer, 0, buffer, 0, oldBuffer.Length);
    }

    /// <summary>
    /// Parses the HTTP request out of the given <paramref name="stream"/> and stores the result
    /// in this instance. The first line of the request remains to be parsed, because it is specific
    /// to special HTTP messages. Subclasses will have to parse the returned <paramref name="firstLine"/>
    /// by their own.
    /// </summary>
    /// <param name="stream">HTTP data stream to parse.</param>
    /// <param name="firstLine">Returns the first line of the parsed HTTP request.</param>
    /// <exception cref="MediaPortal.Utilities.Exceptions.InvalidDataException">If the given <paramref name="stream"/>
    /// is malformed.</exception>
    internal void ParseHeaderAndBody(Stream stream, out string firstLine)
    {
      byte[] data = new byte[HEADER_BUF_INC];
      int numHeaderBytes = 0;
      int b;
      int delimiterLength = 3;
      while ((b = stream.ReadByte()) != -1)
      {
        if (numHeaderBytes - 1 == data.Length)
          IncreaseBuffer(data, HEADER_BUF_INC);
        data[numHeaderBytes] = (byte)b;

        // Regular CRLF line ending
        if (numHeaderBytes > 4 &&
          data[numHeaderBytes - 3] == CR && data[numHeaderBytes - 2] == LF &&
          data[numHeaderBytes - 1] == CR && data[numHeaderBytes] == LF)
          break;

        // Single LF line ending
        if (numHeaderBytes > 2 && data[numHeaderBytes - 1] == LF && data[numHeaderBytes] == LF)
        {
          delimiterLength = 1;
          break;
        }

        numHeaderBytes++;
      }

      string header = Encoding.UTF8.GetString(data, 0, numHeaderBytes - delimiterLength);
      string[] lines = header.Split(new[] { LF }, StringSplitOptions.None);
      if (lines.Length < 1)
        throw new InvalidDataException("Invalid empty HTTP header");

      int contentLength = -1;
      firstLine = lines[0].Trim();
      for (int i = 1; i < lines.Length; i++)
      {
        string line = lines[i].Trim().Trim(CR); // Also remove Carriage Return, as we splitted on Newline only
        int index = line.IndexOf(':');
        if (index == -1)
          throw new InvalidDataException("Invalid HTTP header line '{0}'", line);
        try
        {
          string key = line.Substring(0, index).Trim();
          string value = line.Substring(index + 1).Trim();
          SetHeader(key, value);
          if (key.ToUpperInvariant() == "CONTENT-LENGTH" && !int.TryParse(value, out contentLength))
            contentLength = -1;
        }
        catch (ArgumentException e)
        {
          throw new InvalidDataException("Invalid HTTP header line '{0}'", e, line);
        }
      }
      byte[] bodyBuffer;
      if (contentLength == -1)
      {
        bodyBuffer = new byte[BODY_BUFF_INC];
        contentLength = 0;
        int len;
        do
        {
          len = stream.Read(bodyBuffer, contentLength, BODY_BUFF_INC);
          contentLength += len;
          IncreaseBuffer(bodyBuffer, BODY_BUFF_INC);
        }
        while (len == BODY_BUFF_INC);
      }
      else
      {
        bodyBuffer = new byte[contentLength];
        contentLength = stream.Read(bodyBuffer, 0, contentLength);
      }
      _bodyBuffer = new byte[contentLength];
      Array.Copy(bodyBuffer, 0, _bodyBuffer, 0, contentLength);
    }

    public override string ToString()
    {
      StringBuilder sb = new StringBuilder(1024);
      sb.Append(EncodeStartingLine());
      sb.Append(CRLF);
      AddEncodedHeaders(sb);
      sb.Append(CRLF);
      return sb.ToString();
    }
  }
}
