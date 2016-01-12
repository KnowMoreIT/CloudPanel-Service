using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CPService.Database
{
    [Database]
    public class CloudPanelDbContext : DataContext
    {
        public CloudPanelDbContext(string connectionString) : base(connectionString)
        {

        }

        public Table<Users> Users;
        public Table<Companies> Companies;
        public Table<Statistics> Statistics;
        public Table<CitrixUserToDesktopGroup> CitrixUserToDesktopGroup;
        public Table<StatMessageTrackingCounts> StatMessageTrackingCounts;
        public Table<StatMailboxSizes> StatMailboxSizes;
        public Table<StatMailboxArchiveSizes> StatMailboxArchiveSizes;
        public Table<StatMailboxDatabaseSizes> StatMailboxDatabaseSizes;
    }
}
