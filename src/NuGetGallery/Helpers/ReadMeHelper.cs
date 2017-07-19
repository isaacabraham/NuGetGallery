﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using NuGetGallery.RequestModels;
using System;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;

namespace NuGetGallery.Helpers
{
    internal static class ReadMeHelper
    {

        public const string ReadMeTypeUrl = "Url";
        public const string ReadMeTypeFile = "File";
        public const string ReadMeTypeWritten = "Written";

        /// <summary>
        /// Returns if posted package form contains a ReadMe.
        /// </summary>
        /// <param name="formData">A ReadMeRequest with the ReadMe data from the form.</param>
        /// <returns>Whether there is a ReadMe to upload.</returns>
        public static Boolean HasReadMe(ReadMeRequest formData)
        {
            switch (formData?.ReadMeType)
            {
                case ReadMeTypeUrl:
                    var readMeUrl = formData.ReadMeUrl;
                    var pattern = @"^(((ht|f)tp(s?))\:\/\/)?(www.|[a-zA-Z].)[a-zA-Z0-9\-\.]+\.(com|edu|gov|mil|net|org|biz|info|name|museum|us|ca|uk)(\:[0-9]+)*(\/($|[a-zA-Z0-9\.\,\;\?\'\\\+&amp;%\$#\=~_\-]+))*$";
                    bool isValidUrl = Regex.IsMatch(readMeUrl, pattern);
                    if (isValidUrl && !formData.ReadMeUrl.StartsWith("http://") && !formData.ReadMeUrl.StartsWith("https://"))
                    {
                        readMeUrl = "http://" + formData.ReadMeUrl;
                    } 
                    return !String.IsNullOrWhiteSpace(formData.ReadMeUrl) && Uri.IsWellFormedUriString(readMeUrl, UriKind.Absolute);
                case ReadMeTypeFile:
                    return formData.ReadMeFile != null;
                case ReadMeTypeWritten:
                    return !String.IsNullOrWhiteSpace(formData.ReadMeWritten);
                default: return false;
            }
        }

        /// <summary>
        /// Takes in a string containing a markdown file and converts it to HTML.
        /// </summary>
        /// <param name="readMe">A string containing a markdown file's contents</param>
        /// <returns>A string containing the HTML version of the markdown</returns>
        private static string ConvertMarkDownToHTML(string readMe)
        {
            return CommonMark.CommonMarkConverter.Convert(readMe);
        }

        /// <summary>
        /// Takes in a Stream representing a readme file in markdown, converts it to HTML and 
        /// returns a Stream representing the HTML version of the readme.
        /// </summary>
        /// <param name="readMeMarkdownStream">Stream containing a readMe in markdown</param>
        /// <returns>A stream with the HTML version of the readMe</returns>
        public static Stream GetReadMeHTMLStream(Stream readMeMarkdownStream)
        {
            using (var reader = new StreamReader(readMeMarkdownStream))
            {
                var readMeHtml = ConvertMarkDownToHTML(reader.ReadToEnd());
                return GetStreamFromWritten(readMeHtml);
            }
        }

        /// <summary>
        /// Takes in a ReadMeRequest with a markdown ReadMe file, converts it to HTML
        /// and returns a stream with the data.
        /// </summary>
        /// <param name="readMeRequest">The readMe type and markdown file</param>
        /// <returns>A stream representing the ReadMe.html file</returns>
        public static Stream GetReadMeHTMLStream(ReadMeRequest readMeRequest)
        {
            return GetReadMeHTMLStream(GetReadMeMarkdownStream(readMeRequest));
        }

        /// <summary>
        /// Finds the highest priority ReadMe file stream and returns it. Highest priority is an uploaded file,
        /// then a repository URL inputted via the website, then a repository URL entered through the nuspec.
        /// </summary>
        /// <param name="formData">The current package's form data submitted through the verify page</param>
        /// <param name="packageMetadata">The package metadata from the nuspec file</param>
        /// <returns>A stream with the encoded ReadMe file</returns>
        public static Stream GetReadMeMarkdownStream(ReadMeRequest formData)
        {
            switch (formData.ReadMeType)
            {
                case ReadMeTypeUrl:
                    return ReadMeUrlToStream(formData.ReadMeUrl);
                case ReadMeTypeFile:
                    return formData.ReadMeFile.InputStream;
                case ReadMeTypeWritten:
                    return GetStreamFromWritten(formData.ReadMeWritten);
                default:
                    throw new InvalidOperationException('Form data contains an invalid ReadMeType.');
            }
        }

        /// <summary>
        /// Converts a ReadMe's url to a file stream.
        /// </summary>
        /// <param name="readMeUrl">A link to the raw ReadMe.md file</param>
        /// <returns>A stream to allow the file to be read</returns>
        private static Stream ReadMeUrlToStream(string readMeUrl)
        {
            if (!readMeUrl.StartsWith("http://") && !readMeUrl.StartsWith("https://"))
            {
                readMeUrl = "http://" + readMeUrl;
            }
            var webRequest = WebRequest.Create(readMeUrl);
            var response = webRequest.GetResponse();
            return response.GetResponseStream();
        }

        private static Stream GetStreamFromWritten(string writtenText)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(writtenText);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }
        
    }
}