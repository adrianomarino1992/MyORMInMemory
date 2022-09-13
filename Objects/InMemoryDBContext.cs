using MyORM.Exceptions;
using MyORM.Interfaces;
using System.Reflection;

namespace MyORMInMemory.Objects
{
    public class InMemoryDBContext : IDBContext
    {

        public IEnumerable<Type> MappedTypes
        {
            get
            {
                return this.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(d => d.PropertyType.GetInterfaces().Contains(typeof(IEntityCollection))).
                    Select(s => s.PropertyType.GetGenericArguments()[0]).Distinct().ToList();

            }
        }

        public InMemoryDBContext()
        {
            PropertyInfo[] propertyInfos = this.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
              .Where(d => d.PropertyType.GetInterfaces().Contains(typeof(IEntityCollection))).ToArray();

            foreach (PropertyInfo propertyInfo in propertyInfos)
            {
                propertyInfo.SetValue(this, Activator.CreateInstance(propertyInfo.PropertyType, this));
            }
        }

        public IEntityCollection<T>? Collection<T>() where T : class
        {
            List<PropertyInfo> props = this.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(d => d.PropertyType.IsAssignableTo(typeof(IEntityCollection))).ToList();

            foreach (PropertyInfo s in props)
            {
                if (s.PropertyType.GetGenericArguments().Contains(typeof(T)))
                    return s.GetValue(this) as IEntityCollection<T>;
            }

            throw new NoEntityMappedException($"No one IEntityCollection was found to the type {typeof(T)}");
        }

        public IEntityCollection? Collection(Type collectionType)
        {
            List<PropertyInfo> props = this.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(d => d.PropertyType.IsAssignableTo(typeof(IEntityCollection))).ToList();

            foreach (PropertyInfo s in props)
            {
                if (s.PropertyType.GetGenericArguments().Contains(collectionType))
                    return s.GetValue(this) as IEntityCollection;
            }

            throw new NoEntityMappedException($"No one IEntityCollection was found to the type {collectionType.Name}");


        }

        public void CreateDataBase()
        {
            return;
        }

        public void DropDataBase()
        {
            foreach(Type tp in this.MappedTypes)
            {
                IEntityCollection? col = this.Collection(tp);

                if(col != null)
                {
                    MethodInfo? resetDelegate = col.GetType().GetMethod("Reset", BindingFlags.Instance | BindingFlags.Public);

                    if(resetDelegate != null)
                    {
                        resetDelegate.Invoke(col, null);
                    }
                }

            }
        }

        public bool TestConnection()
        {
            return true;
        }

        public void UpdateDataBase()
        {
            return;
        }
    }
}
