using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using Touryo.Infrastructure.Public.Util;

namespace WSServer_sample.Tests
{
    /// <summary>テスト全体の前処理</summary>
    [SetUpFixture]
    public class TestSetup
    {
        /// <summary>Web の appsettings.json（テストの出力へ写る）をライブラリに渡す。Startup と同じ。</summary>
        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            GetConfigParameter.InitConfiguration(
                new ConfigurationBuilder()
                    .SetBasePath(TestContext.CurrentContext.TestDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build());
        }
    }
}
