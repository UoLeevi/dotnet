using Xunit;

namespace Code.DotNet.App.Test
{
    public class TextEditorTest
    {
        #region Test data
        const string cSharpClassFileCodeSample =
@"using System;
using System.Collections.Generic;

namespace EFCoreTest.Library.Models
{
    public partial class Address
    {
        public Address()
        {
            BusinessEntityAddress = new HashSet<BusinessEntityAddress>();
            SalesOrderHeaderBillToAddress = new HashSet<SalesOrderHeader>();
            SalesOrderHeaderShipToAddress = new HashSet<SalesOrderHeader>();
        }

        public int AddressID { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string City { get; set; }
        public int StateProvinceID { get; set; }
        public string PostalCode { get; set; }
        public Guid rowguid { get; set; }
        public DateTime ModifiedDate { get; set; }

        public virtual StateProvince StateProvince { get; set; }
        public virtual ICollection<BusinessEntityAddress> BusinessEntityAddress { get; set; }
        public virtual ICollection<SalesOrderHeader> SalesOrderHeaderBillToAddress { get; set; }
        public virtual ICollection<SalesOrderHeader> SalesOrderHeaderShipToAddress { get; set; }
    }
}
";
        const string cSharpDbContextFileCodeSample =
@"using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using EFCoreTest.Library.Models;

namespace EFCoreTest.Library.Context
{
    public partial class AdventureWorks2017Context : DbContext
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
";
        #endregion

        [Theory]
        #region Expected result
        [InlineData(
@"using System;
using System.Collections.Generic;

namespace EFCoreTest.Library.Models
{
    public partial class Address
    {
        public Address()
        {
            BusinessEntityAddress = new HashSet<BusinessEntityAddress>();
            SalesOrderHeaderBillToAddress = new HashSet<SalesOrderHeader>();
            SalesOrderHeaderShipToAddress = new HashSet<SalesOrderHeader>();
        }

        public int AddressID { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string City { get; set; }
        public int StateProvinceID { get; set; }
        public string PostalCode { get; set; }
        public Guid rowguid { get; set; }
        public DateTime ModifiedDate { get; set; }

        public virtual StateProvince StateProvince { get; set; }
        public virtual ICollection<BusinessEntityAddress> BusinessEntityAddress { get; set; }
        public virtual ICollection<SalesOrderHeader> SalesOrderHeaderBillToAddress { get; set; }
        public virtual ICollection<SalesOrderHeader> SalesOrderHeaderShipToAddress { get; set; }
    }
}
")]
        #endregion
        public void ShouldReturnOriginalIfNothingIsEdited(string expectedText)
        {
            var editor = new TextEditor { Text = expectedText };

            Assert.Equal(expectedText, editor.Text);
        }

        [Theory]
        #region Expected result
        [InlineData(
@"using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Code.DotNet.App.Test;

namespace EFCoreTest.Library.Models
{
    public partial class Address
    {
        public Address()
        {
            BusinessEntityAddress = new HashSet<BusinessEntityAddress>();
            SalesOrderHeaderBillToAddress = new HashSet<SalesOrderHeader>();
            SalesOrderHeaderShipToAddress = new HashSet<SalesOrderHeader>();
        }

        public int AddressID { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string City { get; set; }
        public int StateProvinceID { get; set; }
        public string PostalCode { get; set; }
        public Guid rowguid { get; set; }
        public DateTime ModifiedDate { get; set; }

        public virtual StateProvince StateProvince { get; set; }
        public virtual ICollection<BusinessEntityAddress> BusinessEntityAddress { get; set; }
        public virtual ICollection<SalesOrderHeader> SalesOrderHeaderBillToAddress { get; set; }
        public virtual ICollection<SalesOrderHeader> SalesOrderHeaderShipToAddress { get; set; }
    }
}
")]
        #endregion
        public void CanInsertNamespacesIntoCSharpClassFileCode(string expectedText)
        {
            string[] namespaces = new []
            {
                "Microsoft.EntityFrameworkCore.ChangeTracking",
                "Code.DotNet.App.Test"
            };

            var editor = new TextEditor { Text = cSharpClassFileCodeSample };

            editor
                .MoveToStart()
                .MoveToNextEmptyLine();

            foreach (var @namespace in namespaces)
            {
                editor
                    .WriteLine($"using {@namespace};");
            }

            Assert.Equal(expectedText, editor.Text);
        }

        [Theory]
        #region Expected result
        [InlineData(
@"using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using EFCoreTest.Library.Models;

namespace EFCoreTest.Library.Context
{
    public partial class AdventureWorks2017Context : DbContext
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasChangeTrackingStrategy(ChangeTrackingStrategy.ChangingAndChangedNotifications);

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
")]
        #endregion
        public void CanInsertMethodCallIntoCSharpDbContextFileCode(string expectedText)
        {
            var editor = new TextEditor { Text = cSharpDbContextFileCodeSample }
                .MoveToPattern(@"OnModelCreatingPartial\(modelBuilder\);")
                .Write("modelBuilder.HasChangeTrackingStrategy(ChangeTrackingStrategy.ChangingAndChangedNotifications);\r\n\r\n            ");

            Assert.Equal(expectedText, editor.Text);
        }
    }
}
