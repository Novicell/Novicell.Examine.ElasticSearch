using System;

namespace Novicell.Examine.ElasticSearch.Tests
{
    public class RandomIdRAMDirectory : RAMDirectory
    {
        private readonly string _lockId = Guid.NewGuid().ToString();
        public override string GetLockId()
        {
            return _lockId;
        }
    }
}