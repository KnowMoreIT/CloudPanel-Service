using System;
using System.Data.Linq.Mapping;

namespace CPService
{
    [Table(Name ="Companies")]
    public class Companies
    {
        [Column(IsPrimaryKey = true, IsDbGenerated = true)]
        public int CompanyId { get; set; }

        [Column]
        public bool IsReseller { get; set; }

        [Column]
        public string ResellerCode { get; set; }

        [Column]
        public string CompanyCode { get; set; }

        [Column]
        public string CompanyName { get; set; }
    }

    [Table(Name="Users")]
    public class Users
    {
        [Column(IsPrimaryKey = true, IsDbGenerated = true)]
        public int ID { get; set; }

        [Column]
        public Guid UserGuid { get; set; }

        [Column]
        public Guid ExchangeGuid { get; set; }

        [Column]
        public string CompanyCode { get; set; }

        [Column]
        public string DistinguishedName { get; set; }

        [Column]
        public string SamAccountName { get; set; }

        [Column]
        public string UserPrincipalName { get; set; }

        [Column]
        public string DisplayName { get; set; }

        [Column]
        public string Email { get; set; }

        [Column]
        public bool? IsEnabled { get; set; }

        [Column]
        public bool? IsLockedOut { get; set; }

        [Column]
        public int? MailboxPlan { get; set; }

        [Column]
        public int? ArchivePlan { get; set; }
    }

    [Table(Name ="Statistics")]
    public class Statistics
    {
        [Column(IsPrimaryKey = true, IsDbGenerated = true)]
        public int ID { get; set; }

        [Column]
        public DateTime Retrieved { get; set; }

        [Column]
        public int UserCount { get; set; }

        [Column]
        public int MailboxCount { get; set; }

        [Column]
        public int CitrixCount { get; set; }

        [Column]
        public string ResellerCode { get; set; }

        [Column]
        public string CompanyCode { get; set; }
    }

    [Table(Name ="CitrixUserToDesktopGroup")]
    public class CitrixUserToDesktopGroup
    {
        [Column(IsPrimaryKey = true)]
        public int UserRefDesktopGroupId { get; set; }

        [Column(IsPrimaryKey = true)]
        public int DesktopGroupRefUserId { get; set; }
    }

    [Table(Name ="StatMessageTrackingCounts")]
    public class StatMessageTrackingCounts
    {
        [Column(IsPrimaryKey = true, IsDbGenerated = true)]
        public int ID { get; set; }

        [Column]
        public int UserID { get; set; }

        [Column]
        public int TotalSent { get; set; }

        [Column]
        public int TotalReceived { get; set; }

        [Column]
        public DateTime Start { get; set; }

        [Column]
        public DateTime End { get; set; }

        [Column]
        public long TotalBytesSent { get; set; }

        [Column]
        public long TotalBytesReceived { get; set; }
    }
    
    [Table(Name ="StatMailboxSizes")]
    public class StatMailboxSizes
    {
        [Column(IsPrimaryKey = true, IsDbGenerated = true)]
        public int ID { get; set; }

        [Column]
        public Guid UserGuid { get; set; }

        [Column]
        public string UserPrincipalName { get; set; }

        [Column]
        public string MailboxDatabase { get; set; }

        [Column]
        public string TotalItemSize { get; set; }

        [Column]
        public long TotalItemSizeInBytes { get; set; }

        [Column]
        public string TotalDeletedItemSize { get; set; }

        [Column]
        public long TotalDeletedItemSizeInBytes { get; set; }

        [Column]
        public int ItemCount { get; set; }

        [Column]
        public int DeletedItemCount { get; set; }

        [Column]
        public DateTime Retrieved { get; set; }
    }

    [Table(Name = "StatMailboxSizes")]
    public class StatMailboxArchiveSizes
    {
        [Column(IsPrimaryKey = true, IsDbGenerated = true)]
        public int ID { get; set; }

        [Column]
        public Guid UserGuid { get; set; }

        [Column]
        public string UserPrincipalName { get; set; }

        [Column]
        public string MailboxDatabase { get; set; }

        [Column]
        public string TotalItemSize { get; set; }

        [Column]
        public long TotalItemSizeInBytes { get; set; }

        [Column]
        public string TotalDeletedItemSize { get; set; }

        [Column]
        public long TotalDeletedItemSizeInBytes { get; set; }

        [Column]
        public int ItemCount { get; set; }

        [Column]
        public int DeletedItemCount { get; set; }

        [Column]
        public DateTime Retrieved { get; set; }
    }

    [Table(Name ="StatMailboxDatabaseSizes")]
    public class StatMailboxDatabaseSizes
    {
        [Column(IsPrimaryKey = true, IsDbGenerated = true)]
        public int ID { get; set; }

        [Column]
        public string DatabaseName { get; set; }

        [Column]
        public string Server { get; set; }

        [Column]
        public string DatabaseSize { get; set; }

        [Column]
        public DateTime Retrieved { get; set; }

        [Column]
        public long DatabaseSizeInBytes { get; set; }
    }
}
