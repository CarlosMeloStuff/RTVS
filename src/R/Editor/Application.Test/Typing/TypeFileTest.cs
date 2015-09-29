﻿using System.Diagnostics.CodeAnalysis;
using Microsoft.Languages.Core.Test.Utility;
using Microsoft.R.Editor.Application.Test.TestShell;
using Microsoft.R.Editor.ContentType;
using Microsoft.R.Support.RD.ContentTypes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.R.Editor.Application.Test.Typing
{
    [ExcludeFromCodeCoverage]
    [TestClass]
    public class TypeFileTest : UnitTestBase
    {
        //[TestMethod]
        public void TypeFile_R()
        {
            string actual = TypeFileInEditor("01.r", RContentTypeDefinition.ContentType);
            string expected =
@"library(abind)
x <- function (x, y, wt = NULL, intercept = TRUE, tolerance = 1e-07,
          yname = NULL)
{
    abind(a, )
}";
            Assert.AreEqual(expected, actual);
        }

        //[TestMethod]
        public void TypeFile_RD()
        {
            TypeFileInEditor("01.rd", RdContentTypeDefinition.ContentType);
        }

        /// <summary>
        /// Opens file in an editor window
        /// </summary>
        /// <param name="fileName">File name</param>
        private string TypeFileInEditor(string fileName, string contentType)
        {
            var script = new TestScript(contentType);
            string text = TestFiles.LoadFile(this.TestContext, fileName);

            try
            {
                script.Type(text, idleTime: 10);
                return script.EditorText;
            }
            finally
            {
                script.Close();
            }
        }
    }
}
