
using System.ComponentModel.DataAnnotations;
using MyORM.Attributes;
using MyORM.Exceptions;
using MyORM.Interfaces;

using MyORMInMemory.Helpers;

using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;


using static MyORMInMemory.Helpers.ReflectionExtension;

namespace MyORMInMemory.Objects
{
    public class InMemoryCollection<T> : IEntityCollection<T> where T : class
    {
        private long _maxId = 0;
        private int _limit = 0;
        private int _offset = 0;
        private bool _madeQuery;
        private List<T> _list;
        private List<T> _curr;
        private InMemoryDBContext _context;
        public InMemoryCollection(InMemoryDBContext context)
        {
            _list = new List<T>();
            _curr = new List<T>();
            _context = context;
        }

        public T Add(T obj)
        {
            if (!_list.Contains(obj))
                _list.Add(obj);

            PropertyInfo? key = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(s => s.GetCustomAttribute<DBIgnoreAttribute>() == null)
                .Where(s => s.GetCustomAttribute<DBPrimaryKeyAttribute>() != null).FirstOrDefault();

#pragma warning disable
            if (key != null && ((long)key.GetValue(obj)) <= 0)
#pragma warning enable
            {
                _maxId++;
                key.SetValue(obj, _maxId);
            }

            PropertyInfo[] props = obj.GetPublicProperties(s => s.PropertyType.IsClass)
                .Where(s => s.PropertyType != typeof(string)).ToArray();

            foreach (PropertyInfo prop in props)
            {
                List<object> toAdd = new List<object>();
               

                if (typeof(IEnumerable).IsAssignableFrom(prop.PropertyType))
                {
                    if (prop.PropertyType.GetElementType() != null)
                    {
                        if (!_context.MappedTypes.Contains(prop.PropertyType.GetElementType()))
                        {
                            continue;
                        }

                        Array arr = (Array)prop.GetValue(obj);

                        foreach (var it in arr)
                        {
                            toAdd.Add(it);
                        }

                    }
                    else
                    {
                        if (!_context.MappedTypes.Contains(prop.PropertyType.GetGenericArguments()[0]))
                        {
                            continue;
                        }

                        IList list = (IList)prop.GetValue(obj);

                        foreach (var it in list)
                        {
                            toAdd.Add(it);
                        }

                    }
                }
                else
                {
                    toAdd.Add(prop.GetValue(obj));
                }

          
                    foreach (var @object in toAdd)
                    {

                        if (@object == null)
                            continue;

                        PropertyInfo fk = GetPublicProperty(@object.GetType(), c => c.Name == $"{obj.GetType().Name}Id" || c.PropertyType.GetCustomAttribute<DBColumnAttribute>()?.Name == $"{obj.GetType().Name}Id");


                        if (fk != null && key != null)
                            fk.SetValue(@object, key.GetValue(obj));

                        try
                        {

                            object? result = _context.CallMethodByReflection("Collection", new Type[] { @object.GetType() }, null, null);

                            result?.CallMethodByReflection("Add", new object?[] { @object });

                            fk = GetPublicProperty(obj.GetType(), c => c.Name == $"{@object.GetType().Name}Id" || @object.GetType().GetCustomAttribute<DBColumnAttribute>()?.Name == $"{@object.GetType().Name}Id");

                            if (fk != null)
                            {
                                key = @object.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                                           .Where(s => s.GetCustomAttribute<DBIgnoreAttribute>() == null)
                                                           .Where(s => s.GetCustomAttribute<DBPrimaryKeyAttribute>() != null).FirstOrDefault();

                                if (key != null)
                                {
                                    fk.SetValue(obj, key.GetValue(@object));

                                }
                            }

                        }
                        catch { }
                    }
                


            }

            return obj;
        }

        public Task<T> AddAsync(T obj)
        {
            return Task.Run<T>(() => Add(obj));
        }

        public IQueryableCollection<T> And<TResult>(System.Linq.Expressions.Expression<Func<T, TResult>> expression)
        {
            _madeQuery = true;

            if (!expression.IsPredicate())
                throw new CastFailException($"Is not possible cast this {expression.Body.ToString()} into Func<{typeof(T).Name},{typeof(bool).Name}>");

#pragma warning disable
            _list.Where(s => (bool)(object)(expression.Compile()(s)))
#pragma warning enable
                .ToList()
                .ForEach(r =>
                {
                    if (!_curr.Contains(r))
                        _curr.Add(r);
                });

            return this;
        }

        public int Count()
        {
            return _list.Count;
        }

        public Task<int> CountAsync()
        {
            return Task.Run<int>(() => Count());
        }

        public void Delete(T obj)
        {
            if (_list.Contains(obj))
                _list.Remove(obj);
        }

        public Task DeleteAsync(T obj)
        {
            return Task.Run(() => Delete(obj));
        }

        public ICommand GetCommand()
        {
            return null;
        }

        public IQueryableCollection<T> Join<TResult>(System.Linq.Expressions.Expression<Func<T, TResult>> expression)
        {
            MemberExpression? memberExpression = expression.Body as MemberExpression;

            if (memberExpression == null)
            {
                throw new global::MyORM.Exceptions.InvalidExpressionException($"The lambda expression {expression.Body.ToString()} is not a valid member expression");
            }

            PropertyInfo? member = memberExpression.Member as PropertyInfo;

            if (memberExpression == null)
            {
                throw new global::MyORM.Exceptions.InvalidMemberForExpressionException($"Can´t read the PropertyInfo of the member of expression");
            }

            if (!(member.ReflectedType == typeof(T) || typeof(T).IsSubclassOf(member.ReflectedType)))
            {
                throw new global::MyORM.Exceptions.InvalidMemberForExpressionException($"The property {member.Name} is not a property of {typeof(T).Name}");

            }

            if (member.PropertyType.IsValueType || member.PropertyType == typeof(string))
            {
                return this;

            }

            PropertyInfo fk = member.PropertyType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(s => s.GetCustomAttribute<DBIgnoreAttribute>() == null)
                .Where(s => s.GetCustomAttribute<DBForeignKeyAttribute>() != null)
                .Where(s => s.Name == $"{typeof(T).Name}Id")
                .FirstOrDefault();

            Type colType = member.PropertyType;

            if(typeof(IEnumerable).IsAssignableFrom(member.PropertyType))
            {
                if(member.PropertyType.GetElementType() != null)
                {
                    fk = member.PropertyType.GetElementType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .Where(s => s.GetCustomAttribute<DBIgnoreAttribute>() == null)
                        .Where(s => s.GetCustomAttribute<DBForeignKeyAttribute>() != null)
                        .Where(s => s.Name == $"{typeof(T).Name}Id")
                        .FirstOrDefault();

                    colType = member.PropertyType.GetElementType();
                }
                else
                {
                    fk = member.PropertyType.GetGenericArguments()[0].GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .Where(s => s.GetCustomAttribute<DBIgnoreAttribute>() == null)
                        .Where(s => s.GetCustomAttribute<DBForeignKeyAttribute>() != null)
                        .Where(s => s.Name == $"{typeof(T).Name}Id")
                        .FirstOrDefault();

                    colType = member.PropertyType.GetGenericArguments()[0];

                }
            }

            if(!_context.MappedTypes.Contains(colType))
            {
                return this;
            }

            PropertyInfo key = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(s => s.GetCustomAttribute<DBIgnoreAttribute>() == null)
                .Where(s => s.GetCustomAttribute<DBPrimaryKeyAttribute>() != null)
                .FirstOrDefault();

            bool reverseKeyCheck = false;

            if(fk == null)
            {
                fk = colType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .Where(s => s.GetCustomAttribute<DBIgnoreAttribute>() == null)
                        .Where(s => s.GetCustomAttribute<DBPrimaryKeyAttribute>() != null)
                        .FirstOrDefault();

                key =  typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .Where(s => s.GetCustomAttribute<DBIgnoreAttribute>() == null)
                        .Where(s => s.Name == $"{colType.Name}Id")
                        .FirstOrDefault();

                reverseKeyCheck = true;
            }

            if (fk == null)
            {
                throw new MyORM.Exceptions.NoEntityMappedException($"No one foreign key was mapped in {member.PropertyType.Name} to refer {typeof(T).Name}");
            }

            if (key == null)
            {
                throw new MyORM.Exceptions.NoEntityMappedException($"No one key was mapped in {typeof(T).Name}");
            }

            object? result = _context.CallMethodByReflection("Collection", new Type[] { colType }, null, null);

            object list = result?.CallMethodByReflection("Run");

            if (list.GetType().IsAssignableTo(typeof(IList)))
            {
                IList i = list as IList;

                List<T> r = _madeQuery ? _curr : _list;

                foreach (var o in r)
                {
                    if(typeof(IEnumerable).IsAssignableFrom(member.PropertyType))
                    {
                        member.SetValue(o, Activator.CreateInstance(member.PropertyType));
                    }
                }

                foreach (object u in i)
                {                                     

                    r.ForEach(s =>
                    {
                        if (fk.GetValue(u).Equals(key.GetValue(s)))
                        {
                            if(typeof(IEnumerable).IsAssignableFrom(member.PropertyType))
                            {
                                object @ref = s.GetType().GetProperty(member.Name).GetValue(s);

                                MethodInfo? addDelegate = @ref.GetType().GetMethod("Add");

                                if(addDelegate != null)
                                {
                                    addDelegate.Invoke(@ref, new object[]{u});
                                }

                            }else{

                                s.GetType().GetProperty(member.Name).SetValue(s, u);
                            }
                            
                            
                        }
                    });
                }

            }


            return this;


        }

        public IQueryableCollection<T> Limit(int limit)
        {
            _limit = limit;

            return this;
        }

        public IQueryableCollection<T> OffSet(int offSet)
        {
            _offset = offSet;

            return this;
        }

        public IQueryableCollection<T> Or<TResult>(System.Linq.Expressions.Expression<Func<T, TResult>> expression)
        {

            return this.And(expression);

        }

        public IQueryableCollection<T> OrderBy<TResult>(System.Linq.Expressions.Expression<Func<T, TResult>> expression)
        {
            if (expression.IsMemberAcess())
            {
                if (!_madeQuery)
                    _list = _list.OrderBy(expression.Compile()).ToList();
                else
                    _curr = _curr.OrderBy(expression.Compile()).ToList();
            }

            return this;
        }

        public IQueryableCollection<T> Query<TResult>(System.Linq.Expressions.Expression<Func<T, TResult>> expression)
        {
            _madeQuery = true;

            _curr = _list.Where(s =>
            {
                try
                {

                    var func = expression.Compile();

                    var result = func(s);

                    return result != null && result.GetType().Equals(typeof(bool)) && (bool)(object)result;

                }
                catch { return false; }

            }).ToList();

            return this;
        }

        public IEnumerable<T> Run()
        {
            List<T> r = new List<T>();

            if (!_madeQuery)
                r = _list;
            else
                r = _curr;

            _madeQuery = false;
           

            if (_offset > 0 && _offset < (r.Count - 1))
                r = r.GetRange(_offset, r.Count - (_offset + 1));

            if (_limit > 0 && _limit <= (r.Count - 1))
                r = r.Take(_limit).ToList();

            _offset = 0;
            _limit = 0;

            return r;
        }

        public IEnumerable<T> Run(string sql)
        {
            return Run();
        }

        public Task<IEnumerable<T>> RunAsync()
        {
            return Task.Run(() => Run());
        }

        public Task<IEnumerable<T>> RunAsync(string sql)
        {
            return Task.Run(() => Run());
        }

        public void Update(T obj)
        {
            PropertyInfo? key = obj.GetPublicProperty(s => s.GetCustomAttribute<DBPrimaryKeyAttribute>() != null);

            if (key == null)
                throw new KeyNotFoundException($"The Type {obj.GetType().Name} do not have any key mapped");

            _list.RemoveAll(s => key.GetValue(s).Equals(key.GetValue(obj)));

            Add(obj);            

        }

        public Task UpdateAsync(T obj)
        {
            return Task.Run(() => Update(obj));
        }


        public void Reset()
        {
            _limit = 0;
            _offset = 0;
            _list = new List<T>();
            _curr = new List<T>();
            _madeQuery = false;
            _maxId = 0;
        }
    }
}
