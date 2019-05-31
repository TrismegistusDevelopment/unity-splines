using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace Trismegistus.Splines
{
    public interface ISplineManager : IRelocate<int>, ICreate<int?>, IGetParams<float, PointParams>, 
        IGetAll<WaypointEntity>,
        IGetParams<int, PointParams>, IDelete<int>, IDelete<WaypointEntity>{
        int SelectClosestWaypointIndex(Vector3 position);
        Vector3 SelectClosestWaypointPosition(Vector3 position);
    }

    public interface IGetParams<in T, out TD>
    {
        TD GetParams(T entity);
    }

    public interface IDelete<in T>{
        void Delete(T entity);
    }

    public interface ICreate<in T>{
        void AddPoint([CanBeNull] T entity = default, Vector3? position = null);
    }

    public interface IRelocate<in T>{
        void Relocate(T from, T to);
    }

    public interface IGetAll<T>
    {
        int Count { get; }
        List<T> Entities { get; }
    }
}