﻿using System;
using System.Collections.Generic;
using NUnit.Framework;
using SmartFormat.Core.Formatting;
using SmartFormat.Core.Output;
using SmartFormat.Core.Settings;
using SmartFormat.Tests.TestUtils;
using SmartFormat.Utilities;

namespace SmartFormat.Tests.Core
{
    [TestFixture]
    public class FormatterTests
    {
        private object[] errorArgs = new object[]{ new FormatDelegate(format => { throw new Exception("ERROR!"); } ) };

        [Test]
        public void Formatter_Throws_Exceptions()
        {
            var formatter = Smart.CreateDefaultSmartFormat();
            formatter.Settings.FormatErrorAction = ErrorAction.ThrowError;

            Assert.Throws<FormattingException>(() => formatter.Test("--{0}--", errorArgs, "--ERROR!--ERROR!--"));
        }

        [Test]
        public void Formatter_Outputs_Exceptions()
        {
            var formatter = Smart.CreateDefaultSmartFormat();
            formatter.Settings.FormatErrorAction = ErrorAction.OutputErrorInResult;

            formatter.Test("--{0}--{0:ZZZZ}--", errorArgs, "--ERROR!--ERROR!--");
        }

        [Test]
        public void Formatter_Ignores_Exceptions()
        {
            var formatter = Smart.CreateDefaultSmartFormat();
            formatter.Settings.FormatErrorAction = ErrorAction.Ignore;

            formatter.Test("--{0}--{0:ZZZZ}--", errorArgs, "------");
        }

        [Test]
        public void Formatter_Maintains_Tokens()
        {
            var formatter = Smart.CreateDefaultSmartFormat();
            formatter.Settings.FormatErrorAction = ErrorAction.MaintainTokens;

            formatter.Test("--{0}--{0:ZZZZ}--", errorArgs, "--{0}--{0:ZZZZ}--");
        }

        [Test]
        public void Formatter_Maintains_Object_Tokens()
        {
            var formatter = Smart.CreateDefaultSmartFormat();
            formatter.Settings.FormatErrorAction = ErrorAction.MaintainTokens;
            formatter.Test("--{Object.Thing}--", errorArgs, "--{Object.Thing}--");
        }

        [Test]
        public void Formatter_AlignNull()
        {
            string? name = null;
            var obj = new { name = name };
            var str2 = Smart.Format("Name: {name,-10}| Column 2", obj);
            Assert.That(str2, Is.EqualTo("Name:           | Column 2"));
        }

        [Test]
        public void Nested_Placeholders_Braces()
        {
            var data = new {Person = new {FirstName = "John", LastName = "Long"}, Address = new {City = "London"}};
            var formatter = Smart.CreateDefaultSmartFormat();
            
            // This is necessary to avoid undesired trailing blanks:
            // }}} are now considered as 3 different closing braces
            formatter.Parser.UseAlternativeEscapeChar('\\');
            
            // This allows a nested template to access outer scopes.
            // Here, {City} will come from Address, but {FirstName} will come from Person:
            var result = formatter.Format("{Person:{Address:City: {City}, Name: {FirstName}}}", data);
            
            Assert.That(result, Is.EqualTo("City: London, Name: John"));
        }

        [TestCase(1)]
        [TestCase(2)]
        public void Nested_PlaceHolders_Conditional(int numOfPeople)
        {

            var data = numOfPeople == 1
                ? new {People = new List<object> {new {Name = "Name 1", Age = 20}}}
                : new {People = new List<object> {new {Name = "Name 1", Age = 20}, new {Name = "Name 2", Age = 30}}};
            var formatter = Smart.CreateDefaultSmartFormat();
            
            var result = formatter.Format("There {People.Count:is a person.|are {} people.}", data);
            
            Assert.That(result, numOfPeople == 1 ? Is.EqualTo("There is a person.") : Is.EqualTo("There are 2 people."));
        }

        [Test]
        public void Formatter_NotifyFormattingError()
        {
            var obj = new { Name = "some name" };
            var badPlaceholder = new List<string>();

            var formatter = Smart.CreateDefaultSmartFormat();
            formatter.Settings.FormatErrorAction = ErrorAction.Ignore;
            formatter.OnFormattingFailure += (o, args) => badPlaceholder.Add(args.Placeholder);
            var res = formatter.Format("{NoName} {Name} {OtherMissing}", obj);
            Assert.That(badPlaceholder.Count == 2 && badPlaceholder[0] == "{NoName}" && badPlaceholder[1] == "{OtherMissing}");
        }

        [Test]
        public void LeadingBackslashMustNotEscapeBraces()
        {
            var smart = Smart.CreateDefaultSmartFormat();
            smart.Settings.ConvertCharacterStringLiterals = false;

            var expected = "\\Hello";
            var actual = smart.Format("\\{Test}", new { Test = "Hello" });
            Assert.AreEqual(expected, actual);

            smart.Settings.ConvertCharacterStringLiterals = true;

            expected = @"\Hello";
            actual = smart.Format(@"\\{Test}", new { Test = "Hello" }); // double backslash means escaping the backslash
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void NullAndBoxedNullBehaveTheSame()
        {
            // see issue https://github.com/scottrippey/SmartFormat.NET/issues/101
            var smart = Smart.CreateDefaultSmartFormat();
            object? boxedNull = null;
            Assert.AreEqual(smart.Format("{0}", default(object)!), smart.Format("{0}", boxedNull!));
        }

        [Test]
        public void SmartFormatter_FormatDetails()
        {
            var args = new object[] {new Dictionary<string, string> {{"Greeting", "Hello"}} };
            var format = "{Greeting}";
            var output = new StringOutput();
            var formatter = new SmartFormatter();
            formatter.Settings.CaseSensitivity = CaseSensitivityType.CaseInsensitive;
            formatter.Settings.ConvertCharacterStringLiterals = true;
            formatter.Settings.FormatErrorAction = ErrorAction.OutputErrorInResult;
            formatter.Settings.ParseErrorAction = ErrorAction.OutputErrorInResult;
            formatter.Parser.AddAlphanumericSelectors(); // required for this test
            var formatParsed = formatter.Parser.ParseFormat(format, new []{string.Empty});
            var formatDetails = new FormatDetails(formatter, formatParsed, args, null, null, output);
            
            Assert.AreEqual(args, formatDetails.OriginalArgs);
            Assert.AreEqual(format, formatDetails.OriginalFormat.RawText);
            Assert.AreEqual(formatter.Settings, formatDetails.Settings);
            Assert.IsTrue(formatDetails.FormatCache == null);
        }
    }
}