namespace MyORMInMemory.Tests 
{
    
    public class ConnectionAndDDLTests
    {
        private Classes.InMemoryContext Context { get; }

        private const string _schema = "orm_pg_test";

        private const string _datname = "orm_pg_test";

        public ConnectionAndDDLTests()
        {
            Context = new Classes.InMemoryContext();                        
            
        }

        [Fact]
        public void TestConnection()
        {
            Context.TestConnection();
        }

        [Fact]
        public void CreateDataBase()
        {           

            Context.CreateDataBase();

            Context.DropDataBase();


        }

        [Fact]
        public void DropDataBase()
        {
            Context.DropDataBase();
        }

        [Fact]
        public void CreateColumns()
        {

            Context.CreateDataBase();

            Context.UpdateDataBase();

            Context.DropDataBase();

        }



        [Fact]
        public void FitColumns()
        {
            Context.CreateDataBase();

            Context.UpdateDataBase();

            Context.DropDataBase();

        }
    }

}
