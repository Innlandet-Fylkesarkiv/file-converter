using FileConverter.HelperClasses;
using iText.Kernel.Colors;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace file_converter_prog2900Tests.src
{
    [TestClass()]
    public class LoggerTests
    {
        Logger logger = Logger.Instance;
        [TestMethod()]
        public void TestSetRequesterAndConverter()
        {
            // Arrange
            var expectedRequesterAndConverter = GetExpectedRequesterAndConverter();
            Debug.WriteLine("Expected Requester and Converter: " + expectedRequesterAndConverter);
            // Act
            GlobalVariables.ParsedOptions.AcceptAll = true;
            Logger.SetRequesterAndConverter();
            // Assert
            Assert.AreEqual(expectedRequesterAndConverter, Logger.JsonRoot.Requester);
            Assert.AreEqual(expectedRequesterAndConverter, Logger.JsonRoot.Converter);
            Debug.WriteLine("Actual Requester: " + Logger.JsonRoot.Requester);
            Debug.WriteLine("Actual Converter: " + Logger.JsonRoot.Converter);
        }

        private static string GetExpectedRequesterAndConverter()
        {
            string user = Environment.UserName;
            if (OperatingSystem.IsWindows())
            {
                user = UserPrincipal.Current.DisplayName;
            }
            return user;
        }
    }
}
