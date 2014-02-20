﻿using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UPnP.Infrastructure.Utils.HTTP;

namespace Tests.UPnP
{
  [TestClass]
  public class HttpHeaders
  {
    [TestMethod]
    public void ParseHttpRequestHeaders()
    {
      // Typical http headers, taken TMP forum search
      List<string> requestHeaders = new List<string>
      {
        "POST http://forum.team-mediaportal.com/search/search HTTP/1.1",
        "Host: forum.team-mediaportal.com",
        "User-Agent: Mozilla/5.0 (Windows NT 6.3; WOW64; rv:27.0) Gecko/20100101 Firefox/27.0",
        "Accept: text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8",
        "Accept-Language: de-de,de;q=0.8,en-us;q=0.5,en;q=0.3",
        "Accept-Encoding: gzip, deflate",
        "Referer: http://forum.team-mediaportal.com/threads/error-parsing-http-headers.124716/",
        "Cookie: xf_session=1234567; xf_user=abcdefghi",
        "Connection: keep-alive",
        "Content-Type: application/x-www-form-urlencoded",
        "Content-Length: 115",
        "",
        "keywords=header&users=&date=&nodes%5B%5D=532&_xfToken=48495%2C1392921388%2Cf813dbe3c466d430b06d11bd9bddbb5f8b6cf644",
      };

      // Construct header using different line break styles
      IEnumerable<string> delimiters = new[] { "\r\n", "\n" };
      foreach (string delimiter in delimiters)
      {
        string fullRequest = string.Join(delimiter, requestHeaders.ToArray());

        using (Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(fullRequest)))
        {
          SimpleHTTPRequest result;
          SimpleHTTPRequest.Parse(stream, out result);
          for (int index = 1; index < requestHeaders.Count - 2; index++)
          {
            string requestHeader = requestHeaders[index];
            int pos = requestHeader.IndexOf(':');
            string header = requestHeader.Substring(0, pos).ToUpperInvariant();
            string value = requestHeader.Substring(pos + 1).Trim();
            Assert.AreEqual(value, result[header]);
          }
        }
      }
    }
  }
}
