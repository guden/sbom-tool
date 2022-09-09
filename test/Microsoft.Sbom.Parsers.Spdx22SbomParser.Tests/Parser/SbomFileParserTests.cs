﻿using Microsoft.Sbom.Contracts;
using Microsoft.Sbom.Exceptions;
using Microsoft.Sbom.Parsers.Spdx22SbomParser;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Text;
using System.Text.Json;

namespace Microsoft.Sbom.Parser;

[TestClass]
public class SbomFileParserTests
{
    [TestMethod]
    public void ParseSbomFilesTest()
    {
        byte[] bytes = Encoding.UTF8.GetBytes(JsonStrings.GoodJsonWith2FilesString);
        var stream = new MemoryStream(bytes);

        var buffer = new byte[Constants.ReadBufferSize];

        stream.Read(buffer);

        var parser1 = new SbomFileParser(stream, ref buffer);
        var result = parser1.GetSbomFile(out SBOMFile sbomFile);

        Assert.IsTrue(result != 0);
        Assert.IsNotNull(sbomFile);

        var parser2 = new SbomFileParser(stream, ref buffer, parser1.CurrentState);
        result = parser2.GetSbomFile(out SBOMFile sbomFile2);

        Assert.IsTrue(result != 0);
        Assert.IsNotNull(sbomFile2);

        var parser3 = new SbomFileParser(stream, ref buffer, parser2.CurrentState);
        result = parser3.GetSbomFile(out SBOMFile sbomFile3);

        Assert.IsTrue(result == 0);
        Assert.IsNull(sbomFile3);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void NullByteBufferThrows()
    {
        byte[] buffer = null;
        new SbomFileParser(new MemoryStream(), ref buffer);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void NullStreamThrows()
    {
        var buffer = new byte[2];
        new SbomFileParser(null, ref buffer);
    }

    [TestMethod]
    public void EmptyByteArrayReadsStreamToArrayBeforeStart()
    {
        byte[] bytes = Encoding.UTF8.GetBytes(JsonStrings.GoodJsonWith2FilesString);
        var stream = new MemoryStream(bytes);

        var buffer = new byte[Constants.ReadBufferSize];

        var parser1 = new SbomFileParser(stream, ref buffer);
        var result = parser1.GetSbomFile(out SBOMFile sbomFile);

        Assert.IsTrue(result != 0);
        Assert.IsNotNull(sbomFile);

        var parser2 = new SbomFileParser(stream, ref buffer, parser1.CurrentState);
        result = parser2.GetSbomFile(out SBOMFile sbomFile2);

        Assert.IsTrue(result != 0);
        Assert.IsNotNull(sbomFile2);

        var parser3 = new SbomFileParser(stream, ref buffer, parser2.CurrentState);
        result = parser3.GetSbomFile(out SBOMFile sbomFile3);

        Assert.IsTrue(result == 0);
        Assert.IsNull(sbomFile3);
    }

    [TestMethod]
    public void StreamClosedTestReturnsNull()
    {
        var stream = new MemoryStream();
        stream.Close();
        var buffer = new byte[Constants.ReadBufferSize];

        var parser = new SbomFileParser(stream, ref buffer);
        var result = parser.GetSbomFile(out SBOMFile file);

        Assert.IsTrue(result == 0);
        Assert.IsNull(file);
    }

    [TestMethod]
    public void StreamEmptyTestReturnsNull()
    {
        var stream = new MemoryStream();
        stream.Read(new byte[Constants.ReadBufferSize]);
        var buffer = new byte[Constants.ReadBufferSize];

        var parser = new SbomFileParser(stream, ref buffer);
        var result = parser.GetSbomFile(out SBOMFile file);

        Assert.IsTrue(result == 0);
        Assert.IsNull(file);
    }

    [DataTestMethod]
    [DataRow(JsonStrings.JsonWith1FileMissingNameString)]
    [DataRow(JsonStrings.JsonWith1FileMissingIDString)]
    [DataRow(JsonStrings.JsonWith1FileMissingChecksumsString)]
    [DataRow(JsonStrings.JsonWith1FileMissingSHA256ChecksumsString)]
    [DataRow(JsonStrings.JsonWith1FileMissingLicenseConcludedString)]
    [DataRow(JsonStrings.JsonWith1FileMissingLicenseInfoInFilesString)]
    [DataRow(JsonStrings.JsonWith1FileMissingCopyrightString)]
    [DataRow(JsonStrings.JsonWith1FileMissingCopyrightAndPathString)]
    [TestMethod]
    [ExpectedException(typeof(ParserError))]
    public void MissingPropertiesTest_Throws(string json)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(json);
        var stream = new MemoryStream(bytes);

        var buffer = new byte[Constants.ReadBufferSize];

        stream.Read(buffer);

        var parser = new SbomFileParser(stream, ref buffer);
        parser.GetSbomFile(out SBOMFile _);
    }

    [DataTestMethod]
    [DataRow(JsonStrings.GoodJsonWith1FileAdditionalObjectPropertyString)]
    [DataRow(JsonStrings.GoodJsonWith1FileAdditionalArrayPropertyString)]
    [DataRow(JsonStrings.GoodJsonWith1FileAdditionalStringPropertyString)]
    [TestMethod]
    public void IgnoresAdditionalPropertiesTest(string json)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(json);
        var stream = new MemoryStream(bytes);

        var buffer = new byte[Constants.ReadBufferSize];

        stream.Read(buffer);

        var parser = new SbomFileParser(stream, ref buffer);
        var result = parser.GetSbomFile(out SBOMFile sbomFile);

        Assert.IsTrue(result != 0);
        Assert.IsNotNull(sbomFile);
    }

    [TestMethod]
    [ExpectedException(typeof(ParserError))]
    public void MalformedJsonTest_Throws()
    {
        byte[] bytes = Encoding.UTF8.GetBytes(JsonStrings.MalformedJson);
        var stream = new MemoryStream(bytes);

        var buffer = new byte[Constants.ReadBufferSize];

        var parser = new SbomFileParser(stream, ref buffer);
        parser.GetSbomFile(out SBOMFile _);
    }

    [TestMethod]
    [ExpectedException(typeof(ParserError))]
    public void BadStateJsonTest_Throws()
    {
        byte[] bytes = Encoding.UTF8.GetBytes(JsonStrings.GoodJsonWith2FilesString);
        var stream = new MemoryStream(bytes);

        var buffer = new byte[Constants.ReadBufferSize];
        var bufferCopy = new byte[Constants.ReadBufferSize];

        stream.Read(buffer);

        Array.Copy(buffer, bufferCopy, bufferCopy.Length);

        var reader = new Utf8JsonReader(buffer, isFinalBlock: false, state: default);

        while (reader.TokenType != JsonTokenType.String)
        {
            ParserUtils.Read(stream, ref buffer, ref reader);
        }

        var parser = new SbomFileParser(stream, ref buffer, reader.CurrentState);
        parser.GetSbomFile(out SBOMFile _);
    }
}