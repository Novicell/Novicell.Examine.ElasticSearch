using System.IO;
using Examine.Test;
using Examine.Test.DataServices;

namespace Novicell.Examine.ElasticSearch.Tests.DataServices
{
    public class TestDataService : IDataService
    {

        public TestDataService()
        {
            ContentService = new TestContentService();
            LogService = new TestLogService();
            MediaService = new TestMediaService();
        }

        #region IDataService Members

        public IContentService ContentService { get; private set; }

        public ILogService LogService { get; private set; }

        public IMediaService MediaService { get; private set; }

        public string MapPath(string virtualPath)
        {
            return new DirectoryInfo(TestHelper.AssemblyDirectory) + "\\" + virtualPath.Replace("/", "\\");
        }

        #endregion
    }
}