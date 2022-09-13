# MyORMInMemory

MyORMInMemory is a implementation of MyORM in memory for unit tests of functions that use another implementations.


## Installation

.NET CLI

```bash
dotnet add package Adr.MyORMInMemory --version 3.0.0
```

Nuget package manager

```bash
PM> Install-Package Adr.MyORMInMemory -Version 3.0.0
```

packageReference

```bash
<PackageReference Include="Adr.MyORMInMemory" Version="3.0.0" />
```

## Usage

**Create a instance of InMemoryContext:**
```csharp
public class Context : InMemoryContext
    {
        
        public Context() : base() { }

        public InMemoryCollection<Item> Items { get; set; }
        public InMemoryCollection<Order> Orders { get; set; }
    }
```


**Using a instance of InMemoryContext:**
```csharp

public class OrderService 
    {
        Data.Context _context;
        public OrderService(Data.Context context)
        {
            _context = context;
        }

        public async Task Add(Order order)
        {
            await _context.Orders.AddAsync(order);
        }

        public async Task<IEnumerable<Order>> GetAll()
        {                        

            return await _context.Orders.OrderBy(d => d.Id).Join(d => d.Item).ToListAsync();
        }

        public async Task<Order?> Find(long id)
        {
            return await _context.Orders.Where(d => d.Id == id).FirstAsync();

        }

        public async Task<IEnumerable<Order>> GetFirst10()
        {
            return await _context.Orders.Take(10);

        }
}
```

**Sample of DI and IS, so we can change database easily:**
```csharp
public class OrderService 
    {
        MyORM.Interfaces.IDBContext _context;

        
        public OrderService(MyORM.Interfaces.IDBContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Order>> GetAll()
        {
            return await _context.Collection<Order>().OrderBy(d => d.Id).Join(d => d.Item).ToListAsync();
        }
}

```

# For web

```csharp


builder.Services.AddScoped<MyORM.Interfaces.IDBContext, Data.Context>();

new Data.Context(pgConnBuilder).UpdateDataBase();

```


## Contributing
Pull requests are welcome. For major changes, please open an issue first to discuss what you would like to change.

## License
[MIT](https://choosealicense.com/licenses/mit/)
