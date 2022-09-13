using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MyORMInMemory.Tests.Classes;
using MyORM.Linq;

namespace MyORMInMemory.Tests
{
    
    public class CollectionMethodsAndDMLTests
    {
        private Classes.InMemoryContext Context { get; }       

        public CollectionMethodsAndDMLTests()
        {
            Context = new Classes.InMemoryContext();

            Context.DropDataBase();

        }


        [Fact]
        public void TestAddRow()
        {
            _CreateAndUpdate();

            Departament control = new Departament();

            control.Name = "Test";
            
            Context.Departaments.Add(control);

            Assert.Equal(1, Context.Departaments.Count());

            Departament result = Context.Departaments.ToList().First();

            Assert.Equal(control.Name, result.Name);

            _Drop();

        }

        [Fact]
        public void TestUpdateRow()
        {
            _CreateAndUpdate();

            Seller control = _GetSeller();

            Context.Sellers.Add(control);

            Seller result = Context.Sellers.First();

            result.Name = "Changed";

            Context.Sellers.Update(result);

            result = Context.Sellers.First();

            Assert.Equal("Changed", result.Name);            

            _Drop();

        }


        [Fact]
        public void TestUpdateChainedRow()
        {
            _CreateAndUpdate();

            Seller control = _GetSeller();

            Context.Sellers.Add(control);

            Seller result = Context.Sellers
                .Join(s => s.Departament).
                ToList().First();

            result.Departament.Name = "Changed";

            Context.Sellers.Update(result);

            result = Context.Sellers
                .Join(s => s.Departament).ToList().First();

            Assert.Equal("Changed", result.Departament.Name);

            Departament departament = Context.Departaments.Where(s => s.Id == result.DepartamentId).First();

            Assert.Equal("Changed", departament.Name);

            _Drop();

        }



        [Fact]
        public void TestChainRows()
        {
            _CreateAndUpdate();

            Seller control = _GetSeller();

            Context.Sellers.Add(control);

            Assert.Single(Context.Departaments.ToList());
            Assert.Equal(2, Context.Products.ToList().Count());
            Assert.Equal(2, Context.Sales.ToList().Count());

            Departament departamentResult = Context.Departaments.ToList().First();

            Assert.Equal(control.Departament.Name, departamentResult.Name);
            Assert.Equal(2, control.Sales.Count());
            Assert.Equal(2, control.Sales.Select(s => s.Product).Count());

            List<Product> products = Context.Products.ToList();
#pragma warning disable
            Assert.True(products.Any(s => s.Name == control.Sales[0].Product.Name));
            Assert.True(products.Any(s => s.Name == control.Sales[1].Product.Name));
#pragma warning restore

            Seller result = Context.Sellers.Join(s => s.Sales).ToList().First();

            Assert.Equal(2, result.Sales.Count());

            Assert.Equal(2, result.Phones.Count());

            _Drop();

        }


        [Fact]
        public void TestUpdateChainedObjects()
        {
            _CreateAndUpdate();

            Seller control = _GetSeller();

            Context.Sellers.Add(control);

            int initPhonesCount = control.Phones.Count;
            int initSalesCount = control.Sales.Count;
            int initProductsCount = Context.Products.Count();

            Seller result = Context.Sellers.Join(s => s.Sales).ToList().First();

            result.Phones.Add("98 8745 6541");
            result.Sales.Add(new Sale
            {
                Product = new Product 
                {
                    Name = "new Product", 
                    Value = 50
                }
            });


            Context.Sellers.Update(result);

            result = Context.Sellers.Join(s => s.Sales).ToList().First();

            Assert.Equal(initPhonesCount + 1, result.Phones.Count);
            Assert.Equal(initSalesCount + 1, result.Sales.Count);
            Assert.Equal(initProductsCount + 1, Context.Products.Count());

            _Drop();

        }


        private void _Drop() => Context.DropDataBase();

        private void _CreateAndUpdate()
        {
            Context.DropDataBase();

            Context.UpdateDataBase();
        }

        private Seller _GetSeller()
        {
            Seller control = new Seller();

            control.Phones = new List<string>
            {
             "12 1234 5678",
             "23 4567 8910"
            };

            control.Name = "SellerName";
            control.Departament = new Departament()
            {
                Name = "Test"
            };
            control.Sales = new List<Sale>
            {
                new Sale
                {
                    Product = new Product
                    {
                        Name = "Procuct",
                        Value = 10.5
                    }
                },
                new Sale
                {
                    Product = new Product{
                        Name = "Product 2",
                        Value = 12.5
                    }
                }
            };

            return control;
        }
    }
}
